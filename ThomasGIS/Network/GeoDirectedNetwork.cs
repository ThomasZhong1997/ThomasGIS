using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.SpatialIndex;
using ThomasGIS.Vector;

namespace ThomasGIS.Network
{
    public class GeoDirectedNetwork : GeoNetwork, IDirectedNetwork
    {
        public GeoDirectedNetwork(CoordinateBase coordinate) : base(coordinate)
        {
            
        }

        public GeoDirectedNetwork(IShapefile shapefile, string directionField) : base(shapefile.GetCoordinateRef())
        {
            HashSet<string> pointSet = new HashSet<string>();

            // 从shapefile里拿到所有的道路节点构建顶点集合V
            for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
            {
                ShpPolyline polyline = shapefile.GetFeature(i) as ShpPolyline;

                for (int j = 0; j < polyline.PartNumber; j++)
                {
                    int startLoc = polyline.PartList[j];
                    int endLoc;
                    if (j == polyline.PartNumber - 1)
                    {
                        endLoc = polyline.PointNumber;
                    }
                    else
                    {
                        endLoc = polyline.PartList[j + 1];
                    }

                    for (int k = startLoc; k < endLoc; k++)
                    {
                        pointSet.Add($"{ polyline.PointList[k].GetX() },{ polyline.PointList[k].GetY() }");
                    }
                }
            }

            List<string> pointStrList = pointSet.ToList();
            pointSet.Clear();

            BoundaryBox mbr = shapefile.GetBoundaryBox();
            double scale = (mbr.XMax - mbr.XMin) / 1000.0;
            if (shapefile.GetCoordinateRef().GetCoordinateType() == CoordinateType.Geographic)
            {
                scale *= 111000.0;
            }
            
            pointSpatialIndex = new GridSpatialIndex(mbr, scale, shapefile.GetCoordinateRef());

            // 向网络中添加节点
            foreach (string pointString in pointStrList)
            {
                string[] coordinate = pointString.Split(',');
                double x = Convert.ToDouble(coordinate[0]);
                double y = Convert.ToDouble(coordinate[1]);
                GeoNetworkNode newNode = new GeoNetworkNode(this.NodeNumber, x, y, null);
                this.AddNode(newNode);
                pointSpatialIndex.AddItem(newNode);
            }

            // 刷新空间索引内容
            pointSpatialIndex.RefreshIndex();

            // 构建点与边的关系
            for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
            {
                ShpPolyline polyline = shapefile.GetFeature(i) as ShpPolyline;

                // 方向参数
                int direction = shapefile.GetFieldValueAsInt(i, directionField);

                for (int j = 0; j < polyline.PartNumber; j++)
                {
                    int startLoc = polyline.PartList[j];
                    int endLoc;
                    if (j == polyline.PartNumber - 1)
                    {
                        endLoc = polyline.PointNumber;
                    }
                    else
                    {
                        endLoc = polyline.PartList[j + 1];
                    }

                    for (int k = startLoc; k < endLoc - 1; k++)
                    {
                        IPoint startPoint = polyline.PointList[k];
                        IPoint endPoint = polyline.PointList[k + 1];

                        if (startPoint.GetX() == endPoint.GetX() && startPoint.GetY() == endPoint.GetY()) continue;

                        IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(startPoint, 0);
                        IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(endPoint, 0);

                        GeoNetworkNode startNode = null, endNode = null;

                        foreach (int index in startNodeCandicateIndex)
                        {
                            GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                            if (Math.Abs(checkNode.X - startPoint.GetX()) < 0.0000000001 && Math.Abs(checkNode.Y - startPoint.GetY()) < 0.0000000001)
                            {
                                startNode = checkNode;
                                break;
                            }
                        }

                        foreach (int index in endNodeCandicateIndex)
                        {
                            GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                            if (Math.Abs(checkNode.X - endPoint.GetX()) < 0.0000000001 && Math.Abs(checkNode.Y - endPoint.GetY()) < 0.0000000001)
                            {
                                endNode = checkNode;
                                break;
                            }
                        }

                        if (startNode == null || endNode == null) throw new Exception("Big Error!");

                        Dictionary<string, object> dataField = new Dictionary<string, object>();
                        IEnumerable<string> fieldNames = shapefile.GetFieldNames();
                        foreach (string fieldName in fieldNames)
                        {
                            dataField.Add(fieldName, shapefile.GetFieldValueAsString(i, fieldName));
                        }
                        dataField.Add("SegmentID", i);

                        if (direction == 1)
                        {
                            this.AddArc(new GeoNetworkArc(startNode, endNode, dataField));
                            this.AddArc(new GeoNetworkArc(endNode, startNode, dataField));
                        }
                        else if (direction == 2)
                        {
                            this.AddArc(new GeoNetworkArc(startNode, endNode, dataField));
                        }
                        else if (direction == 3)
                        {
                            this.AddArc(new GeoNetworkArc(endNode, startNode, dataField));
                        }
                    }
                }
            }
        }

