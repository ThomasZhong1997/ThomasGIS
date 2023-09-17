using System;
using System.Collections.Generic;
using ThomasGIS.Geometries;
using ThomasGIS.Network;
using ThomasGIS.SpatialIndex;
using ThomasGIS.Vector;
using System.Linq;
using ThomasGIS.Helpers;
using ThomasGIS.Coordinates;
using System.Diagnostics;

namespace ThomasGIS.TrajectoryPackage.MapMatch
{
    public class HMMRouteCase
    {
        public double possibility;
        public List<GnssPoint> pointList;
        public List<int> segmentList;
        public double sumDistance;

        public HMMRouteCase()
        {
            this.possibility = 0;
            this.pointList = new List<GnssPoint>();
            this.segmentList = new List<int>();
            this.sumDistance = 0;
        }

        // 深拷贝一份
        public HMMRouteCase(List<GnssPoint> points, List<int> segments, double possibility, double sumDistance)
        {
            this.possibility = possibility;
            this.pointList = new List<GnssPoint>();
            this.segmentList = new List<int>();
            this.pointList.AddRange(points);
            this.segmentList.AddRange(segments);
            this.sumDistance = sumDistance;
        }
    }

    // 数据结构，用于存储候选点的位置与条件概率
    public class CandicatePoint
    {
        public Point point;
        public double conditionPossibility;
        public int segmentIndex;

        public CandicatePoint(double x, double y, double conditionPossibility, int segmentIndex)
        {
            this.point = new Point(x, y);
            this.conditionPossibility = conditionPossibility;
            this.segmentIndex = segmentIndex;
        }
    }


    // HMM，观测概率为点到直线的距离，转移概率为最短路径长度与点连线长度之比
    public class HMMMapMatch : IMapMatch
    {
        private IGeoNetwork geoNetwork = null;

        private ISpatialIndex arcSpatialIndex = null;

        private double Linear(double x)
        {
            if (x <= 1)
            {
                return 1.0;
            }
            else
            {
                return 1.0 / Math.Sqrt(x);
            }
        }

        private double Linear2(double x)
        {
            if (x <= 1)
            {
                return 1.0;
            }
            else
            {
                return 1.0 / x;
            }
        }

        public HMMMapMatch(IShapefile shapefile, double scale, string powerField = "", BoundaryBox boundary = null, string directionField = "")
        {
            if (directionField == "")
            {
                this.geoNetwork = new GeoNetwork(shapefile);
            }
            else
            {
                this.geoNetwork = new GeoDirectedNetwork(shapefile, directionField);
            }

            if (boundary == null)
            {
                arcSpatialIndex = new GridSpatialIndex(shapefile.GetBoundaryBox(), scale, shapefile.GetCoordinateRef());
            }
            else
            {
                arcSpatialIndex = new GridSpatialIndex(boundary, scale, shapefile.GetCoordinateRef());
            }

            for (int i = 0; i < geoNetwork.GetArcNumber(); i++)
            {
                // 需要把NetworkArc对象转为SingleLine对象
                
                this.arcSpatialIndex.AddItem(geoNetwork.GetGeoNetworkArc(i));
            }

            this.arcSpatialIndex.RefreshIndex();

            this.geoNetwork.RefreshNeighborMatrix(powerField);
        }

