using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;
using ThomasGIS.TrajectoryPackage.Preprocess;

namespace ThomasGIS.TrajectoryPackage.QuickIO
{
    public class AISTrajectorySetCreator : ITrajectorySetCreator
    {
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

        public virtual void PointFilter(Trajectory oneTrajectory)
        {
            int i = 0;
            while (i < oneTrajectory.PointNumber - 1)
            {
                GnssPoint startPoint = oneTrajectory.GetPointByIndex(i);
                GnssPoint endPoint = oneTrajectory.GetPointByIndex(i + 1);
                double distance;
                if (oneTrajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                }

                if (distance < 10 && endPoint.Timestamp == startPoint.Timestamp)
                {
                    oneTrajectory.RemovePoint(i + 1);
                }
                else
                {
                    i += 1;
                }
            }
            return;
        }

        public virtual GnssPoint Reader(string line)
        {
            string[] items = line.Split('~');
            string ID = items[0];
            double X = Convert.ToDouble(items[4]) / 600000.0;
            double Y = Convert.ToDouble(items[5]) / 600000.0;
            //double X = Convert.ToDouble(items[4]);
            //double Y = Convert.ToDouble(items[5]);
            double timeStamp = Convert.ToDouble(items[3]);

            Dictionary<string, object> innerData = new Dictionary<string, object>();
            innerData.Add("aistype", items[2]);

            return new GnssPoint(ID, X, Y, timeStamp, innerData);
        }

        private double JudgeJumpPercent(Trajectory oneTrajectory)
        {
            CoordinateType coordinateType = oneTrajectory.GetCoordinateSystem().GetCoordinateType();
            int jumpCount = 0;

            for (int j = 0; j < oneTrajectory.PointNumber - 1; j++)
            {
                GnssPoint startPoint = oneTrajectory.GetPointByIndex(j);
                GnssPoint endPoint = oneTrajectory.GetPointByIndex(j + 1);
                double distance;
                if (coordinateType == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                }

                double timeInterval = endPoint.Timestamp - startPoint.Timestamp;
                if (timeInterval == 0)
                {
                    if (distance > 100.0)
                    {
                        jumpCount += 1;
                    }
                }
                else
                {
                    if (distance > 2000.0)
                    {
                        jumpCount += 1;
                    }
                }
            }

            double jumpPercent = (double)jumpCount / (double)(oneTrajectory.PointNumber - 1);

            return jumpPercent;
        }