        public int InDegree(int index)
        {
            int nodeID = this.GetGeoNetworkNode(index).GetID();
            IEnumerable<INetworkArc> inArcList = this.ArcList.Where(item => item.GetEndNodeID() == nodeID);
            return inArcList.Count();
        }

        public int OutDegree(int index)
        {
            int nodeID = this.GetGeoNetworkNode(index).GetID();
            IEnumerable<INetworkArc> inArcList = this.ArcList.Where(item => item.GetStartNodeID() == nodeID);
            return inArcList.Count();
        }

        //有向图的集聚系数
        public override double ClusterCoefficient(int nodeIndex)
        {
            if (neighborMatrix == null || networkChangeFlag == true)
            {
                RefreshNeighborMatrix();
            }

            HashSet<int> neighborNodeIDSet = new HashSet<int>();

            for (int i = 0; i < this.NodeNumber; i++)
            {
                List<LinkValuePair> neighborLinks = this.neighborMatrix[i];

                if (i == nodeIndex)
                {
                    foreach (LinkValuePair pair in neighborLinks)
                    {
                        neighborNodeIDSet.Add(pair.EndNodeIndex);
                    }
                }
                else
                {
                    foreach (LinkValuePair pair in neighborLinks)
                    {
                        if (pair.EndNodeIndex == nodeIndex)
                        {
                            neighborNodeIDSet.Add(i);
                        }
                    }
                }
            }

            int count = 0;

            foreach (int neighborIndex in neighborNodeIDSet)
            {
                List<LinkValuePair> moreLinks = neighborMatrix[neighborIndex];
                foreach (LinkValuePair pair in moreLinks)
                {
                    if (neighborNodeIDSet.Contains(pair.EndNodeIndex))
                    {
                        count++;
                    }
                }
            }

            if (neighborNodeIDSet.Count == 0 || neighborNodeIDSet.Count == 1)
            {
                return 0;
            }

            return (double)count / (double)neighborNodeIDSet.Count / (double)(neighborNodeIDSet.Count - 1);
        }

