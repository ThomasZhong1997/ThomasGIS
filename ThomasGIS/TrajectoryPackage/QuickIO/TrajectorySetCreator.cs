using System;
using System.Collections.Generic;
using System.Linq;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using ThomasGIS.TrajectoryPackage.Preprocess;

namespace ThomasGIS.TrajectoryPackage.QuickIO
{
    public class TrajectorySetCreator : ITrajectorySetCreator
    {
        protected BoundaryBox readerBoundary = null;
        protected bool jumpTitleLine = false;

        public bool IsJumpTitleLine()
        {
            return jumpTitleLine;
        }

        public bool SetJumpTitleLine(bool option)
        {
            this.jumpTitleLine = option;
            return true;
        }

        public TrajectorySetCreator(BoundaryBox readerBoundary = null)
        {
            this.readerBoundary = readerBoundary;
        }

        // 虚方法，轨迹点的预处理函数，内部可自由组合Preprocessor类中定义的预处理方法
        public virtual void PointFilter(Trajectory oneTrajectory)
        {
            double maxSpeed = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.filter.maxspeed"));
            // 移除跳变点与时间相同点
            Preprocessor.RemoveErrorPoint(oneTrajectory, maxSpeed);
            Preprocessor.RemoveSameLocationPoint(oneTrajectory);
        }

        // 虚方法，定义如何处理读入的每行数据
        public virtual GnssPoint Reader(string line)
        {
            string[] items = line.Split(',');
            string ID = items[0];
            double X = Convert.ToDouble(items[1]);
            double Y = Convert.ToDouble(items[2]);
            int timestamp = DatetimeCalculator.DatetimeToTimestamp(items[3], "yyyy-MM-dd HH:mm:ss");

            // 如果不存在点范围限制则直接返回
            if (readerBoundary == null) return new GnssPoint(ID, X, Y, timestamp);
            // 否则需要判断点是否在BoundaryBox内
            if (X < readerBoundary.XMin || X > readerBoundary.XMax || Y < readerBoundary.YMin || Y > readerBoundary.YMax) return null;

            return new GnssPoint(ID, X, Y, timestamp);
        }

        // 虚方法，定义如何分割轨迹对象，默认按配置文件中提供的距离与时间间隔执行分割
        public virtual IEnumerable<Trajectory> TrajectorySpliter(Trajectory oneTrajectory)
        {
            if (oneTrajectory.PointNumber == 0) throw new Exception("轨迹对象中不包含任何轨迹点！");

            double maxTimeInterval = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.split.timeinterval"));
            double maxDistance = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.split.maxdistance"));

            List<Trajectory> splitTrajectory = new List<Trajectory>();
            List<GnssPoint> oneSplitTrajectoryPointList = new List<GnssPoint>();
            oneSplitTrajectoryPointList.Add(oneTrajectory.GetPointByIndex(0));

            int partNumber = 0;

            for (int i = 1; i < oneTrajectory.PointNumber; i++)
            {
                GnssPoint startPoint = oneSplitTrajectoryPointList.Last();
                GnssPoint endPoint = oneTrajectory.GetPointByIndex(i);

                double distance;
                if (oneTrajectory.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                }

                double timeInterval = endPoint.Timestamp - startPoint.Timestamp;

                if (distance < maxDistance && timeInterval < maxTimeInterval)
                {
                    oneSplitTrajectoryPointList.Add(endPoint);
                }
                else
                {
                    foreach (GnssPoint point in oneSplitTrajectoryPointList)
                    {
                        point.ID = point.ID + "_" + partNumber.ToString();
                    }

                    Trajectory newTrajectory = new Trajectory(oneTrajectory.TaxiID + "_" + partNumber.ToString(), oneTrajectory.TrajectoryCoordinate, oneSplitTrajectoryPointList);
                    partNumber += 1;
                    splitTrajectory.Add(newTrajectory);
                    oneSplitTrajectoryPointList.Clear();
                    oneSplitTrajectoryPointList.Add(endPoint);
                }
            }

            if (oneSplitTrajectoryPointList.Count >= 2)
            {
                foreach (GnssPoint point in oneSplitTrajectoryPointList)
                {
                    point.ID = point.ID + "_" + partNumber.ToString();
                }
                Trajectory newTrajectory = new Trajectory(oneTrajectory.TaxiID + "_" + partNumber.ToString(), oneTrajectory.TrajectoryCoordinate, oneSplitTrajectoryPointList);
                partNumber += 1;
                splitTrajectory.Add(newTrajectory);
                oneSplitTrajectoryPointList.Clear();
            }

            return splitTrajectory;
        }
    }
}
