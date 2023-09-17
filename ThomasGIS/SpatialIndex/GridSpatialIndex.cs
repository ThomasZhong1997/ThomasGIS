using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using System.Linq;
using ThomasGIS.Network;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Coordinates;
using ThomasGIS.TrajectoryPackage;
using ThomasGIS.Geometries.OpenGIS;

namespace ThomasGIS.SpatialIndex
{
    public class GridSpatialIndex : ISpatialIndex
    {
        private double xmin;
        private double xmax;
        private double ymin;
        private double ymax;

        private int rows;
        private int cols;

        private int preLocation = 0;

        private double scale;

        private HashSet<int>[,] spatialIndexContainer = null;

        private List<IGeometry> innerData = null;

        public int GeometryCount => innerData.Count;

        public CoordinateBase coordinateSystem = null;

        public GridSpatialIndex(BoundaryBox boundary, double scale, CoordinateBase coordinateBase)
        {
            if (scale == 0) scale = 1;

            if (coordinateBase.GetCoordinateType() == CoordinateType.Geographic)
            {
                scale /= 111000.0;
            }

            int rows = (int)((boundary.YMax - boundary.YMin) / scale + 1);
            int cols = (int)((boundary.XMax - boundary.XMin) / scale + 1);

            this.xmin = boundary.XMin;
            this.xmax = boundary.XMax;
            this.ymin = boundary.YMin;
            this.ymax = boundary.YMax;
            this.scale = scale;
            this.rows = rows;
            this.cols = cols;

            if (rows <= 0 || cols <= 0) throw new Exception("索引的空间范围错误，请检查boundary参数是否合法！");

            spatialIndexContainer = new HashSet<int>[rows, cols];

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    spatialIndexContainer[i,j] = new HashSet<int>();
                }
            }

            this.innerData = new List<IGeometry>();

            this.preLocation = 0;