        // 有向图的刷新邻接矩阵与无向图不同.jpg
        public override void RefreshNeighborMatrix(string dataField = null)
        {
            if (dataField == null)
            {
                dataField = nowMatrixField;
            }

            RefreshGridSpatialIndex();

            // 有多少个点就有多少条记录，记录内的长度不一致
            this.neighborMatrix = new List<List<LinkValuePair>>();
            // 由于邻接矩阵空间占用较大，在放弃前一个矩阵后应当执行GC过程释放内存
            GC.Collect();

            for (int i = 0; i < NodeNumber; i++)
            {
                this.neighborMatrix.Add(new List<LinkValuePair>());
            }

            for (int i = 0; i < this.ArcList.Count; i++)
            {
                GeoNetworkArc oneArc = this.ArcList[i] as GeoNetworkArc;

                IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(new Point(oneArc.StartPoint.GetX(), oneArc.StartPoint.GetY()), this.pointSpatialIndex.GetScale());
                IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(new Point(oneArc.EndPoint.GetX(), oneArc.EndPoint.GetY()), this.pointSpatialIndex.GetScale());

                int startNodeIndex = -1, endNodeIndex = -1;

                foreach (int index in startNodeCandicateIndex)
                {
                    GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                    if (checkNode.X == oneArc.StartPoint.GetX() && checkNode.Y == oneArc.StartPoint.GetY())
                    {
                        startNodeIndex = index;
                        break;
                    }
                }

                foreach (int index in endNodeCandicateIndex)
                {
                    GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                    if (checkNode.X == oneArc.EndPoint.GetX() && checkNode.Y == oneArc.EndPoint.GetY())
                    {
                        endNodeIndex = index;
                        break;
                    }
                }

                if (startNodeIndex == -1 || endNodeIndex == -1) throw new Exception("Big Error!");

                if (dataField != "")
                {
                    // 如果边的属性表中不包含该字段，则直接跳过
                    if (!oneArc.GetKeys().Contains(dataField)) continue;

                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, (double)oneArc.GetProperty(dataField), i));
                }
                else
                {
                    double distance;
                    if (networkCoordinateSystem.GetCoordinateType() == CoordinateType.Projected)
                    {
                        distance = DistanceCalculator.SpatialDistance(oneArc.StartPoint, oneArc.EndPoint);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistanceGeo(oneArc.StartPoint, oneArc.EndPoint);
                    }
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, distance, i));
                }
            }

            this.nowMatrixField = dataField;
            this.networkChangeFlag = false;
        }

        public override Route SolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
        {
            if (powerField == null) powerField = "";

            // GeoNetwork里节点具备地理坐标后重写为A*算法
            if (neighborMatrix == null || networkChangeFlag == true || nowMatrixField != powerField)
            {
                RefreshNeighborMatrix(powerField);
                RefreshGridSpatialIndex();
            }

            IEnumerable<int> candidateStartArcs = segmentSpatialIndex.SearchID(startPoint, tolerateDistance);
            IEnumerable<int> candidateEndArcs = segmentSpatialIndex.SearchID(endPoint, tolerateDistance);

            if (candidateStartArcs.Count() == 0 || candidateEndArcs.Count() == 0) return null;

            double minStartDistance = double.MaxValue;
            int minStartDistanceIndex = -1;

            foreach (int candidateArcIndex in candidateStartArcs)
            {
                GeoNetworkArc candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc;
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, startPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, startPoint);
                }

                if (distance < minStartDistance)
                {
                    minStartDistanceIndex = candidateArcIndex;
                    minStartDistance = distance;
                }
            }

            double minEndDistance = double.MaxValue;
            int minEndDistanceIndex = -1;

            foreach (int candidateArcIndex in candidateEndArcs)
            {
                GeoNetworkArc candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc;
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, endPoint);
                }

                if (distance < minEndDistance)
                {
                    minEndDistanceIndex = candidateArcIndex;
                    minEndDistance = distance;
                }
            }

            GeoNetworkArc startArc = ArcList[minStartDistanceIndex] as GeoNetworkArc;
            GeoNetworkArc endArc = ArcList[minEndDistanceIndex] as GeoNetworkArc;

            IPoint startTruePoint = DistanceCalculator.CrossPoint(startArc, startPoint);
            IPoint endTruePoint = DistanceCalculator.CrossPoint(endArc, endPoint);

            int startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetEndNodeID()));
            int endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetEndNodeID()));

            double startArcDistance, endArcDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startArcDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetEndPoint());
                endArcDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetStartPoint());
            }
            else
            {
                startArcDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetEndPoint());
                endArcDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetStartPoint());
            }

            Route route = this.SolvePath(startNodeIndex, endNodeIndex, powerField);


            route.Impedance += startArcDistance;
            route.RouteNodes.Insert(0, new GeoNetworkNode(-1, startTruePoint.GetX(), startTruePoint.GetY(), null));


            route.Impedance += endArcDistance;
            route.RouteNodes.Add(new GeoNetworkNode(-1, endTruePoint.GetX(), endTruePoint.GetY(), null));

            return route;
        }

        public override Route GreedySolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
        {
            if (powerField == null) powerField = "";

            // GeoNetwork里节点具备地理坐标后重写为A*算法
            if (neighborMatrix == null || networkChangeFlag == true || nowMatrixField != powerField)
            {
                RefreshNeighborMatrix(powerField);
                RefreshGridSpatialIndex();
            }

            IEnumerable<int> candidateStartArcs = segmentSpatialIndex.SearchID(startPoint, tolerateDistance);
            IEnumerable<int> candidateEndArcs = segmentSpatialIndex.SearchID(endPoint, tolerateDistance);

            if (candidateStartArcs.Count() == 0 || candidateEndArcs.Count() == 0) return null;

            double minStartDistance = double.MaxValue;
            int minStartDistanceIndex = -1;

            foreach (int candidateArcIndex in candidateStartArcs)
            {
                GeoNetworkArc candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc;
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, startPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, startPoint);
                }

                if (distance < minStartDistance)
                {
                    minStartDistanceIndex = candidateArcIndex;
                    minStartDistance = distance;
                }
            }

            double minEndDistance = double.MaxValue;
            int minEndDistanceIndex = -1;

            foreach (int candidateArcIndex in candidateEndArcs)
            {
                GeoNetworkArc candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc;
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, endPoint);
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, endPoint);
                }

                if (distance < minEndDistance)
                {
                    minEndDistanceIndex = candidateArcIndex;
                    minEndDistance = distance;
                }
            }

            GeoNetworkArc startArc = ArcList[minStartDistanceIndex] as GeoNetworkArc;
            GeoNetworkArc endArc = ArcList[minEndDistanceIndex] as GeoNetworkArc;

            IPoint startTruePoint = DistanceCalculator.CrossPoint(startArc, startPoint);
            IPoint endTruePoint = DistanceCalculator.CrossPoint(endArc, endPoint);

            int startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetEndNodeID()));
            int endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetEndNodeID()));

            double startArcDistance, endArcDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startArcDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetEndPoint());
                endArcDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetStartPoint());
            }
            else
            {
                startArcDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetEndPoint());
                endArcDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetStartPoint());
            }

            Route route = this.GreedySolvePath(startNodeIndex, endNodeIndex, powerField);

            if (route.Impedance == 0 && route.RouteNodes.Count == 0 && route.RouteArcs.Count == 0)
            {
                return route;
            }

            route.Impedance += startArcDistance;
            route.RouteNodes.Insert(0, new GeoNetworkNode(-1, startTruePoint.GetX(), startTruePoint.GetY(), null));


            route.Impedance += endArcDistance;
            route.RouteNodes.Add(new GeoNetworkNode(-1, endTruePoint.GetX(), endTruePoint.GetY(), null));

            return route;
        }
    }
}
