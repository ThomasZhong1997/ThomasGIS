using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;
using ThomasGIS.BaseConfiguration;
using System.Linq;
using ThomasGIS.TrajectoryPackage.Order;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Preprocess
{
    public static class Preprocessor
    {
        // 移除时间相同的点以及位置漂移点
        public static void RemoveErrorPoint(ITrajectory trajectory, double maxSpeed = -1)
        {
            if (maxSpeed == -1)
            {
                maxSpeed = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.filter.maxspeed"));
            }

            int i = 0;
            while (i < trajectory.GetPointNumber() - 1)
            {
                GnssPoint startPoint = trajectory.GetPointByIndex(i);
                GnssPoint endPoint = trajectory.GetPointByIndex(i + 1);

                // 移除时间相同点
                if (endPoint.Timestamp - startPoint.Timestamp <= 0)
                {
                    trajectory.RemovePoint(i + 1);
                    continue;
                }

                // 移除漂移速度过大点
                double distance;
                if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                }

                double timeInterval = endPoint.Timestamp - startPoint.Timestamp;
                double speed = distance / timeInterval;
                if (speed > maxSpeed)
                {
                    trajectory.RemovePoint(i + 1);
                    continue;
                }

                i++;
            }
        }

        // 移除连续的位置相同点，若有两个及以上的位置相同点则至少保留两个，以保证后续可测算出停留时间
        public static void RemoveSameLocationPoint(ITrajectory trajectory)
        {
            int i = 0;
            while (i < trajectory.GetPointNumber() - 2)
            {
                GnssPoint prevPoint = trajectory.GetPointByIndex(i);
                GnssPoint middlePoint = trajectory.GetPointByIndex(i + 1);
                GnssPoint nextPoint = trajectory.GetPointByIndex(i + 2);

                double distance_1;
                double distance_2;

                if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance_1 = DistanceCalculator.SpatialDistanceGeo(prevPoint, middlePoint);
                    distance_2 = DistanceCalculator.SpatialDistanceGeo(middlePoint, nextPoint);
                }
                else
                {
                    distance_1 = DistanceCalculator.SpatialDistance(prevPoint, middlePoint);
                    distance_2 = DistanceCalculator.SpatialDistance(middlePoint, nextPoint);
                }

                if (distance_1 == distance_2)
                {
                    trajectory.RemovePoint(i + 1);
                    continue;
                }

                i++;
            }
        }

        // 移除乒乓效应点
        // 基于移动速度的检测方法 = RemoveErrorPoint方法
        public static void RemovePingpangPoint_Speed(ITrajectory trajectory, double maxSpeed=-1)
        {
            if (maxSpeed == -1)
            {
                maxSpeed = Convert.ToDouble(Configuration.GetConfiguration("trajectory.quickio.filter.maxspeed"));
            }
            RemoveErrorPoint(trajectory, maxSpeed);
        }

        // 基于重复跳转的检测方法，该方法仅适用于两点重复跳转
        // 未测试
        public static void RemovePingpangPoint_JumpFrequency(ITrajectory trajectory)
        {
            // 先移除位置连续出现的点如 AAABBBABABABA -> AABBABABABA
            RemoveSameLocationPoint(trajectory);

            int i = 0;
            while (i < trajectory.GetPointNumber() - 1)
            {
                // 找到一组特征
                GnssPoint startPoint = trajectory.GetPointByIndex(i);
                GnssPoint endPoint = trajectory.GetPointByIndex(i + 1);

                string token_1 = $"{startPoint.X}-{startPoint.Y}";
                string token_2 = $"{endPoint.X}-{endPoint.Y}";

                // 向后找特征
                int j = i + 2;
                while (j < trajectory.GetPointNumber() - 1)
                {
                    GnssPoint patternStartPoint = trajectory.GetPointByIndex(j);
                    GnssPoint patternEndPoint = trajectory.GetPointByIndex(j + 1);

                    string p_token_1 = $"{patternStartPoint.X}-{patternStartPoint.Y}";
                    string p_token_2 = $"{patternEndPoint.X}-{patternEndPoint.Y}";

                    if (token_1 == p_token_1 && token_2 == p_token_2)
                    {
                        j += 2;
                        continue;
                    }
                    else if (token_1 == p_token_1 && token_2 != p_token_2)
                    {
                        j += 1;
                        break;
                    }
                    else
                    {
                        break;
                    }
                }

                // 由 i - j 中的点构成了一组重复Pattern，如ABABABABAB
                // 此时移除 i 与 j 间的全部轨迹点
                int removeNumber = j - i - 2;
                if (removeNumber > 0)
                {
                    while (removeNumber > 0)
                    {
                        trajectory.RemovePoint(i + 1);
                        removeNumber -= 1;
                    }
                    continue;
                }

                i += 1;
            }
        }

        // 基于频繁转向的检测方法，若轨迹中产生连续的强烈方向角变化，则删除
        // 未测试
        public static void RemovePingpangPoint_TurnFrequency(ITrajectory trajectory, double A0, int timeWindow=3600, int maxTurnTime=6)
        {
            int i = 0;
            while (i < trajectory.GetPointNumber() - 2)
            {
                GnssPoint prevPoint = trajectory.GetPointByIndex(i);
                GnssPoint middlePoint = trajectory.GetPointByIndex(i + 1);
                GnssPoint nextPoint = trajectory.GetPointByIndex(i + 2);

                double angle_1;
                double angle_2;
                if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                {
                    angle_1 = DistanceCalculator.DirectionAngleGeo(prevPoint, middlePoint);
                    angle_2 = DistanceCalculator.DirectionAngleGeo(middlePoint, nextPoint);
                }
                else
                {
                    angle_1 = DistanceCalculator.DirectionAngle(prevPoint, middlePoint);
                    angle_2 = DistanceCalculator.DirectionAngle(middlePoint, nextPoint);
                }

                double dAngle = Math.Abs(angle_2 - angle_1);
                if (dAngle > 180)
                {
                    dAngle = Math.Abs(dAngle - 360);
                }

                // 出现角度过大的
                if (dAngle > A0)
                {
                    int count = 0;
                    double baseTimestamp = middlePoint.Timestamp;

                    int j = i + 1;
                    while (j < trajectory.GetPointNumber() - 2)
                    {
                        GnssPoint patternPrevPoint = trajectory.GetPointByIndex(i);
                        GnssPoint patternMiddlePoint = trajectory.GetPointByIndex(i + 1);
                        GnssPoint patternNextPoint = trajectory.GetPointByIndex(i + 2);

                        // 超过时间窗口就退出
                        if (patternMiddlePoint.Timestamp - baseTimestamp > timeWindow)
                        {
                            break;
                        }

                        // 计算两条线间的夹角
                        double p_angle_1;
                        double p_angle_2;
                        if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                        {
                            p_angle_1 = DistanceCalculator.DirectionAngleGeo(patternPrevPoint, patternMiddlePoint);
                            p_angle_2 = DistanceCalculator.DirectionAngleGeo(patternMiddlePoint, patternNextPoint);
                        }
                        else
                        {
                            p_angle_1 = DistanceCalculator.DirectionAngle(patternPrevPoint, patternMiddlePoint);
                            p_angle_2 = DistanceCalculator.DirectionAngle(patternMiddlePoint, patternNextPoint);
                        }

                        double p_dAngle = Math.Abs(p_angle_2 - p_angle_1);
                        if (p_dAngle > 180)
                        {
                            p_dAngle = Math.Abs(p_dAngle - 360);
                        }

                        // 大于阈值就计数，小于阈值就停
                        if (p_dAngle > A0)
                        {
                            count += 1;
                            j += 1;
                        }
                        else
                        {
                            break;
                        }
                    }

                    // 如果有6个连续的大角度变化则表示发生了兵乓效应，然就就删除呗
                    if (count > maxTurnTime)
                    {
                        while (count > 0)
                        {
                            trajectory.RemovePoint(i + 1);
                            count -= 1;
                        }
                        continue;
                    }
                }

                i += 1;
            }
        }


        private static List<GnssPoint> Resample_Time_Core(ITrajectory trajectory, double timeInterval)
        {
            List<GnssPoint> newPointList = new List<GnssPoint>();

            double startTimestamp = trajectory.GetPointByIndex(0).Timestamp;
            double endTimestamp = trajectory.GetPointByIndex(-1).Timestamp;

            int i = 0;
            for (double nowTimestamp = startTimestamp; nowTimestamp < endTimestamp; nowTimestamp += timeInterval)
            {
                while (i < trajectory.GetPointNumber())
                {
                    GnssPoint startPoint = trajectory.GetPointByIndex(i);
                    GnssPoint endPoint = trajectory.GetPointByIndex(i + 1);

                    if (startPoint.Timestamp <= nowTimestamp && nowTimestamp < endPoint.Timestamp)
                    {
                        double percent = (nowTimestamp - startPoint.Timestamp) / (endPoint.Timestamp - startPoint.Timestamp);
                        double trueX = startPoint.X + percent * (endPoint.X - startPoint.X);
                        double trueY = startPoint.Y + percent * (endPoint.Y - startPoint.Y);
                        double distance;
                        double direction;
                        if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                            direction = DistanceCalculator.DirectionAngleGeo(startPoint, endPoint);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                            direction = DistanceCalculator.DirectionAngle(startPoint, endPoint);
                        }
                        double speed = distance / (endPoint.Timestamp - startPoint.Timestamp);
                        newPointList.Add(new GnssPoint(trajectory.GetTaxiID(), trueX, trueY, nowTimestamp, direction, speed, endPoint.ExtraInformation));
                        break;
                    }
                    else
                    {
                        i++;
                    }
                }

            }

            GnssPoint lastPoint = trajectory.GetPointByIndex(-1);
            newPointList.Add(new GnssPoint(trajectory.GetTaxiID(), lastPoint.X, lastPoint.Y, lastPoint.Timestamp, lastPoint.Direction, lastPoint.Speed, lastPoint.ExtraInformation));

            return newPointList;
        }

        // 等时间间隔重采样
        public static void Resample_Time(ITrajectory trajectory, double timeInterval)
        {
            List<GnssPoint> newPointList = Resample_Time_Core(trajectory, timeInterval);
            trajectory.Clear();
            trajectory.AddPoints(newPointList);
        }

        public static ITrajectory Resample_Time2(ITrajectory trajectory, double timeInterval)
        {
            List<GnssPoint> newPointList = Resample_Time_Core(trajectory, timeInterval);
            return new Trajectory(trajectory.GetTaxiID(), trajectory.GetCoordinateSystem(), newPointList);
        }

        private static List<GnssPoint> Resample_Distance_Core(ITrajectory trajectory, double distanceInterval)
        {
            List<GnssPoint> newPointList = new List<GnssPoint>();

            double totalDistance = trajectory.GetLength();
            double sumDistance = 0;

            int i = 0;
            for (double nowDistance = 0; nowDistance < totalDistance; nowDistance += distanceInterval)
            {
                while (i < trajectory.GetPointNumber())
                {
                    GnssPoint startPoint = trajectory.GetPointByIndex(i);
                    GnssPoint endPoint = trajectory.GetPointByIndex(i + 1);

                    double distance;
                    double direction;
                    if (trajectory.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
                        direction = DistanceCalculator.DirectionAngleGeo(startPoint, endPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance(startPoint, endPoint);
                        direction = DistanceCalculator.DirectionAngle(startPoint, endPoint);
                    }

                    if (sumDistance + distance > nowDistance)
                    {
                        double percent = (nowDistance - sumDistance) / distance;
                        double trueX = startPoint.X + percent * (endPoint.X - startPoint.X);
                        double trueY = startPoint.Y + percent * (endPoint.Y - startPoint.Y);
                        double speed = distance / (endPoint.Timestamp - startPoint.Timestamp);
                        double nowTimestamp = startPoint.Timestamp + percent * (endPoint.Timestamp - startPoint.Timestamp);
                        newPointList.Add(new GnssPoint(trajectory.GetTaxiID(), trueX, trueY, nowTimestamp, direction, speed, endPoint.ExtraInformation));
                        break;
                    }
                    else
                    {
                        sumDistance += distance;
                        i += 1;
                    }
                }
            }

            return newPointList;
        }

        // 等距离间隔重采样
        public static void Resample_Distance(ITrajectory trajectory, double distanceInterval)
        {
            List<GnssPoint> newPointList = Resample_Distance_Core(trajectory, distanceInterval);
            trajectory.Clear();
            trajectory.AddPoints(newPointList);
        }

        public static ITrajectory Resample_Distance2(ITrajectory trajectory, double distanceInterval)
        {
            List<GnssPoint> newPointList = Resample_Distance_Core(trajectory, distanceInterval);
            return new Trajectory(trajectory.GetTaxiID(), trajectory.GetCoordinateSystem(), newPointList);
        }

        public static ITrajectoryOrderSet SimplifyTrajectoryToOrder(ITrajectorySet trajectorySet, ThomasGISTransportMode inputMode = ThomasGISTransportMode.FloatCar)
        {
            ITrajectoryOrderSet newOrderSet = new TrajectoryOrderSet();
            foreach (ITrajectory oneTrajectory in trajectorySet.GetTrajectoryEnumerable())
            {
                GnssPoint startPoint = oneTrajectory.GetPointByIndex(0);
                GnssPoint endPoint = oneTrajectory.GetPointByIndex(-1);
                double distance = oneTrajectory.GetLength();
                string trajectoryID = oneTrajectory.GetTaxiID();
                double timeInteval = endPoint.Timestamp - startPoint.Timestamp;
                TrajectoryOrder newOrder = new TrajectoryOrder(trajectoryID, startPoint, endPoint, startPoint.Timestamp, endPoint.Timestamp, oneTrajectory.GetCoordinateSystem(), distance, distance / timeInteval, -1, inputMode);
                newOrderSet.AddOrder(newOrder);
            }
            return newOrderSet;
        }
    }
}
