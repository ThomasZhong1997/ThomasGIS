using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;
using ThomasGIS.Grids.Basic;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Helpers;
using System.Linq;
using ThomasGIS.Coordinates;

namespace ThomasGIS.TrajectoryPackage.DensityAnalysis
{
    public enum CharacteristicBandGenerateFunction
    {
        DOT,
        LINE
    }

    // 轨迹特征栅格生成器类
    public static class TrajectoryCharacteristicBandGenerator
    { 
        // 轨迹密度栅格
        public static IRasterBand DensityBand(TrajectorySet trajectorySet, CharacteristicBandGenerateFunction type, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] densityBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    densityBandData[i, j] = 0;
                }
            }

            if (type == CharacteristicBandGenerateFunction.DOT)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    for (int i = 0; i < oneTrajectory.PointNumber; i++)
                    {
                        GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                        int locX = (int)((gnssPoint.X - boundary.XMin) / scale);
                        int locY = (int)((gnssPoint.Y - boundary.YMin) / scale);
                        if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                        densityBandData[rows - 1 - locY, locX] += 1;
                    }
                }
            }
            else if (type == CharacteristicBandGenerateFunction.LINE)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    HashSet<string> locationSet = new HashSet<string>();
                    // 标记一条轨迹占用的全部栅格位置再计数
                    for (int i = 0; i < oneTrajectory.PointNumber - 1; i++)
                    {
                        GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                        GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);

                        int startLocX = (int)((startPoint.X - boundary.XMin) / scale);
                        int startLocY = (int)((startPoint.Y - boundary.YMin) / scale);
                        int endLocX = (int)((endPoint.X - boundary.XMin) / scale);
                        int endLocY = (int)((endPoint.Y - boundary.YMin) / scale);

                        List<Location> locations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY).ToList();

                        for (int j = 0; j < locations.Count; j++)
                        {
                            locationSet.Add(locations[j].X.ToString() + "," + locations[j].Y.ToString());
                        }
                    }

                    foreach (string location in locationSet)
                    {
                        string[] items = location.Split(',');
                        int lineX = Convert.ToInt32(items[0]);
                        int lineY = Convert.ToInt32(items[1]);
                        if (lineX < 0 || lineX >= cols || lineY < 0 || lineY >= rows) continue;
                        densityBandData[rows - 1 - lineY, lineX] += 1;
                    }
                }
            }

            RasterBand newBand = new RasterBand(rows, cols);
            newBand.WriteData(densityBandData);
            return newBand;
        }

        // 轨迹方向栅格
        public static IRasterBand DirectionBand(TrajectorySet trajectorySet, CharacteristicBandGenerateFunction type, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] densityBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    densityBandData[i, j] = 0;
                }
            }

            double[,] directionBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    directionBandData[i, j] = 0;
                }
            }

            if (type == CharacteristicBandGenerateFunction.DOT)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    for (int i = 0; i < oneTrajectory.PointNumber; i++)
                    {
                        GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                        int locX = (int)((gnssPoint.X - boundary.XMin) / scale);
                        int locY = (int)((gnssPoint.Y - boundary.YMin) / scale);
                        if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;

                        double direction = gnssPoint.Direction;
                        if (direction == -1) continue;

                        if (direction > 337.5) direction -= 360.0;
                        densityBandData[rows - 1 - locY, locX] += 1;
                        directionBandData[rows - 1 - locY, locX] += direction;
                    }
                }
            }
            else if (type == CharacteristicBandGenerateFunction.LINE)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    // 标记一条轨迹占用的全部栅格位置再计数
                    for (int i = 0; i < oneTrajectory.PointNumber - 1; i++)
                    {
                        GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                        GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);

                        int startLocX = (int)((startPoint.X - boundary.XMin) / scale);
                        int startLocY = (int)((startPoint.Y - boundary.YMin) / scale);
                        int endLocX = (int)((endPoint.X - boundary.XMin) / scale);
                        int endLocY = (int)((endPoint.Y - boundary.YMin) / scale);

                        double angle;
                        if (oneTrajectory.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            angle = DistanceCalculator.DirectionAngleGeo(startPoint, endPoint);
                        }
                        else
                        {
                            angle = DistanceCalculator.DirectionAngle(startPoint, endPoint);
                        }

                        List<Location> locations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY).ToList();

                        if (i == oneTrajectory.PointNumber - 2)
                        {
                            for (int j = 0; j < locations.Count; j++)
                            {
                                int locY = rows - 1 - locations[j].Y;
                                int locX = locations[j].X;
                                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                                densityBandData[locY, locX] += 1;
                                directionBandData[locY, locX] += angle;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < locations.Count - 1; j++)
                            {
                                int locY = rows - 1 - locations[j].Y;
                                int locX = locations[j].X;
                                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                                densityBandData[locY, locX] += 1;
                                directionBandData[locY, locX] += angle;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (densityBandData[i, j] != 0)
                    {
                        directionBandData[i, j] /= densityBandData[i, j];
                    }
                }
            }

            RasterBand newBand = new RasterBand(rows, cols);
            newBand.WriteData(directionBandData);
            return newBand;
        }

        // 轨迹方向栅格
        public static IRasterBand SpeedBand(TrajectorySet trajectorySet, CharacteristicBandGenerateFunction type, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] densityBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    densityBandData[i, j] = 0;
                }
            }

            double[,] speedBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    speedBandData[i, j] = 0;
                }
            }

            if (type == CharacteristicBandGenerateFunction.DOT)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    for (int i = 0; i < oneTrajectory.PointNumber; i++)
                    {
                        GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                        int locX = (int)((gnssPoint.X - boundary.XMin) / scale);
                        int locY = (int)((gnssPoint.Y - boundary.YMin) / scale);
                        if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;

                        double speed = gnssPoint.Speed;
                        if (speed < 0) continue;

                        densityBandData[rows - 1 - locY, locX] += 1;
                        speedBandData[rows - 1 - locY, locX] += speed;
                    }
                }
            }
            else if (type == CharacteristicBandGenerateFunction.LINE)
            {
                foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
                {
                    // 标记一条轨迹占用的全部栅格位置再计数
                    for (int i = 0; i < oneTrajectory.PointNumber - 1; i++)
                    {
                        GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                        GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);

                        if (startPoint.Timestamp == endPoint.Timestamp) continue;

                        int startLocX = (int)((startPoint.X - boundary.XMin) / scale);
                        int startLocY = (int)((startPoint.Y - boundary.YMin) / scale);
                        int endLocX = (int)((endPoint.X - boundary.XMin) / scale);
                        int endLocY = (int)((endPoint.Y - boundary.YMin) / scale);

                        double distance;
                        if (oneTrajectory.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                        }

                        double speed = distance / (endPoint.Timestamp - startPoint.Timestamp);

                        List<Location> locations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY).ToList();

                        if (i == oneTrajectory.PointNumber - 2)
                        {
                            for (int j = 0; j < locations.Count; j++)
                            {
                                int locY = (int)(rows - 1 - locations[j].Y);
                                int locX = (int)(locations[j].X);
                                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                                densityBandData[locY, locX] += 1;
                                speedBandData[locY, locX] += speed;
                            }
                        }
                        else
                        {
                            for (int j = 0; j < locations.Count - 1; j++)
                            {
                                int locY = (int)(rows - 1 - locations[j].Y);
                                int locX = (int)(locations[j].X);
                                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                                densityBandData[locY, locX] += 1;
                                speedBandData[locY, locX] += speed;
                            }
                        }
                    }
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (densityBandData[i, j] != 0)
                    {
                        speedBandData[i, j] /= densityBandData[i, j];
                    }
                }
            }

            RasterBand newBand = new RasterBand(rows, cols);
            newBand.WriteData(speedBandData);
            return newBand;
        }

        // 重载方法，用户自行定义数值，仅使用轨迹点的extraInfo部分
        public static IRasterBand PropertyBand(TrajectorySet trajectorySet, Func<Dictionary<string, object>, double> innerValue, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] densityBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    densityBandData[i, j] = 0;
                }
            }

            double[,] propertyBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    propertyBandData[i, j] = 0;
                }
            }


            foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
            {
                for (int i = 0; i < oneTrajectory.PointNumber; i++)
                {
                    GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                    int locX = (int)((gnssPoint.X - boundary.XMin) / scale);
                    int locY = (int)((gnssPoint.Y - boundary.YMin) / scale);
                    if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;

                    double speed = gnssPoint.Speed;
                    if (speed < 0) continue;

                    densityBandData[rows - 1 - locY, locX] += 1;
                    propertyBandData[rows - 1 - locY, locX] += innerValue(gnssPoint.ExtraInformation);
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    propertyBandData[i, j] /= densityBandData[i, j];
                }
            }

            RasterBand newBand = new RasterBand(rows, cols);
            newBand.WriteData(propertyBandData);
            return newBand;
        }

        // 重载方法，用户自行定义数值，使用轨迹点的方向，速度与extraInfo部分
        public static IRasterBand ComplexBand(TrajectorySet trajectorySet, Func<double, double, Dictionary<string, object>, double> innerValue, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] densityBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    densityBandData[i, j] = 0;
                }
            }

            double[,] propertyBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    propertyBandData[i, j] = 0;
                }
            }

            foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
            {
                for (int i = 0; i < oneTrajectory.PointNumber; i++)
                {
                    GnssPoint gnssPoint = oneTrajectory.GetPointByIndex(i);
                    int locX = (int)((gnssPoint.X - boundary.XMin) / scale);
                    int locY = (int)((gnssPoint.Y - boundary.YMin) / scale);
                    if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;

                    double speed = gnssPoint.Speed;
                    if (speed < 0) continue;

                    densityBandData[rows - 1 - locY, locX] += 1;
                    propertyBandData[rows - 1 - locY, locX] += innerValue(gnssPoint.Direction, gnssPoint.Speed, gnssPoint.ExtraInformation);
                }
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    propertyBandData[i, j] /= densityBandData[i, j];
                }
            }

            RasterBand newBand = new RasterBand(rows, cols);
            newBand.WriteData(propertyBandData);
            return newBand;
        }

        public static IRasterBand OriginBand(TrajectorySet trajectorySet, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] originBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    originBandData[i, j] = 0;
                }
            }

            foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
            {
                GnssPoint originPoint = oneTrajectory.GetPointByIndex(0);
                int locX = (int)((originPoint.X - boundary.XMin) / scale);
                int locY = (int)((originPoint.Y - boundary.YMin) / scale);
                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                originBandData[rows - 1 - locY, locX] += 1;
            }

            IRasterBand newRasterBand = new RasterBand(rows, cols);
            newRasterBand.WriteData(originBandData);
            return newRasterBand;
        }

        public static IRasterBand DestinationBand(TrajectorySet trajectorySet, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] destinationBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    destinationBandData[i, j] = 0;
                }
            }

            foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
            {
                GnssPoint destinationPoint = oneTrajectory.GetPointByIndex(oneTrajectory.PointNumber - 1);
                int locX = (int)((destinationPoint.X - boundary.XMin) / scale);
                int locY = (int)((destinationPoint.Y - boundary.YMin) / scale);
                if (locX < 0 || locX >= cols || locY < 0 || locY >= rows) continue;
                destinationBandData[rows - 1 - locY, locX] += 1;
            }

            IRasterBand newRasterBand = new RasterBand(rows, cols);
            newRasterBand.WriteData(destinationBandData);
            return newRasterBand;
        }

        public static IRasterBand OriginDestinationBand(TrajectorySet trajectorySet, BoundaryBox boundary = null, double scale = -1)
        {
            if (scale <= 0)
            {
                scale = Convert.ToDouble(Configuration.GetConfiguration("grid.default.scale"));
            }

            if (boundary == null)
            {
                boundary = trajectorySet.CalculateBoundaryBox();
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale) + 1;
            int cols = (int)((boundary.XMax - boundary.XMin) / scale) + 1;

            double[,] ODBandData = new double[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    ODBandData[i, j] = 0;
                }
            }

            foreach (Trajectory oneTrajectory in trajectorySet.TrajectoryList)
            {
                GnssPoint originPoint = oneTrajectory.GetPointByIndex(0);
                int locX = (int)((originPoint.X - boundary.XMin) / scale);
                int locY = (int)((originPoint.Y - boundary.YMin) / scale);
                if (locX >= 0 && locX < cols && locY >= 0 && locY < rows)
                {
                    ODBandData[rows - 1 - locY, locX] += 1;
                }

                GnssPoint destinationPoint = oneTrajectory.GetPointByIndex(oneTrajectory.PointNumber - 1);
                locX = (int)((destinationPoint.X - boundary.XMin) / scale);
                locY = (int)((destinationPoint.Y - boundary.YMin) / scale);
                if (locX >= 0 && locX < cols && locY >= 0 && locY < rows)
                {
                    ODBandData[rows - 1 - locY, locX] += 1;
                }
            }

            IRasterBand newRasterBand = new RasterBand(rows, cols);
            newRasterBand.WriteData(ODBandData);
            return newRasterBand;
        }
    }
}