            this.coordinateSystem = coordinateBase;
        }

        // 依据表
        public bool RefreshIndex()
        {
            // 只向后更新未添加过的即可
            for (int i = preLocation; i < this.innerData.Count; i++)
            {
                IGeometry geometry = this.innerData[i];
                string geometryBaseType = geometry.GetBaseGeometryType();
                if (geometryBaseType == "Point")
                {
                    IPoint point = geometry as IPoint;
                    int xLoc = (int)((point.GetX() - xmin) / scale);
                    int yLoc = (int)((point.GetY() - ymin) / scale);
                    if (xLoc < 0 || xLoc >= this.cols || yLoc < 0 || yLoc >= this.rows) continue;
                    this.spatialIndexContainer[yLoc, xLoc].Add(i);
                }

                if (geometryBaseType == "SingleLine")
                {
                    ISingleLine singleLine = geometry as ISingleLine;

                    int startLocX = (int)((singleLine.GetStartPoint().GetX() - xmin) / scale);
                    int startLocY = (int)((singleLine.GetStartPoint().GetY() - ymin) / scale);
                    int endLocX = (int)((singleLine.GetEndPoint().GetX() - xmin) / scale);
                    int endLocY = (int)((singleLine.GetEndPoint().GetY() - ymin) / scale);

                    IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                    foreach (Location location in fillLocations)
                    {
                        if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                        this.spatialIndexContainer[location.Y, location.X].Add(i);
                    }
                }

                if (geometryBaseType == "LineString")
                {
                    ILineString lineString = geometry as ILineString;
                    List<IPoint> pointList = lineString.GetPointList().ToList();
                    for (int k = 0; k < pointList.Count - 1; k++)
                    {
                        IPoint startPoint = pointList[k];
                        IPoint endPoint = pointList[k + 1];

                        int startLocX = (int)((startPoint.GetX() - xmin) / scale);
                        int startLocY = (int)((startPoint.GetY() - ymin) / scale);
                        int endLocX = (int)((endPoint.GetX() - xmin) / scale);
                        int endLocY = (int)((endPoint.GetY() - ymin) / scale);

                        IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                        foreach (Location location in fillLocations)
                        {
                            if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                            this.spatialIndexContainer[location.Y, location.X].Add(i);
                        }
                    }
                }

                if (geometryBaseType == "MultiLineString")
                {
                    IMultiLineString multiLineString = geometry as IMultiLineString;
                    List<IEnumerable<IPoint>> pointList = multiLineString.GetPointList().ToList();
                    for (int j = 0; j < pointList.Count; j++)
                    {
                        List<IPoint> oneLineString = pointList[j].ToList();
                        for (int k = 0; k < oneLineString.Count - 1; k++)
                        {
                            IPoint startPoint = oneLineString[k];
                            IPoint endPoint = oneLineString[k + 1];

                            int startLocX = (int)((startPoint.GetX() - xmin) / scale);
                            int startLocY = (int)((startPoint.GetY() - ymin) / scale);
                            int endLocX = (int)((endPoint.GetX() - xmin) / scale);
                            int endLocY = (int)((endPoint.GetY() - ymin) / scale);

                            IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                            foreach (Location location in fillLocations)
                            {
                                if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                this.spatialIndexContainer[location.Y, location.X].Add(i);
                            }
                        }
                    }
                }

                if (geometryBaseType == "Polygon")
                {
                    IPolygon polygon = geometry as IPolygon;
                    List<IEnumerable<IPoint>> pointList = polygon.GetPointList().ToList();

                    for (int k = 0; k < pointList.Count; k++)
                    {
                        List<IPoint> onePart = pointList[k].ToList();
                        List<Location> boundaryLocation = new List<Location>();

                        for (int m = 0; m < onePart.Count; m++)
                        {
                            IPoint nowPoint = onePart[k];
                            int locX = (int)((nowPoint.GetX() - this.xmin) / this.scale);
                            int locY = (int)((nowPoint.GetY() - this.ymin) / this.scale);
                            boundaryLocation.Add(new Location(locX, locY));
                        }

                        IEnumerable<Location> fillLocations = GISCGCalculator.PolygonFillLocations(boundaryLocation);
                        if (k == 0)
                        {
                            foreach (Location location in fillLocations)
                            {
                                if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                this.spatialIndexContainer[location.Y, location.X].Add(i);
                            }
                        }
                        else
                        {
                            foreach (Location location in fillLocations)
                            {
                                if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                this.spatialIndexContainer[location.Y, location.X].Remove(i);
                            }
                        }
                    }
                }

                if (geometryBaseType == "MultiPolygon")
                {
                    IMultiPolygon multiPolygon = geometry as IMultiPolygon;

                    List<IEnumerable<IEnumerable<IPoint>>> pointList = multiPolygon.GetPointList().ToList();

                    for (int j = 0; j < pointList.Count; j++)
                    {
                        List<IEnumerable<IPoint>> onePolygon = pointList[j].ToList();
                        for (int k = 0; k < onePolygon.Count; k++)
                        {
                            List<IPoint> onePart = onePolygon[k].ToList();
                            List<Location> boundaryLocation = new List<Location>();

                            for (int m = 0; m < onePart.Count; m++)
                            {
                                IPoint nowPoint = onePart[m];
                                int locX = (int)((nowPoint.GetX() - this.xmin) / this.scale);
                                int locY = (int)((nowPoint.GetY() - this.ymin) / this.scale);
                                if (boundaryLocation.Count > 0)
                                {
                                    Location lastLocation = boundaryLocation.Last();
                                    if (lastLocation.X != locX || lastLocation.Y != locY)
                                    {
                                        boundaryLocation.Add(new Location(locX, locY));
                                    }
                                }
                                else
                                {
                                    boundaryLocation.Add(new Location(locX, locY));
                                }
                            }

                            // 检测转换到格网中是线还是面，可能N个点全在一个格子里，那就是线形态了
                            IEnumerable<Location> fillLocations = boundaryLocation;
                            if (boundaryLocation.Count == 2 || boundaryLocation.Count == 3)
                            {
                                fillLocations = GISCGCalculator.PolylineFillLocations(boundaryLocation);
                            }
                            else if (boundaryLocation.Count > 3)
                            {
                                fillLocations = GISCGCalculator.PolygonFillLocations(boundaryLocation);
                            }
                            
                            if (k == 0)
                            {
                                foreach (Location location in fillLocations)
                                {
                                    if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                    this.spatialIndexContainer[location.Y, location.X].Add(i);
                                }
                            }
                            else
                            {
                                foreach (Location location in fillLocations)
                                {
                                    if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                    this.spatialIndexContainer[location.Y, location.X].Remove(i);
                                }
                            }
                        }
                    }
                }
            }

            this.preLocation = innerData.Count;
            return true;
        }

        public bool AddItem(IGeometry item)
        {
            this.innerData.Add(item);
            return true;
        }

        // 移除对应index的要素
        public bool RemoveItem(int index)
        {
            // 本质上仅移除grid里的index数值，不操作innerData中的index，降低复杂度，即只向innerData中添加
            IGeometry geometry = this.innerData[index];
            if (geometry.GetType() == typeof(Point) || geometry.GetType() == typeof(GeoNetworkNode) || geometry.GetType() == typeof(ShpPoint) || geometry.GetType() == typeof(GnssPoint) || geometry.GetType() == typeof(OpenGIS_Point))
            {
                IPoint point = geometry as IPoint;
                int xLoc = (int)((point.GetX() - xmin) / scale);
                int yLoc = (int)((point.GetY() - ymin) / scale);
                if (xLoc < 0 || xLoc >= this.cols || yLoc < 0 || yLoc >= this.rows) return false;
                this.spatialIndexContainer[yLoc, xLoc].Remove(index);
            }

            if (geometry.GetType() == typeof(SingleLine) || geometry.GetType() == typeof(GeoNetworkArc))
            {
                ISingleLine singleLine = geometry as ISingleLine;

                int startLocX = (int)((singleLine.GetStartPoint().GetX() - xmin) / scale);
                int startLocY = (int)((singleLine.GetStartPoint().GetY() - ymin) / scale);
                int endLocX = (int)((singleLine.GetEndPoint().GetX() - xmin) / scale);
                int endLocY = (int)((singleLine.GetEndPoint().GetY() - ymin) / scale);

                IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                foreach (Location location in fillLocations)
                {
                    if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                    this.spatialIndexContainer[location.Y, location.X].Remove(index);
                }
            }

            if (geometry.GetType() == typeof(OpenGIS_LineString))
            {
                ILineString lineString = geometry as ILineString;
                List<IPoint> pointList = lineString.GetPointList().ToList();
                for (int k = 0; k < pointList.Count - 1; k++)
                {
                    IPoint startPoint = pointList[k];
                    IPoint endPoint = pointList[k + 1];

                    int startLocX = (int)((startPoint.GetX() - xmin) / scale);
                    int startLocY = (int)((startPoint.GetY() - ymin) / scale);
                    int endLocX = (int)((endPoint.GetX() - xmin) / scale);
                    int endLocY = (int)((endPoint.GetY() - ymin) / scale);

                    IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                    foreach (Location location in fillLocations)
                    {
                        if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                        this.spatialIndexContainer[location.Y, location.X].Remove(index);
                    }
                }
            }

            if (geometry.GetType() == typeof(ShpPolyline) || geometry.GetType() == typeof(OpenGIS_MultiLineString))
            {
                IMultiLineString multiLineString = geometry as IMultiLineString;
                List<IEnumerable<IPoint>> pointList = multiLineString.GetPointList().ToList();
                for (int j = 0; j < pointList.Count; j++)
                {
                    List<IPoint> oneLineString = pointList[j].ToList();
                    for (int k = 0; k < oneLineString.Count - 1; k++)
                    {
                        IPoint startPoint = oneLineString[k];
                        IPoint endPoint = oneLineString[k + 1];

                        int startLocX = (int)((startPoint.GetX() - xmin) / scale);
                        int startLocY = (int)((startPoint.GetY() - ymin) / scale);
                        int endLocX = (int)((endPoint.GetX() - xmin) / scale);
                        int endLocY = (int)((endPoint.GetY() - ymin) / scale);

                        IEnumerable<Location> fillLocations = GISCGCalculator.DigitalDifferentialAnalyzer(startLocX, startLocY, endLocX, endLocY);

                        foreach (Location location in fillLocations)
                        {
                            if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                            this.spatialIndexContainer[location.Y, location.X].Remove(index);
                        }
                    }
                }
            }

            if (geometry.GetType() == typeof(OpenGIS_Polygon))
            {
                IPolygon polygon = geometry as IPolygon;
                List<IEnumerable<IPoint>> pointList = polygon.GetPointList().ToList();

                for (int k = 0; k < pointList.Count; k++)
                {
                    List<IPoint> onePart = pointList[k].ToList();
                    List<Location> boundaryLocation = new List<Location>();

                    for (int m = 0; m < onePart.Count; m++)
                    {
                        IPoint nowPoint = onePart[k];
                        int locX = (int)((nowPoint.GetX() - this.xmin) / this.scale);
                        int locY = (int)((nowPoint.GetY() - this.ymin) / this.scale);
                        boundaryLocation.Add(new Location(locX, locY));
                    }

                    IEnumerable<Location> fillLocations = GISCGCalculator.PolygonFillLocations(boundaryLocation);
                    if (k == 0)
                    {
                        foreach (Location location in fillLocations)
                        {
                            if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                            this.spatialIndexContainer[location.Y, location.X].Remove(index);
                        }
                        break;
                    }
                }
            }

            if (geometry.GetType() == typeof(ShpPolygon) || geometry.GetType() == typeof(OpenGIS_MultiPolygon))
            {
                IMultiPolygon multiPolygon = geometry as IMultiPolygon;

                List<IEnumerable<IEnumerable<IPoint>>> pointList = multiPolygon.GetPointList().ToList();

                for (int j = 0; j < pointList.Count; j++)
                {
                    List<IEnumerable<IPoint>> onePolygon = pointList[j].ToList();
                    for (int k = 0; k < onePolygon.Count; k++)
                    {
                        List<IPoint> onePart = onePolygon[k].ToList();
                        List<Location> boundaryLocation = new List<Location>();

                        for (int m = 0; m < onePart.Count; m++)
                        {
                            IPoint nowPoint = onePart[k];
                            int locX = (int)((nowPoint.GetX() - this.xmin) / this.scale);
                            int locY = (int)((nowPoint.GetY() - this.ymin) / this.scale);
                            boundaryLocation.Add(new Location(locX, locY));
                        }

                        IEnumerable<Location> fillLocations = GISCGCalculator.PolygonFillLocations(boundaryLocation);
                        if (k == 0)
                        {
                            foreach (Location location in fillLocations)
                            {
                                if (location.X < 0 || location.X >= this.cols || location.Y < 0 || location.Y >= this.rows) continue;
                                this.spatialIndexContainer[location.Y, location.X].Remove(index);
                            }
                            break;
                        }
                    }
                }
            }

            return true;
        }

        public bool RemoveItem(IGeometry geometry)
        {
            int index = this.innerData.IndexOf(geometry);
            this.RemoveItem(index);
            return true;
        }

        public IEnumerable<IGeometry> SearchItem(IPoint point, double range)
        {
            IEnumerable<int> resultIndexList = SearchID(point, range);

            List<IGeometry> result = new List<IGeometry>();

            foreach (int index in resultIndexList)
            {
                result.Add(this.innerData[index]);
            }

            return result;
        }

        public IEnumerable<int> SearchID(IPoint point, double range)
        {
            if (preLocation != innerData.Count)
            {
                RefreshIndex();
            }

            int locX = (int)((point.GetX() - this.xmin) / this.scale);
            int locY = (int)((point.GetY() - this.ymin) / this.scale);

            int xWindowSize = -1;
            int yWindowSize = -1;

            if (this.coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                double latitudeLength = 111000.0 * Math.Cos(point.GetY() / 180.0 * Math.PI);
                double longitudeLength = 111000.0;
                xWindowSize = (int)((double)(range / longitudeLength) / scale + 1);
                yWindowSize = (int)((double)(range / latitudeLength) / scale + 1);
            }
            else
            {
                xWindowSize = (int)(range / this.scale) + 1;
                yWindowSize = (int)(range / this.scale) + 1;
            }


            HashSet<int> result = new HashSet<int>();

            for (int i = -xWindowSize; i <= xWindowSize; i++)
            {
                for (int j = -yWindowSize; j <= yWindowSize; j++)
                {
                    int nowX = locX + i;
                    int nowY = locY + j;
                    if (nowX < 0 || nowX >= this.cols || nowY < 0 || nowY >= this.rows) continue;
                    result.UnionWith(this.spatialIndexContainer[nowY, nowX]);
                }
            }

            return result;
        }

        public IGeometry GetItemByIndex(int index)
        {
            if (index < -this.GeometryCount || index >= this.GeometryCount) throw new Exception("索引超出范围！");

            if (index < 0) index += this.GeometryCount;

            return this.innerData[index];
        }

        public double GetScale()
        {
            return scale;
        }
    }
}