        public virtual IEnumerable<Trajectory> TrajectorySpliter(Trajectory oneTrajectory)
        {
            CoordinateType coordinateType = oneTrajectory.GetCoordinateSystem().GetCoordinateType();

            double maxTimeInterval = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.split.timeinterval"));
            double maxDistance = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.split.maxdistance"));
            double maxSpeed = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.filter.maxspeed"));

            List<Trajectory> result = new List<Trajectory>();

            if (oneTrajectory.PointNumber == 0) throw new Exception("轨迹对象中不包含任何轨迹点！");

            bool[] visited = new bool[oneTrajectory.PointNumber];
            for (int i = 0; i < oneTrajectory.PointNumber; i++)
            {
                if (visited[i]) continue;
                List<GnssPoint> tempGnssPointList = new List<GnssPoint>();
                tempGnssPointList.Add(oneTrajectory.GetPointByIndex(i));

                for (int j = i + 1; j < oneTrajectory.PointNumber; j++)
                {
                    if (visited[j]) continue;
                    GnssPoint prevPoint = tempGnssPointList[tempGnssPointList.Count - 1];
                    GnssPoint nowPoint = oneTrajectory.GetPointByIndex(j);

                    double distance;
                    if (coordinateType == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(prevPoint, nowPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(prevPoint, nowPoint);
                    }

                    double timeInterval = nowPoint.Timestamp - prevPoint.Timestamp;

                    if (timeInterval == 0) continue;

                    double speed = distance / timeInterval;

                    if (speed < maxSpeed && distance < maxDistance && timeInterval < maxTimeInterval)
                    {
                        tempGnssPointList.Add(nowPoint);
                        visited[j] = true;
                    }
                }

                if (tempGnssPointList.Count >= 5)
                {
                    Trajectory newTrajectory = new Trajectory(oneTrajectory.TaxiID, oneTrajectory.GetCoordinateSystem(), tempGnssPointList);
                    double jumpPercent = JudgeJumpPercent(newTrajectory);
                    if (newTrajectory != null && jumpPercent < 0.1)
                    {
                        result.Add(newTrajectory);
                    }
                }
            }

            if (result.Count == 0) return result;

            // 轨迹段归并
            List<List<Trajectory>> mergedTrajectoryList = new List<List<Trajectory>>();
            mergedTrajectoryList.Add(new List<Trajectory>());
            mergedTrajectoryList[0].Add(result[0]);

            // 从 1 开始，查看和先前所有轨迹段间的关系
            for (int i = 1; i < result.Count; i++)
            {
                Trajectory nowTrajectory = result[i];
                double minDistance = double.MaxValue;
                int targetMergeIndex = -1;
                for (int j = 0; j < mergedTrajectoryList.Count; j++)
                {
                    Trajectory prevTrajectory = mergedTrajectoryList[j][mergedTrajectoryList[j].Count - 1];
                    GnssPoint prevEndPoint = prevTrajectory.GetPointByIndex(-1);
                    GnssPoint nowStartPoint = nowTrajectory.GetPointByIndex(0);
                    double timeInterval = nowStartPoint.Timestamp - prevEndPoint.Timestamp;
                    double distance;
                    if (coordinateType == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(prevEndPoint, nowStartPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(prevEndPoint, nowStartPoint);
                    }

                    if (timeInterval <= 0)
                    {
                        continue;
                    }

                    double speed = distance / timeInterval;

                    if (speed > maxSpeed)
                    {
                        continue;
                    }

                    if (distance < minDistance)
                    {
                        minDistance = speed;
                        targetMergeIndex = j;
                    }
                }

                if (targetMergeIndex == -1)
                {
                    List<Trajectory> newMerge = new List<Trajectory>();
                    newMerge.Add(nowTrajectory);
                    mergedTrajectoryList.Add(newMerge);
                }
                else
                {
                    mergedTrajectoryList[targetMergeIndex].Add(nowTrajectory);
                }
            }

            result.Clear();
            int partCount = 0;
            string taxiID = oneTrajectory.GetTaxiID();
            for (int i = 0; i < mergedTrajectoryList.Count; i++)
            {
                string suffix = "_" + partCount.ToString();
                List<GnssPoint> newTrajectoryPointList = new List<GnssPoint>();
                for (int j = 0; j < mergedTrajectoryList[i].Count; j++)
                {
                    for (int k = 0; k < mergedTrajectoryList[i][j].PointNumber; k++)
                    {
                        GnssPoint gnssPoint = mergedTrajectoryList[i][j].GetPointByIndex(k);
                        // gnssPoint.ID = gnssPoint.ID.Insert(gnssPoint.ID.Length, suffix);
                        newTrajectoryPointList.Add(gnssPoint);
                    }
                }
                Trajectory newTrajectory = new Trajectory(oneTrajectory.TaxiID + "_" + partCount.ToString(), oneTrajectory.TrajectoryCoordinate, newTrajectoryPointList);
                if (newTrajectory != null && newTrajectory.Length > 50000)
                {
                    result.Add(newTrajectory);
                    partCount += 1;
                }
            }

            for (int i = 0; i < result.Count; i++)
            {
                double jumpPercent = JudgeJumpPercent(result[i]);
                if (jumpPercent > 0.1)
                {
                    result.RemoveAt(i);
                    i -= 1;
                }
            }

            return result;
        }
    }
}