        // MapMatch核心方法，将轨迹点对象转为Route对象，Route中的点集即为新的Trajectory
        public MapMatchResult MatchRoute(ITrajectory trajectory, double range, int K)
        {
            if (trajectory.GetCoordinateSystem().GetCoordinateType() != this.geoNetwork.GetGeoNetworkCoordinate().GetCoordinateType())
            {
                throw new Exception("轨迹与路网的坐标系不一致！");
            }

            CoordinateType coordinateType = this.geoNetwork.GetGeoNetworkCoordinate().GetCoordinateType();

            List<HMMRouteCase> KRouteList = new List<HMMRouteCase>();

            // 首先找到第一个缓冲范围内有道路的Gnss轨迹点
            int i = 0;
            IEnumerable<int> candicateSegmentIndex;
            GnssPoint prevPoint;
            do
            {
                prevPoint = trajectory.GetPointByIndex(i);
                candicateSegmentIndex = arcSpatialIndex.SearchID(prevPoint, range);
                i += 1;
            } while (candicateSegmentIndex.Count() == 0 && i < trajectory.GetPointNumber());

            
            IEnumerable<CandicatePoint> selectedPoints = getCandicatePoints(candicateSegmentIndex, prevPoint, K, trajectory.GetCoordinateSystem());

            // HMMRouteCase里
            foreach (CandicatePoint candicatePoint in selectedPoints)
            {
                HMMRouteCase oneCase = new HMMRouteCase();
                oneCase.possibility = candicatePoint.conditionPossibility;
                oneCase.pointList.Add(new GnssPoint(trajectory.GetTaxiID(), candicatePoint.point.X, candicatePoint.point.Y, prevPoint.Timestamp));
                oneCase.segmentList.Add(candicatePoint.segmentIndex);
                KRouteList.Add(oneCase);
            }

            MapMatchResult result = new MapMatchResult();

            double tempTrajectorySumDistance = 0;

            for (; i < trajectory.GetPointNumber(); i++)
            {
                GnssPoint nextPoint = trajectory.GetPointByIndex(i);

                if (KRouteList.Count == 0) break;

                double maxRoutePointLength = KRouteList[0].pointList.Count;
                for (int j = 1; j < KRouteList.Count; j++)
                {
                    if (KRouteList[j].pointList.Count > maxRoutePointLength)
                    {
                        maxRoutePointLength = KRouteList[j].pointList.Count;
                    }
                }

                if (maxRoutePointLength > 5000 || i % 200 == 0)
                {
                    double tempMaxPossibility = KRouteList.Max(item => item.possibility);

                    for (int j = 0; j < KRouteList.Count; j++)
                    {
                        if (KRouteList[j].possibility == tempMaxPossibility)
                        {
                            HMMRouteCase keepedCase = KRouteList[j];
                            result.gnssPointList.AddRange(keepedCase.pointList);
                            result.gnssPointList.RemoveAt(result.gnssPointList.Count - 1);
                            result.segmentList.AddRange(keepedCase.segmentList);
                            result.segmentList.RemoveAt(result.segmentList.Count - 1);
                            KRouteList.Clear();
                            HMMRouteCase reBuildCase = new HMMRouteCase(new List<GnssPoint>(), new List<int>(), 1.0, keepedCase.sumDistance);
                            reBuildCase.pointList.Add(keepedCase.pointList[keepedCase.pointList.Count - 1]);
                            reBuildCase.segmentList.Add(keepedCase.segmentList[keepedCase.segmentList.Count - 1]);
                            KRouteList.Add(reBuildCase);
                            break;
                        }
                    }
                }

                // 实际轨迹连线的距离
                double trajectoryDistance;
                if (coordinateType == CoordinateType.Geographic)
                {
                    trajectoryDistance = DistanceCalculator.SpatialDistanceGeo(trajectory.GetPointByIndex(i), prevPoint);
                }
                else
                {
                    trajectoryDistance = DistanceCalculator.SpatialDistance(trajectory.GetPointByIndex(i), prevPoint);
                }

                if (trajectoryDistance <= 100 && i < trajectory.GetPointNumber() - 1) continue;

                // 目前遍历的轨迹长度
                tempTrajectorySumDistance += trajectoryDistance;

                if (coordinateType == CoordinateType.Geographic)
                {
                    nextPoint.Direction = DistanceCalculator.DirectionAngleGeo(prevPoint, nextPoint);
                }
                else
                {
                    nextPoint.Direction = DistanceCalculator.DirectionAngle(prevPoint, nextPoint);
                }

                // 有N个候选路段
                candicateSegmentIndex = arcSpatialIndex.SearchID(nextPoint, range);

                if (candicateSegmentIndex.Count() == 0) continue;

                // 老规矩先选K个可能性最大的出来
                selectedPoints = getCandicatePoints(candicateSegmentIndex, nextPoint, K, trajectory.GetCoordinateSystem());

                List<HMMRouteCase> newKCase = new List<HMMRouteCase>();

                // 向前剪枝，因为如果多个case趋向于同一个终点，那么概率最大的就是结果了，其余的可以删除
                // 因此KRouteList中应当包含的是最终节点不同的当前最优解
                for (int j = 0; j < selectedPoints.Count(); j++)
                {
                    CandicatePoint nowPoint = selectedPoints.ElementAt(j);

                    List<double> transferProbabilityList = new List<double>();
                    List<Route> routeList = new List<Route>();

                    for (int k = 0; k < KRouteList.Count; k++)
                    {
                        HMMRouteCase oneCase = KRouteList[k];
                        int beforeSegmentIndex = oneCase.segmentList.Last();
                        IPoint beforePoint = oneCase.pointList.Last();

                        if (beforeSegmentIndex == nowPoint.segmentIndex)
                        {
                            // 投影点间的距离
                            double segmentDistance;


                            if (coordinateType == CoordinateType.Geographic)
                            {
                                segmentDistance = DistanceCalculator.SpatialDistanceGeo(beforePoint, nowPoint.point);
                            }
                            else
                            {
                                segmentDistance = DistanceCalculator.SpatialDistance(beforePoint, nowPoint.point);
                            }

                            if (segmentDistance == 0)
                            {
                                routeList.Add(new Route(new List<INetworkNode>(), new List<int>(), 0, true));
                                transferProbabilityList.Add(0);
                                continue;
                            }

                            // 转移概率：实际轨迹长度 ÷ 匹配长度
                            double transferPossibility = tempTrajectorySumDistance - (segmentDistance + oneCase.sumDistance);

                            transferProbabilityList.Add(Linear2(Math.Abs(transferPossibility)));

                            Route newRoute = new Route(new List<INetworkNode>(), new List<int>(), segmentDistance, true);

                            newRoute.RouteNodes.Add(new GeoNetworkNode(-1, beforePoint.GetX(), beforePoint.GetY(), null));
                            newRoute.RouteNodes.Add(new GeoNetworkNode(-1, nowPoint.point.X, nowPoint.point.Y, null));
                            newRoute.RouteArcs.Add(nowPoint.segmentIndex);

                            routeList.Add(newRoute);
                        }
                        // 处于不同道路则要计算最短路径，节点与节点间的最短路径
                        else
                        {
                            GeoNetworkArc beforeArc = geoNetwork.GetGeoNetworkArc(beforeSegmentIndex);
                            GeoNetworkArc nowArc = geoNetwork.GetGeoNetworkArc(nowPoint.segmentIndex);

                            int startNodeIndex = -1, endNodeIndex = -1;
                            double startMinDistance = -1, endMinDistance = -1;

                            if (this.geoNetwork.GetType() == typeof(GeoNetwork))
                            {
                                double distanceSS;
                                double distanceSE;
                                double distanceES;
                                double distanceEE;

                                // 看两条路段的起点和终点间的距离关系，选最小的
                                if (coordinateType == CoordinateType.Geographic)
                                {
                                    distanceSS = DistanceCalculator.SpatialDistanceGeo(beforePoint, beforeArc.GetStartPoint());
                                    distanceSE = DistanceCalculator.SpatialDistanceGeo(beforePoint, beforeArc.GetEndPoint());
                                    distanceES = DistanceCalculator.SpatialDistanceGeo(nowPoint.point, nowArc.GetStartPoint());
                                    distanceEE = DistanceCalculator.SpatialDistanceGeo(nowPoint.point, nowArc.GetEndPoint());
                                }
                                else
                                {
                                    distanceSS = DistanceCalculator.SpatialDistance(beforePoint, beforeArc.GetStartPoint());
                                    distanceSE = DistanceCalculator.SpatialDistance(beforePoint, beforeArc.GetEndPoint());
                                    distanceES = DistanceCalculator.SpatialDistance(nowPoint.point, nowArc.GetStartPoint());
                                    distanceEE = DistanceCalculator.SpatialDistance(nowPoint.point, nowArc.GetEndPoint());
                                }

                                if (distanceSS < distanceSE)
                                {
                                    startNodeIndex = beforeArc.GetStartNodeID();
                                    startMinDistance = distanceSS;
                                }
                                else
                                {
                                    startNodeIndex = beforeArc.GetEndNodeID();
                                    startMinDistance = distanceSE;
                                }

                                if (distanceES < distanceEE)
                                {
                                    endNodeIndex = nowArc.GetStartNodeID();
                                    endMinDistance = distanceES;
                                }
                                else
                                {
                                    endNodeIndex = nowArc.GetEndNodeID();
                                    endMinDistance = distanceEE;
                                }
                            }
                            else if (this.geoNetwork.GetType() == typeof(GeoDirectedNetwork))
                            {

                                startNodeIndex = beforeArc.GetEndNodeID();
                                endNodeIndex = nowArc.GetStartNodeID();


                                if (coordinateType == CoordinateType.Geographic)
                                {
                                    startMinDistance = DistanceCalculator.SpatialDistanceGeo(beforePoint, beforeArc.GetEndPoint());
                                    endMinDistance = DistanceCalculator.SpatialDistanceGeo(nowPoint.point, nowArc.GetStartPoint());
                                }
                                else
                                {
                                    startMinDistance = DistanceCalculator.SpatialDistance(beforePoint, beforeArc.GetEndPoint());
                                    endMinDistance = DistanceCalculator.SpatialDistance(nowPoint.point, nowArc.GetStartPoint());
                                }
                            }

                            // 如果起始点和终止点相同Index，则最短路径为 实际起点 -> Index -> 实际终点
                            if (startNodeIndex == endNodeIndex)
                            {
                                Route route = new Route(new List<GeoNetworkNode>(), new List<int>(), startMinDistance + endMinDistance, true);
                                if (route.Impedance == 0)
                                {
                                    routeList.Add(route);
                                    transferProbabilityList.Add(0);
                                    continue;
                                }
                                route.RouteNodes.Add(new GeoNetworkNode(-1, beforePoint.GetX(), beforePoint.GetY(), null));
                                route.RouteNodes.Add(this.geoNetwork.GetGeoNetworkNode(startNodeIndex));
                                route.RouteNodes.Add(new GeoNetworkNode(-1, nowPoint.point.X, nowPoint.point.Y, null));
                                route.RouteArcs.Add(beforeSegmentIndex);
                                route.RouteArcs.Add(nowPoint.segmentIndex);
                                routeList.Add(route);
                                transferProbabilityList.Add(Linear2(Math.Abs(tempTrajectorySumDistance - (route.Impedance + oneCase.sumDistance))));
                            }
                            else
                            {
                                Route route = this.geoNetwork.GreedySolvePath(startNodeIndex, endNodeIndex, null);

                                if (route.RouteNodes.Count == 0)
                                {
                                    routeList.Add(route);
                                    transferProbabilityList.Add(0);
                                    continue;
                                }

                                // 相同要减，不同要加，理论上算出来的不应该包含前一个和后一个点所在的SegmentIndex
                                int firstSegmentIndex = route.RouteArcs.First();
                                if (firstSegmentIndex == beforeSegmentIndex)
                                {
                                    route.Impedance -= startMinDistance;
                                    route.RouteArcs.RemoveAt(0);
                                    route.RouteNodes.RemoveAt(0);
                                }
                                else
                                {
                                    route.Impedance += startMinDistance;
                                }

                                if (startMinDistance != 0)
                                {
                                    route.RouteNodes.Insert(0, new GeoNetworkNode(-1, beforePoint.GetX(), beforePoint.GetY(), null));
                                }

                                if (route.RouteArcs.Count != 0 && route.RouteArcs.Last() == nowPoint.segmentIndex)
                                {
                                    route.Impedance -= endMinDistance;
                                    route.RouteNodes.RemoveAt(route.RouteNodes.Count - 1);
                                }
                                else
                                {
                                    route.Impedance += endMinDistance;
                                    route.RouteArcs.Add(nowPoint.segmentIndex);
                                }

                                if (endMinDistance != 0)
                                {
                                    route.RouteNodes.Add(new GeoNetworkNode(-1, nowPoint.point.X, nowPoint.point.Y, null));
                                }

                                routeList.Add(route);
                                transferProbabilityList.Add(Linear2(Math.Abs(tempTrajectorySumDistance - (route.Impedance + oneCase.sumDistance))));
                            }
                        }
                    }

                    double totalTransferPossibility = 0;
                    for (int k = 0; k < routeList.Count; k++)
                    {
                        totalTransferPossibility += transferProbabilityList[k];
                    }

                    if (totalTransferPossibility == 0) continue;

                    // 当前概率 = 先前概率 * 转移概率 * 发生概率
                    for (int k = 0; k < routeList.Count; k++)
                    {
                        transferProbabilityList[k] /= totalTransferPossibility;
                        // transferProbabilityList[k] *= selectedPoints.ElementAt(j).conditionPossibility;
                        transferProbabilityList[k] *= KRouteList[k].possibility;
                    }

                    // 最大的那个是在当前情况下到达该位置的最有可能性的道路，因此把它取出来

                    if (transferProbabilityList.Max() == 0) continue;

                    int bestSuitableCaseIndex = transferProbabilityList.IndexOf(transferProbabilityList.Max());

                    HMMRouteCase bestCase = KRouteList[bestSuitableCaseIndex];

                    Route bestRoute = routeList[bestSuitableCaseIndex];

                    HMMRouteCase newHMMRouteCase = new HMMRouteCase(bestCase.pointList, bestCase.segmentList, transferProbabilityList[bestSuitableCaseIndex], bestCase.sumDistance + bestRoute.Impedance);

                    double sumDistance = 0;
                    double timeInterval = trajectory.GetPointByIndex(i).Timestamp - trajectory.GetPointByIndex(i - 1).Timestamp;
                    double baseTimestamp = trajectory.GetPointByIndex(i - 1).Timestamp;

                    for (int k = 0; k < bestRoute.RouteNodes.Count - 1; k++)
                    {
                        GeoNetworkNode startNode = bestRoute.RouteNodes[k] as GeoNetworkNode;
                        GeoNetworkNode endNode = bestRoute.RouteNodes[k + 1] as GeoNetworkNode;

                        if (coordinateType == CoordinateType.Geographic)
                        {
                            sumDistance += DistanceCalculator.SpatialDistanceGeo(startNode, endNode);
                        }
                        else
                        {
                            sumDistance += DistanceCalculator.SpatialDistance(startNode, endNode);
                        }

                        double timestamp = baseTimestamp + ((sumDistance / bestRoute.Impedance) * timeInterval);

                        if (timestamp > baseTimestamp)
                        {
                            newHMMRouteCase.pointList.Add(new GnssPoint(endNode.GetID().ToString(), endNode.X, endNode.Y, timestamp));
                        }
                    }

                    for (int k = 0; k < bestRoute.RouteArcs.Count; k++)
                    {
                        if (bestRoute.RouteArcs[k] == newHMMRouteCase.segmentList.Last()) continue;

                        newHMMRouteCase.segmentList.Add(bestRoute.RouteArcs[k]);
                    }

                    newKCase.Add(newHMMRouteCase);
                }

                if (newKCase.Count == 0) continue;

                KRouteList.Clear();
                KRouteList.AddRange(newKCase);
                newKCase.Clear();

                prevPoint = nextPoint;

                if (KRouteList.Count == 0) break;
            }

            // 全部计算完毕后在K条路径中选择概率最高的作为结果
            if (KRouteList.Count == 0) return null;

            double maxPossibility = KRouteList.Max(item => item.possibility);
            for (int j = 0; j < KRouteList.Count; j++)
            {
                if (KRouteList[j].possibility == maxPossibility)
                {
                    HMMRouteCase keepedCase = KRouteList[j];
                    result.gnssPointList.AddRange(keepedCase.pointList);
                    result.segmentList.AddRange(keepedCase.segmentList);
                    return result;
                }
            }

            return null;
        }

        private IEnumerable<CandicatePoint> getCandicatePoints(IEnumerable<int> searchedIndexList, IPoint point, int K, CoordinateBase coordinate)
        {
            List<CandicatePoint> result = new List<CandicatePoint>();

            List<double> distanceList = new List<double>();

            List<int> crossedSegmentList = new List<int>();

            // 找到最近的K个路段构造初始概率
            foreach (int index in searchedIndexList)
            {
                SingleLine candicateSegment = this.arcSpatialIndex.GetItemByIndex(index) as SingleLine;

                // 有交点的才会被保留，然后再用角度和距离计算
                Point crossPoint = DistanceCalculator.CrossPoint2(candicateSegment, point);
                if (crossPoint == null) continue;

                double distance;
                double lineDirection;
                if (coordinate.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateSegment, point) + 0.000001;
                    lineDirection = DistanceCalculator.DirectionAngleGeo(candicateSegment.GetStartPoint(), candicateSegment.GetEndPoint());
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateSegment, point) + 0.000001;
                    lineDirection = DistanceCalculator.DirectionAngle(candicateSegment.GetStartPoint(), candicateSegment.GetEndPoint());
                }

                double pointDirection = (point as GnssPoint).Direction;

                if (pointDirection == -1)
                {
                    distanceList.Add(distance);
                }
                else
                {
                    double dDirection = Math.Abs(lineDirection - pointDirection);
                    if (dDirection > 180.0) dDirection = 360.0 - dDirection;
                    double differ = Math.Cos(dDirection / 180.0 * Math.PI);
                    if (differ <= 0)
                    {
                        continue;
                    }
                    else
                    {
                        distanceList.Add(Math.Sqrt(distance) / differ);
                    }
                }

                crossedSegmentList.Add(index);
            }

            // 选出前K个最短的Case
            for (int j = 0; j < Math.Min(K, crossedSegmentList.Count); j++)
            {
                int minDistanceIndex = distanceList.IndexOf(distanceList.Min());

                // 距离的倒数转为Sigmoid函数，范围永远在[0.5 - 1]
                double basePossibility = Linear(distanceList[minDistanceIndex]);

                // double basePossibility = 1;

                SingleLine candicateSegment = this.arcSpatialIndex.GetItemByIndex(crossedSegmentList[minDistanceIndex]) as SingleLine;
                Point crossPoint = DistanceCalculator.CrossPoint(candicateSegment, point);

                result.Add(new CandicatePoint(crossPoint.X, crossPoint.Y, basePossibility, crossedSegmentList[minDistanceIndex]));

                distanceList[minDistanceIndex] = Double.MaxValue;
            }

            return result;
        }

        public GeoNetworkArc GetGeoNetworkArc(int index)
        {
            return this.geoNetwork.GetGeoNetworkArc(index);
        }
    }
}
