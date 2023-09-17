using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;
using ThomasGIS.SpatialIndex;
using System.Diagnostics;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Network
{
    public class GeoNetwork : Network, IGeoNetwork
    {
        public readonly CoordinateBase networkCoordinateSystem;

        public ISpatialIndex pointSpatialIndex;

        public ISpatialIndex segmentSpatialIndex;

        public GeoNetwork(CoordinateBase coordinateSystem) : base()
        {
            this.networkCoordinateSystem = coordinateSystem;
        }

        public virtual BoundaryBox _CalculateBoundaryBox()
        {
            if (this.NodeList.Count == 0) return null;

            double xmax = this.NodeList.Max(node => ((GeoNetworkNode)node).GetX());
            double xmin = this.NodeList.Min(node => ((GeoNetworkNode)node).GetX());
            double ymax = this.NodeList.Max(node => ((GeoNetworkNode)node).GetY());
            double ymin = this.NodeList.Min(node => ((GeoNetworkNode)node).GetY());

            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }

        private void _CalculatePointSpatialIndex()
        {
            BoundaryBox mbr = _CalculateBoundaryBox();
            if (mbr == null) return;

            double scaleX = (mbr.XMax - mbr.XMin) / 1000.0;
            double scaleY = (mbr.YMax - mbr.YMin) / 1000.0;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                scaleX *= 111000.0;
                scaleY *= 111000.0;
            }

            double scale = scaleX > scaleY ? scaleX : scaleY;

            if (scale == 0) scale = 1;

            pointSpatialIndex = new GridSpatialIndex(mbr, scale, networkCoordinateSystem);

            foreach (INetworkNode networkNode in NodeList)
            {
                if (networkNode.GetType() == typeof(GeoNetworkNode))
                {
                    pointSpatialIndex.AddItem(networkNode as GeoNetworkNode);
                }
                else if (networkNode.GetType() == typeof(GeoNetworkNode3D))
                {
                    pointSpatialIndex.AddItem(networkNode as GeoNetworkNode3D);
                }
            }

            pointSpatialIndex.RefreshIndex();
        }

        private void _CalculateSegmentSpatialIndex()
        {
            BoundaryBox mbr = _CalculateBoundaryBox();
            if (mbr == null) return;

            double scaleX = (mbr.XMax - mbr.XMin) / 1000.0;
            double scaleY = (mbr.YMax - mbr.YMin) / 1000.0;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                scaleX *= 111000.0;
                scaleY *= 111000.0;
            }

            double scale = scaleX > scaleY ? scaleX : scaleY;

            if (scale == 0) scale = 1;

            segmentSpatialIndex = new GridSpatialIndex(mbr, scale, networkCoordinateSystem);

            foreach (INetworkArc networkArc in ArcList)
            {
                if (networkArc.GetType() == typeof(GeoNetworkArc))
                {
                    segmentSpatialIndex.AddItem(networkArc as GeoNetworkArc);
                }
                else if (networkArc.GetType() == typeof(GeoNetworkArc3D))
                {
                    segmentSpatialIndex.AddItem(networkArc as GeoNetworkArc3D);
                }
            }

            segmentSpatialIndex.RefreshIndex();
        }

        public GeoNetwork(IShapefile shapefile, bool splitShapePoint = true)
        {
            this.networkCoordinateSystem = shapefile.GetCoordinateRef();

            if (splitShapePoint)
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
                pointSpatialIndex = new GridSpatialIndex(mbr, scale, networkCoordinateSystem);

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

                pointSpatialIndex.RefreshIndex();

                // 构建点与边的关系
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

                        for (int k = startLoc; k < endLoc - 1; k++)
                        {
                            IPoint startPoint = polyline.PointList[k];
                            IPoint endPoint = polyline.PointList[k + 1];

                            if (startPoint.GetX() == endPoint.GetX() && startPoint.GetY() == endPoint.GetY()) continue;

                            IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(startPoint, scale);
                            IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(endPoint, scale);

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

                            this.AddArc(new GeoNetworkArc(startNode, endNode, dataField));
                        }
                    }
                }
            }
            else
            {
                HashSet<string> pointSet = new HashSet<string>();

                // 从shapefile里拿到所有的道路节点构建顶点集合V
                for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline polyline = shapefile.GetFeature(i) as ShpPolyline;

                    if (polyline.PartNumber > 1) throw new Exception("请先执行独立部件操作");

                    int startLoc = 0;
                    int endLoc = polyline.PointNumber - 1;

                    pointSet.Add($"{ polyline.PointList[startLoc].GetX() },{ polyline.PointList[startLoc].GetY() }");
                    pointSet.Add($"{ polyline.PointList[endLoc].GetX() },{ polyline.PointList[endLoc].GetY() }");
                }

                List<string> pointStrList = pointSet.ToList();
                pointSet.Clear();

                BoundaryBox mbr = shapefile.GetBoundaryBox();
                double scale = (mbr.XMax - mbr.XMin) / 1000.0;
                if (shapefile.GetCoordinateRef().GetCoordinateType() == CoordinateType.Geographic)
                {
                    scale *= 111000.0;
                }
                pointSpatialIndex = new GridSpatialIndex(mbr, scale, networkCoordinateSystem);

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

                pointSpatialIndex.RefreshIndex();

                // 构建点与边的关系
                for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline polyline = shapefile.GetFeature(i) as ShpPolyline;

                    int startLoc = 0;
                    int endLoc = polyline.PointNumber - 1;

                    IPoint startPoint = polyline.PointList[startLoc];
                    IPoint endPoint = polyline.PointList[endLoc];

                    if (startPoint.GetX() == endPoint.GetX() && startPoint.GetY() == endPoint.GetY()) continue;

                    IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(startPoint, scale);
                    IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(endPoint, scale);

                    GeoNetworkNode startNode = null, endNode = null;

                    foreach (int index in startNodeCandicateIndex)
                    {
                        GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                        if (Math.Abs(checkNode.X - startPoint.GetX()) < 0.00000001 && Math.Abs(checkNode.Y - startPoint.GetY()) < 0.00000001)
                        {
                            startNode = checkNode;
                            break;
                        }
                    }

                    foreach (int index in endNodeCandicateIndex)
                    {
                        GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                        if (Math.Abs(checkNode.X - endPoint.GetX()) < 0.00000001 && Math.Abs(checkNode.Y - endPoint.GetY()) < 0.00000001)
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

                    this.AddArc(new GeoNetworkArc(startNode, endNode, dataField));
                }
            }

            this.RefreshNeighborMatrix();
        }

        public new int GetArcNumber()
        {
            return this.ArcList.Count;
        }

        public new int GetNodeNumber()
        {
            return this.NodeList.Count;
        }

        public override bool AddNode(INetworkNode newNode)
        {
            if (newNode.GetType() != typeof(GeoNetworkNode))
            {
                throw new Exception("GeoNetwork仅支持添加GeoNetworkNode类型的节点！");
            }

            this.NodeList.Add(newNode);

            return true;
        }

        public override bool AddNodes(IEnumerable<INetworkNode> nodeList)
        {
            foreach (INetworkNode node in nodeList)
            {
                AddNode(node);
            }

            return true;
        }

        public override bool AddArc(INetworkArc newArc)
        {
            if (newArc.GetType() != typeof(GeoNetworkArc))
            {
                throw new Exception("GeoNetwork仅支持添加GeoNetworkArc类型的连边！");
            }

            this.ArcList.Add(newArc);

            return true;
        }

        public override bool AddArcs(IEnumerable<INetworkArc> arcList)
        {
            foreach (INetworkArc arc in arcList)
            {
                AddArc(arc);
            }

            return true;
        }

        protected bool RefreshGridSpatialIndex()
        {
            _CalculatePointSpatialIndex();
            _CalculateSegmentSpatialIndex();
            return true;
        }

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
                    if (checkNode.GetID() == oneArc.GetStartNodeID())
                    {
                        startNodeIndex = index;
                        break;
                    }
                }

                foreach (int index in endNodeCandicateIndex)
                {
                    GeoNetworkNode checkNode = this.NodeList[index] as GeoNetworkNode;
                    if (checkNode.GetID() == oneArc.GetEndNodeID())
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

                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, Convert.ToDouble(oneArc.GetProperty(dataField)), i));
                    this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(startNodeIndex, Convert.ToDouble(oneArc.GetProperty(dataField)), i));
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
                    this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(startNodeIndex, distance, i));
                }
            }

            this.nowMatrixField = dataField;
            this.networkChangeFlag = false;
        }

        public virtual IShapefile ExportNodesToShapefile()
        {
            // 识别所有弧段中的属性
            Dictionary<string, DBFFieldType> fieldDict = new Dictionary<string, DBFFieldType>();

            for (int i = 0; i < NodeNumber; i++)
            {
                GeoNetworkNode oneNode = this.NodeList[i] as GeoNetworkNode;
                foreach (string key in oneNode.GetKeys())
                {
                    if (fieldDict.ContainsKey(key)) continue;

                    object value = oneNode.GetProperty(key);
                    if (value.GetType() == typeof(int) || value.GetType() == typeof(double))
                    {
                        fieldDict.Add(key, DBFFieldType.Number);
                    }
                    else if (value.GetType() == typeof(string))
                    {
                        fieldDict.Add(key, DBFFieldType.Char);
                    }
                    else
                    {
                        fieldDict.Add(key, DBFFieldType.Binary);
                    }
                }
            }

            // 新建一个Shapefile
            IShapefile nodeShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Point);

            // 新建Shapefile的属性表
            foreach (string fieldName in fieldDict.Keys)
            {
                int length;
                int precision;

                DBFFieldType dBFFieldType = fieldDict[fieldName];

                if (dBFFieldType == DBFFieldType.Char)
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.string.length"));
                    precision = 0;
                }
                else if (dBFFieldType == DBFFieldType.Number)
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.numberic.length"));
                    precision = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.numberic.precision"));
                }
                else
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.default.length"));
                    precision = 0;
                }

                nodeShapefile.AddField(fieldName, dBFFieldType, length, precision);
            }

            nodeShapefile.AddField("ID", DBFFieldType.Number, 10, 0);

            // 插入节点与节点的属性表
            for (int i = 0; i < NodeNumber; i++)
            {
                GeoNetworkNode oneNode = this.NodeList[i] as GeoNetworkNode;
                Dictionary<string, object> properties = new Dictionary<string, object>();
                Dictionary<string, object> originProperties = oneNode.GetPropertySheet();
                foreach (string key in originProperties.Keys)
                {
                    properties.Add(key, originProperties[key]);
                }
                properties.Add("ID", i);
                nodeShapefile.AddFeature(oneNode.TransferToShpPoint(), properties);
            }

            // 设置shapefile的坐标系统
            nodeShapefile.SetCoordinateRef(this.networkCoordinateSystem);

            return nodeShapefile;
        }

        public virtual IShapefile ExportArcsToShapefile()
        {
            // 识别所有弧段中的属性
            Dictionary<string, DBFFieldType> fieldDict = new Dictionary<string, DBFFieldType>();

            for (int i = 0; i < ArcNumber; i++)
            {
                INetworkArc oneArc = this.ArcList[i];
                foreach (string key in oneArc.GetKeys())
                {
                    if (fieldDict.ContainsKey(key)) continue;

                    object value = oneArc.GetProperty(key);
                    if (value.GetType() == typeof(int) || value.GetType() == typeof(double))
                    {
                        fieldDict.Add(key, DBFFieldType.Number);
                    }
                    else if (value.GetType() == typeof(string))
                    {
                        fieldDict.Add(key, DBFFieldType.Char);
                    }
                    else
                    {
                        fieldDict.Add(key, DBFFieldType.Binary);
                    }
                }
            }

            // 新建一个Shapefile
            IShapefile arcShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);

            // 新建Shapefile的属性表
            foreach (string fieldName in fieldDict.Keys)
            {
                int length;
                int precision;

                DBFFieldType dBFFieldType = fieldDict[fieldName];

                if (dBFFieldType == DBFFieldType.Char)
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.string.length"));
                    precision = 0;
                }
                else if (dBFFieldType == DBFFieldType.Number)
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.numberic.length"));
                    precision = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.numberic.precision"));
                }
                else
                {
                    length = Convert.ToInt32(Configuration.GetConfiguration("geonetwork.property.default.length"));
                    precision = 0;
                }

                arcShapefile.AddField(fieldName, dBFFieldType, length, precision);
            }

            arcShapefile.AddField("SNodeID", DBFFieldType.Number, 10, 0);
            arcShapefile.AddField("ENodeID", DBFFieldType.Number, 10, 0);

            // 插入节点与节点的属性表
            for (int i = 0; i < ArcNumber; i++)
            {
                GeoNetworkArc oneArc = this.ArcList[i] as GeoNetworkArc;
                ShpPolyline newPolyline = new ShpPolyline();
                List<Point> pointList = new List<Point>();
                pointList.Add(new Point(oneArc.StartPoint.GetX(), oneArc.StartPoint.GetY()));
                pointList.Add(new Point(oneArc.EndPoint.GetX(), oneArc.EndPoint.GetY()));
                newPolyline.AddPart(pointList);
                int featureIndex = arcShapefile.AddFeature(newPolyline, oneArc.GetPropertySheet());
                arcShapefile.SetValue(featureIndex, "SNodeID", oneArc.GetStartNodeID());
                arcShapefile.SetValue(featureIndex, "ENodeID", oneArc.GetEndNodeID());
            }

            // 设置shapefile的坐标系统
            arcShapefile.SetCoordinateRef(this.networkCoordinateSystem);

            return arcShapefile;
        }

        public override Route SolvePath(int startNodeIndex, int endNodeIndex, string powerField)
        {
            GeoNetworkNode originNode = NodeList[startNodeIndex] as GeoNetworkNode;
            GeoNetworkNode destinationNode = NodeList[endNodeIndex] as GeoNetworkNode;

            double directDistance;
            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                directDistance = DistanceCalculator.SpatialDistanceGeo(originNode, destinationNode);
            }
            else
            {
                directDistance = DistanceCalculator.SpatialDistance(originNode, destinationNode);
            }

            if (powerField == null) powerField = "";

            // GeoNetwork里节点具备地理坐标后重写为A*算法
            if (neighborMatrix == null || networkChangeFlag == true || nowMatrixField != powerField)
            {
                RefreshNeighborMatrix(powerField);
                RefreshGridSpatialIndex();
            }

            Dictionary<int, int> parentDict = new Dictionary<int, int>();
            Dictionary<int, double> distanceDict = new Dictionary<int, double>();

            // 开放列表与封闭列表，开放列表中存储下面即将遍历的节点
            List<int> openList = new List<int>();
            List<int> closeList = new List<int>();

            openList.Add(startNodeIndex);
            distanceDict.Add(startNodeIndex, 0);
            parentDict.Add(startNodeIndex, -1);

            while (openList.Count > 0)
            {
                //List<double> FList = new List<double>();
                double minF = Double.MaxValue;
                int minFlistIndex = -1;

                for (int i = 0; i < openList.Count; i++)
                {
                    int nodeIndex = openList[i];

                    // 计算F值
                    int routeIndex = nodeIndex;
                    int parentIndex = parentDict[routeIndex];
                    double G = 0;
                    double H = 0;

                    if (parentIndex != -1)
                    {
                        G = distanceDict[nodeIndex];
                    }

                    GeoNetworkNode nowNode = NodeList[nodeIndex] as GeoNetworkNode;
                    GeoNetworkNode targetNode = NodeList[endNodeIndex] as GeoNetworkNode;

                    // 距离终点的直线距离
                    if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                    {
                        H = DistanceCalculator.SpatialDistanceGeo(nowNode.X, nowNode.Y, targetNode.X, targetNode.Y);
                    }
                    else
                    {
                        H = DistanceCalculator.SpatialDistance(nowNode.X, nowNode.Y, targetNode.X, targetNode.Y);
                    }

                    if (G + H < minF)
                    {
                        minF = G + H;
                        minFlistIndex = i;
                    }
                }

                // 选出F值最小的点
                int selectedNodeIndex = openList[minFlistIndex];
                openList.RemoveAt(minFlistIndex);

                // 将该节点的不在ClostList中的邻居加入OpenList
                List<LinkValuePair> links = this.neighborMatrix[selectedNodeIndex];
                foreach (LinkValuePair pair in links)
                {
                    if (closeList.Contains(pair.EndNodeIndex)) continue;

                    if (openList.Contains(pair.EndNodeIndex))
                    {
                        if (distanceDict[pair.EndNodeIndex] > distanceDict[selectedNodeIndex] + pair.Value)
                        {
                            parentDict[pair.EndNodeIndex] = selectedNodeIndex;
                            distanceDict[pair.EndNodeIndex] = distanceDict[selectedNodeIndex] + pair.Value;
                        }
                    }
                    else
                    {
                        if (distanceDict[selectedNodeIndex] + pair.Value < directDistance * 10)
                        {
                            distanceDict.Add(pair.EndNodeIndex, distanceDict[selectedNodeIndex] + pair.Value);
                            parentDict.Add(pair.EndNodeIndex, selectedNodeIndex);
                            openList.Add(pair.EndNodeIndex);
                        }
                    }
                }
                closeList.Add(selectedNodeIndex);

                // 终点加入OpenList后停止
                if (openList.Contains(endNodeIndex))
                {
                    break;
                }
            }

            if (openList.Count == 0)
            {
                return new Route(new List<INetworkNode>(), new List<int>(), 0, false);
            }
            else
            {
                List<INetworkNode> nodeList = new List<INetworkNode>();
                List<int> arcIndexList = new List<int>();

                int tempIndex = endNodeIndex;
                int parentIndex = parentDict[endNodeIndex];
                double distance = 0;

                while (parentIndex != -1)
                {
                    INetworkNode routeNode = NodeList[tempIndex];
                    LinkValuePair linkpair = this.neighborMatrix[parentIndex].Where(pair => pair.EndNodeIndex == tempIndex).ToList()[0];
                    arcIndexList.Insert(0, linkpair.ArcIndex);
                    distance += linkpair.Value;
                    nodeList.Insert(0, routeNode);
                    tempIndex = parentIndex;
                    parentIndex = parentDict[tempIndex];
                }

                nodeList.Insert(0, NodeList[tempIndex]);

                return new Route(nodeList, arcIndexList, distance, true);
            }
        }

        // 带贪心的A*求解，实时减少OpenList中的节点数量
        public virtual Route GreedySolvePath(int startNodeIndex, int endNodeIndex, string powerField)
        {
            GeoNetworkNode originNode = NodeList[startNodeIndex] as GeoNetworkNode;
            GeoNetworkNode destinationNode = NodeList[endNodeIndex] as GeoNetworkNode;

            double directDistance;
            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                directDistance = DistanceCalculator.SpatialDistanceGeo(originNode, destinationNode);
            }
            else
            {
                directDistance = DistanceCalculator.SpatialDistance(originNode, destinationNode);
            }

            if (powerField == null) powerField = "";

            // GeoNetwork里节点具备地理坐标后重写为A*算法
            if (neighborMatrix == null || networkChangeFlag == true || nowMatrixField != powerField)
            {
                RefreshNeighborMatrix(powerField);
                RefreshGridSpatialIndex();
            }

            Dictionary<int, int> parentDict = new Dictionary<int, int>();
            Dictionary<int, double> distanceDict = new Dictionary<int, double>();

            // 开放列表与封闭列表，开放列表中存储下面即将遍历的节点
            List<int> openList = new List<int>();
            List<int> closeList = new List<int>();

            openList.Add(startNodeIndex);
            distanceDict.Add(startNodeIndex, 0);
            parentDict.Add(startNodeIndex, -1);

            while (openList.Count > 0)
            {
                //List<double> FList = new List<double>();
                double minF = Double.MaxValue;
                int minFlistIndex = -1;

                for (int i = 0; i < openList.Count; i++)
                {
                    int nodeIndex = openList[i];

                    // 计算F值
                    int routeIndex = nodeIndex;
                    int parentIndex = parentDict[routeIndex];
                    double G = 0;
                    double H = 0;

                    if (parentIndex != -1)
                    {
                        G = distanceDict[nodeIndex];
                    }

                    GeoNetworkNode nowNode = NodeList[nodeIndex] as GeoNetworkNode;
                    GeoNetworkNode targetNode = NodeList[endNodeIndex] as GeoNetworkNode;

                    // 距离终点的直线距离
                    if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                    {
                        H = DistanceCalculator.SpatialDistanceGeo(nowNode.X, nowNode.Y, targetNode.X, targetNode.Y);
                    }
                    else
                    {
                        H = DistanceCalculator.SpatialDistance(nowNode.X, nowNode.Y, targetNode.X, targetNode.Y);
                    }

                    if (G + H < minF)
                    {
                        minF = G + H;
                        minFlistIndex = i;
                    }
                }

                // 选出F值最小的点
                int selectedNodeIndex = openList[minFlistIndex];
                openList.RemoveAt(minFlistIndex);

                while (openList.Count > 10)
                {
                    closeList.Add(openList[0]);
                    openList.RemoveAt(0);
                }

                // 将该节点的不在ClostList中的邻居加入OpenList
                List<LinkValuePair> links = this.neighborMatrix[selectedNodeIndex];
                foreach (LinkValuePair pair in links)
                {
                    if (closeList.Contains(pair.EndNodeIndex)) continue;

                    if (openList.Contains(pair.EndNodeIndex))
                    {
                        if (distanceDict[pair.EndNodeIndex] > distanceDict[selectedNodeIndex] + pair.Value)
                        {
                            parentDict[pair.EndNodeIndex] = selectedNodeIndex;
                            distanceDict[pair.EndNodeIndex] = distanceDict[selectedNodeIndex] + pair.Value;
                        }
                    }
                    else
                    {
                        if (distanceDict[selectedNodeIndex] + pair.Value < directDistance * 10)
                        {
                            distanceDict.Add(pair.EndNodeIndex, distanceDict[selectedNodeIndex] + pair.Value);
                            parentDict.Add(pair.EndNodeIndex, selectedNodeIndex);
                            openList.Add(pair.EndNodeIndex);
                        }
                    }
                }
                closeList.Add(selectedNodeIndex);

                // 终点加入OpenList后停止
                if (openList.Contains(endNodeIndex))
                {
                    break;
                }
            }

            if (openList.Count == 0)
            {
                return new Route(new List<INetworkNode>(), new List<int>(), 0, false);
            }
            else
            {
                List<INetworkNode> nodeList = new List<INetworkNode>();
                List<int> arcIndexList = new List<int>();

                int tempIndex = endNodeIndex;
                int parentIndex = parentDict[endNodeIndex];
                double distance = 0;

                while (parentIndex != -1)
                {
                    INetworkNode routeNode = NodeList[tempIndex];
                    LinkValuePair linkpair = this.neighborMatrix[parentIndex].Where(pair => pair.EndNodeIndex == tempIndex).ToList()[0];
                    arcIndexList.Insert(0, linkpair.ArcIndex);
                    distance += linkpair.Value;
                    nodeList.Insert(0, routeNode);
                    tempIndex = parentIndex;
                    parentIndex = parentDict[tempIndex];
                }

                nodeList.Insert(0, NodeList[tempIndex]);

                return new Route(nodeList, arcIndexList, distance, true);
            }
        }

        public virtual Route SolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
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

            if (candidateStartArcs.Count() == 0 || candidateEndArcs.Count() == 0)
            {
                Route newRoute = new Route(new List<GeoNetworkNode>(), new List<int>(), 0, false);
                return newRoute;
            }

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

            double startCrossPointToStartNodeDistance, startCrossPointToEndNodeDistance;
            double endCrossPointToStartNodeDistance, endCrossPointToEndNodeDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetStartPoint());
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetEndPoint());
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetStartPoint());
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetEndPoint());
            }
            else
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetStartPoint());
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetEndPoint());
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetStartPoint());
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetEndPoint());
            }

            int startNodeIndex, endNodeIndex;

            if (startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance)
            {
                startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetStartNodeID()));
            }
            else
            {
                startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetEndNodeID()));
            }

            if (endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance)
            {
                endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetStartNodeID()));
            }
            else
            {
                endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetEndNodeID()));
            }

            Route route = this.SolvePath(startNodeIndex, endNodeIndex, powerField);
            if (!route.isExist) return route;

            if (route.RouteArcs.First() == minStartDistanceIndex)
            {
                route.RouteNodes.RemoveAt(0);
                route.Impedance -= startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance ? startCrossPointToStartNodeDistance : startCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance ? startCrossPointToStartNodeDistance : startCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Insert(0, new GeoNetworkNode(-1, startTruePoint.GetX(), startTruePoint.GetY(), null));

            if (route.RouteArcs.Last() == minEndDistanceIndex)
            {
                route.RouteNodes.RemoveAt(route.RouteNodes.Count - 1);
                route.Impedance -= endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Add(new GeoNetworkNode(-1, endTruePoint.GetX(), endTruePoint.GetY(), null));

            return route;
        }

        public virtual Route GreedySolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
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

            if (candidateStartArcs.Count() == 0 || candidateEndArcs.Count() == 0)
            {
                Route newRoute = new Route(new List<GeoNetworkNode>(), new List<int>(), 0, false);
                return newRoute;
            }

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

            double startCrossPointToStartNodeDistance, startCrossPointToEndNodeDistance;
            double endCrossPointToStartNodeDistance, endCrossPointToEndNodeDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetStartPoint());
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistanceGeo(startTruePoint, startArc.GetEndPoint());
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetStartPoint());
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistanceGeo(endTruePoint, endArc.GetEndPoint());
            }
            else
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetStartPoint());
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance(startTruePoint, startArc.GetEndPoint());
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetStartPoint());
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance(endTruePoint, endArc.GetEndPoint());
            }

            int startNodeIndex, endNodeIndex;

            if (startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance)
            {
                startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetStartNodeID()));
            }
            else
            {
                startNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(startArc.GetEndNodeID()));
            }

            if (endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance)
            {
                endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetStartNodeID()));
            }
            else
            {
                endNodeIndex = this.NodeList.IndexOf(this.GetNodeByID(endArc.GetEndNodeID()));
            }

            Route route = this.GreedySolvePath(startNodeIndex, endNodeIndex, powerField);
            if (!route.isExist) return route;

            if (route.RouteArcs.First() == minStartDistanceIndex)
            {
                route.RouteNodes.RemoveAt(0);
                route.Impedance -= startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance ? startCrossPointToStartNodeDistance : startCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += startCrossPointToStartNodeDistance < startCrossPointToEndNodeDistance ? startCrossPointToStartNodeDistance : startCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Insert(0, new GeoNetworkNode(-1, startTruePoint.GetX(), startTruePoint.GetY(), null));

            if (route.RouteArcs.Last() == minEndDistanceIndex)
            {
                route.RouteNodes.RemoveAt(route.RouteNodes.Count - 1);
                route.Impedance -= endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Add(new GeoNetworkNode(-1, endTruePoint.GetX(), endTruePoint.GetY(), null));

            return route;
        }

        // Function: 基于节点间的距离关系产生预测性连接
        // Params:
        // maxTolerateDistance: 最大容忍距离，在maxTolerateDistance范围内的点产生连接
        // KLinks: 每个节点在其回合内最多连接K条边
        // judgeFunction: 节点属性的相似性判别函数，使用两个不同网络节点的属性表进行计算，判别方式为 !=0 构建实变，==0 构建虚边 
        public bool LinkNodes_KLink_MaxDistance(double maxTolerateDistance, int KLinks, Func<Dictionary<string, object>, Dictionary<string, object>, int> judgeFunction)
        {
            // 首先清除弧段容器中的全部弧段
            ArcList.Clear();

            this.neighborMatrix = new List<List<LinkValuePair>>();

            bool[] flagList = new bool[NodeNumber];

            for (int i = 0; i < NodeNumber; i++)
            {
                neighborMatrix.Add(new List<LinkValuePair>());
                flagList[i] = false;
            }

            for (int i = 0; i < NodeNumber; i++)
            {
                // 选一个未被处理过的节点开始尝试连接
                if (flagList[i] == true) continue;
                Queue<int> generateTree = new Queue<int>();
                generateTree.Enqueue(i);
                flagList[i] = true;

                while (generateTree.Count > 0)
                {
                    int tempNodeIndex = generateTree.Dequeue();
                    GeoNetworkNode tempNode = NodeList[tempNodeIndex] as GeoNetworkNode;
                    flagList[tempNodeIndex] = true;

                    List<KeyValuePair<int, double>> distanceList = new List<KeyValuePair<int, double>>();
                    // 计算剩余节点至该节点的长度
                    for (int j = 0; j < NodeNumber; j++)
                    {
                        if (flagList[j] == true)
                        {
                            continue;
                        }

                        GeoNetworkNode neighborNode = NodeList[j] as GeoNetworkNode;
                        if (networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            double distance = DistanceCalculator.SpatialDistanceGeo(tempNode.X, tempNode.Y, neighborNode.X, neighborNode.Y);
                            distanceList.Add(new KeyValuePair<int, double>(j, distance));
                        }
                        else
                        {
                            double distance = DistanceCalculator.SpatialDistance(tempNode.X, tempNode.Y, neighborNode.X, neighborNode.Y);
                            distanceList.Add(new KeyValuePair<int, double>(j, distance));
                        }
                    }

                    // distanceList按长度升序排序
                    distanceList.Sort((item1, item2) =>
                    {
                        return item1.Value.CompareTo(item2.Value);
                    });

                    // 选取前K个，若不足K个则全取
                    for (int k = 0; k < Math.Min(KLinks, distanceList.Count); k++)
                    {
                        KeyValuePair<int, double> onePair = distanceList[k];
                        int endNodeIndex = onePair.Key;
                        double distance = onePair.Value;

                        // 只要当前超过，后面的都超了，直接结束
                        if (distance > maxTolerateDistance) break;

                        GeoNetworkNode neighborNode = NodeList[endNodeIndex] as GeoNetworkNode;

                        List<LinkValuePair> existingLinks = this.neighborMatrix[tempNodeIndex];

                        // 如果连接已经存在则跳过
                        if (existingLinks.Where(item => item.EndNodeIndex == endNodeIndex).Count() != 0) continue;

                        // 否则开始构建新边
                        int similarity = judgeFunction(tempNode.GetPropertySheet(), NodeList[endNodeIndex].GetPropertySheet());
                        Dictionary<string, object> arcData = new Dictionary<string, object>();

                        if (similarity == 0)
                        {
                            arcData.Add("LinkType", 0);
                        }
                        else
                        {
                            arcData.Add("LinkType", 1);
                        }

                        GeoNetworkArc newArc = new GeoNetworkArc(tempNode, neighborNode, arcData);
                        this.AddArc(newArc);

                        // 顺便把邻接矩阵刷新一下
                        this.neighborMatrix[tempNodeIndex].Add(new LinkValuePair(endNodeIndex, distance, ArcList.Count - 1));
                        this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(tempNodeIndex, distance, ArcList.Count - 1));

                        // 把新增的点加入generateTree
                        generateTree.Enqueue(endNodeIndex);
                    }
                }
            }

            return true;
        }

        public GeoNetworkArc GetGeoNetworkArc(int index)
        {
            if (index < -this.ArcNumber || index >= this.ArcNumber) throw new Exception("索引超出界限！");

            if (index < 0) index += this.ArcNumber;

            return this.ArcList[index] as GeoNetworkArc;
        }

        public GeoNetworkNode GetGeoNetworkNode(int index)
        {
            if (index < -this.NodeNumber || index >= this.NodeNumber) throw new Exception("索引超出界限！");

            if (index < 0) index += this.NodeNumber;

            return this.NodeList[index] as GeoNetworkNode;
        }

        public GeoNetworkNode GetGeoNetworkNodeByID(int ID)
        {
            IEnumerable<INetworkNode> nodeList = this.NodeList.Where(item => item.GetID() == ID);

            if (nodeList.Count() == 0) return null;

            return nodeList.ElementAt(0) as GeoNetworkNode;
        }

        public CoordinateBase GetGeoNetworkCoordinate()
        {
            return this.networkCoordinateSystem;
        }

        public int GetGeoNetworkNodeIndex(int ID)
        {
            IEnumerable<INetworkNode> nodeList = this.NodeList.Where(item => item.GetID() == ID);
            if (nodeList.Count() == 0) return -1;
            return this.NodeList.IndexOf(nodeList.ElementAt(0));
        }

        public bool LinkNodes_Delaunary_Triangle(Func<Dictionary<string, object>, Dictionary<string, object>, int> judgeFunction, double tolerateDistance = -1)
        {
            if (this.NodeList.Count < 3) return false;

            HashSet<string> AddArcsSet = new HashSet<string>();

            // 从点集中任取一点找到与其距离最近的第二个点
            GeoNetworkNode firstNode = this.NodeList[0] as GeoNetworkNode;

            int minDistanceIndex = -1;
            double minDistanceValue = double.MaxValue;

            for (int i = 1; i < this.NodeNumber; i++)
            {
                double distance;
                //if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                //{
                //    distance = DistanceCalculator.SpatialDistanceGeo(firstNode, NodeList[i] as GeoNetworkNode);
                //}
                //else
                //{
                    distance = DistanceCalculator.SpatialDistance(firstNode, NodeList[i] as GeoNetworkNode);
                //}

                if (distance == 0) continue;

                if (distance < minDistanceValue)
                {
                    minDistanceIndex = i;
                    minDistanceValue = distance;
                }
            }

            GeoNetworkNode secondNode = this.NodeList[minDistanceIndex] as GeoNetworkNode;

            Queue<GeoNetworkArc> tempArcQueue = new Queue<GeoNetworkArc>();

            int similarity = judgeFunction(firstNode.GetPropertySheet(), secondNode.GetPropertySheet());
            Dictionary<string, object> arcData = new Dictionary<string, object>();

            if (similarity == 0)
            {
                arcData.Add("LinkType", 0);
            }
            else
            {
                arcData.Add("LinkType", 1);
            }

            GeoNetworkArc firstArc = new GeoNetworkArc(firstNode, secondNode, arcData);
            this.ArcList.Add(firstArc);

            AddArcsSet.Add(firstNode.GetID().ToString() + "," + secondNode.GetID().ToString());
            AddArcsSet.Add(secondNode.GetID().ToString() + "," + firstNode.GetID().ToString());

            // 第一条边左右各一
            int maxAngleIndex = -1;
            double maxAngleValue = -1;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int position = TopoCalculator.PointSingleLinePosition(firstArc, NodeList[i] as GeoNetworkNode);

                if (position >= 0) continue;

                double distance1, distance2, distance3;
                //if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                //{
                //    distance1 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                //    distance2 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                //    distance3 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetStartPoint(), firstArc.GetEndPoint());
                //}
                //else
                //{
                    distance1 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                    distance2 = DistanceCalculator.SpatialDistance(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                    distance3 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), firstArc.GetEndPoint());
                //}

                double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                if (angle == 180) continue;

                if (angle > maxAngleValue)
                {
                    maxAngleIndex = i;
                    maxAngleValue = angle;
                }
            }

            if (maxAngleIndex > 0)
            {
                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;
                int similarity1 = judgeFunction(firstNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData1 = new Dictionary<string, object>();

                if (similarity1 == 0)
                {
                    arcData1.Add("LinkType", 0);
                }
                else
                {
                    arcData1.Add("LinkType", 1);
                }

                GeoNetworkArc newArc1 = new GeoNetworkArc(firstNode, nextNode, arcData1);

                int similarity2 = judgeFunction(secondNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData2 = new Dictionary<string, object>();

                if (similarity2 == 0)
                {
                    arcData2.Add("LinkType", 0);
                }
                else
                {
                    arcData2.Add("LinkType", 1);
                }

                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, secondNode, arcData2);

                string newArc1Identity = firstNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + secondNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + firstNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(secondNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            maxAngleIndex = -1;
            maxAngleValue = -1;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int position = TopoCalculator.PointSingleLinePosition(firstArc, NodeList[i] as GeoNetworkNode);

                if (position <= 0) continue;

                double distance1, distance2, distance3;
                //if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                //{
                //    distance1 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                //    distance2 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                //    distance3 = DistanceCalculator.SpatialDistanceGeo(firstArc.GetStartPoint(), firstArc.GetEndPoint());
                //}
                //else
                //{
                    distance1 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                    distance2 = DistanceCalculator.SpatialDistance(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                    distance3 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), firstArc.GetEndPoint());
                //}

                double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                if (angle == 180) continue;

                if (angle > maxAngleValue)
                {
                    maxAngleIndex = i;
                    maxAngleValue = angle;
                }
            }

            if (maxAngleIndex > 0)
            {
                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;
                int similarity1 = judgeFunction(secondNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData1 = new Dictionary<string, object>();

                if (similarity1 == 0)
                {
                    arcData1.Add("LinkType", 0);
                }
                else
                {
                    arcData1.Add("LinkType", 1);
                }

                GeoNetworkArc newArc1 = new GeoNetworkArc(secondNode, nextNode, arcData1);

                int similarity2 = judgeFunction(firstNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData2 = new Dictionary<string, object>();

                if (similarity2 == 0)
                {
                    arcData2.Add("LinkType", 0);
                }
                else
                {
                    arcData2.Add("LinkType", 1);
                }

                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, firstNode, arcData2);

                string newArc1Identity = firstNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + secondNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + firstNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(secondNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            while (tempArcQueue.Count > 0)
            {
                GeoNetworkArc nowArc = tempArcQueue.Dequeue();
                this.ArcList.Add(nowArc);

                maxAngleIndex = -1;
                maxAngleValue = -1;
                for (int i = 0; i < this.NodeNumber; i++)
                {
                    int position = TopoCalculator.PointSingleLinePosition(nowArc, NodeList[i] as GeoNetworkNode);

                    if (position >= 0) continue;

                    double distance1, distance2, distance3;
                    //if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                    //{
                    //    distance1 = DistanceCalculator.SpatialDistanceGeo(nowArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                    //    distance2 = DistanceCalculator.SpatialDistanceGeo(nowArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                    //    distance3 = DistanceCalculator.SpatialDistanceGeo(nowArc.GetStartPoint(), nowArc.GetEndPoint());
                    //}
                    //else
                    //{
                        distance1 = DistanceCalculator.SpatialDistance(nowArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                        distance2 = DistanceCalculator.SpatialDistance(nowArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                        distance3 = DistanceCalculator.SpatialDistance(nowArc.GetStartPoint(), nowArc.GetEndPoint());
                    //}

                    double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                    if (angle == 180) continue;

                    if (angle > maxAngleValue)
                    {
                        maxAngleIndex = i;
                        maxAngleValue = angle;
                    }
                }

                if (maxAngleIndex == -1) continue;

                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;

                GeoNetworkNode startNode = this.GetGeoNetworkNodeByID(nowArc.GetStartNodeID());
                GeoNetworkNode endNode = this.GetGeoNetworkNodeByID(nowArc.GetEndNodeID());

                int similarity1 = judgeFunction(startNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData1 = new Dictionary<string, object>();

                if (similarity1 == 0)
                {
                    arcData1.Add("LinkType", 0);
                }
                else
                {
                    arcData1.Add("LinkType", 1);
                }

                GeoNetworkArc newArc1 = new GeoNetworkArc(startNode, nextNode, arcData1);

                int similarity2 = judgeFunction(endNode.GetPropertySheet(), nextNode.GetPropertySheet());
                Dictionary<string, object> arcData2 = new Dictionary<string, object>();

                if (similarity2 == 0)
                {
                    arcData2.Add("LinkType", 0);
                }
                else
                {
                    arcData2.Add("LinkType", 1);
                }

                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, endNode, arcData2);

                string newArc1Identity = startNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + endNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + startNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(endNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            for (int i = 0; i < ArcList.Count; i++)
            {
                GeoNetworkArc geoNetworkArc = this.GetGeoNetworkArc(i);
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(geoNetworkArc.GetStartPoint(), geoNetworkArc.GetEndPoint());
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(geoNetworkArc.GetStartPoint(), geoNetworkArc.GetEndPoint());
                }

                if (distance > tolerateDistance && tolerateDistance != -1)
                {
                    this.ArcList.RemoveAt(i);
                    i -= 1;
                }
            }

            RefreshNeighborMatrix();
            RefreshGridSpatialIndex();
            return true;
        }

        public bool DelaunaryTriangle()
        {
            if (this.NodeList.Count < 3) return false;

            HashSet<string> AddArcsSet = new HashSet<string>();

            // 从点集中任取一点找到与其距离最近的第二个点
            GeoNetworkNode firstNode = this.NodeList[0] as GeoNetworkNode;

            int minDistanceIndex = -1;
            double minDistanceValue = double.MaxValue;

            for (int i = 1; i < this.NodeNumber; i++)
            {
                double distance;
                distance = DistanceCalculator.SpatialDistance(firstNode, NodeList[i] as GeoNetworkNode);
                if (distance == 0) continue;

                if (distance < minDistanceValue)
                {
                    minDistanceIndex = i;
                    minDistanceValue = distance;
                }
            }

            GeoNetworkNode secondNode = this.NodeList[minDistanceIndex] as GeoNetworkNode;

            Queue<GeoNetworkArc> tempArcQueue = new Queue<GeoNetworkArc>();
            GeoNetworkArc firstArc = new GeoNetworkArc(firstNode, secondNode);
            this.ArcList.Add(firstArc);

            AddArcsSet.Add(firstNode.GetID().ToString() + "," + secondNode.GetID().ToString());
            AddArcsSet.Add(secondNode.GetID().ToString() + "," + firstNode.GetID().ToString());

            // 第一条边左右各一
            int maxAngleIndex = -1;
            double maxAngleValue = -1;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int position = TopoCalculator.PointSingleLinePosition(firstArc, NodeList[i] as GeoNetworkNode);

                if (position >= 0) continue;

                double distance1, distance2, distance3;
                distance1 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                distance2 = DistanceCalculator.SpatialDistance(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                distance3 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), firstArc.GetEndPoint());

                double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                if (angle == 180) continue;

                if (angle > maxAngleValue)
                {
                    maxAngleIndex = i;
                    maxAngleValue = angle;
                }
            }

            if (maxAngleIndex > 0)
            {
                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;
                GeoNetworkArc newArc1 = new GeoNetworkArc(firstNode, nextNode);
                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, secondNode);

                string newArc1Identity = firstNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + secondNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + firstNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(secondNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            maxAngleIndex = -1;
            maxAngleValue = -1;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int position = TopoCalculator.PointSingleLinePosition(firstArc, NodeList[i] as GeoNetworkNode);

                if (position <= 0) continue;

                double distance1, distance2, distance3;

                distance1 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                distance2 = DistanceCalculator.SpatialDistance(firstArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                distance3 = DistanceCalculator.SpatialDistance(firstArc.GetStartPoint(), firstArc.GetEndPoint());

                double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                if (angle == 180) continue;

                if (angle > maxAngleValue)
                {
                    maxAngleIndex = i;
                    maxAngleValue = angle;
                }
            }

            if (maxAngleIndex > 0)
            {
                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;
                GeoNetworkArc newArc1 = new GeoNetworkArc(secondNode, nextNode);
                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, firstNode);

                string newArc1Identity = firstNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + secondNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + firstNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(secondNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            while (tempArcQueue.Count > 0)
            {
                GeoNetworkArc nowArc = tempArcQueue.Dequeue();
                this.ArcList.Add(nowArc);

                maxAngleIndex = -1;
                maxAngleValue = -1;
                for (int i = 0; i < this.NodeNumber; i++)
                {
                    int position = TopoCalculator.PointSingleLinePosition(nowArc, NodeList[i] as GeoNetworkNode);

                    if (position >= 0) continue;

                    double distance1, distance2, distance3;
                    distance1 = DistanceCalculator.SpatialDistance(nowArc.GetStartPoint(), NodeList[i] as GeoNetworkNode);
                    distance2 = DistanceCalculator.SpatialDistance(nowArc.GetEndPoint(), NodeList[i] as GeoNetworkNode);
                    distance3 = DistanceCalculator.SpatialDistance(nowArc.GetStartPoint(), nowArc.GetEndPoint());

                    double angle = Math.Acos((Math.Pow(distance1, 2) + Math.Pow(distance2, 2) - Math.Pow(distance3, 2)) / (2 * distance1 * distance2)) / Math.PI * 180.0;

                    if (angle == 180) continue;

                    if (angle > maxAngleValue)
                    {
                        maxAngleIndex = i;
                        maxAngleValue = angle;
                    }
                }

                if (maxAngleIndex == -1) continue;

                GeoNetworkNode nextNode = this.NodeList[maxAngleIndex] as GeoNetworkNode;

                GeoNetworkNode startNode = this.GetGeoNetworkNodeByID(nowArc.GetStartNodeID());
                GeoNetworkNode endNode = this.GetGeoNetworkNodeByID(nowArc.GetEndNodeID());
                GeoNetworkArc newArc1 = new GeoNetworkArc(startNode, nextNode);
                GeoNetworkArc newArc2 = new GeoNetworkArc(nextNode, endNode);

                string newArc1Identity = startNode.GetID().ToString() + "," + nextNode.GetID().ToString();
                string newArc2Identity = nextNode.GetID().ToString() + "," + endNode.GetID().ToString();

                if (!AddArcsSet.Contains(newArc1Identity))
                {
                    tempArcQueue.Enqueue(newArc1);
                    AddArcsSet.Add(newArc1Identity);
                    AddArcsSet.Add(nextNode.GetID().ToString() + "," + startNode.GetID().ToString());
                }

                if (!AddArcsSet.Contains(newArc2Identity))
                {
                    tempArcQueue.Enqueue(newArc2);
                    AddArcsSet.Add(newArc2Identity);
                    AddArcsSet.Add(endNode.GetID().ToString() + "," + nextNode.GetID().ToString());
                }
            }

            for (int i = 0; i < ArcList.Count; i++)
            {
                GeoNetworkArc geoNetworkArc = this.GetGeoNetworkArc(i);
                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(geoNetworkArc.GetStartPoint(), geoNetworkArc.GetEndPoint());
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(geoNetworkArc.GetStartPoint(), geoNetworkArc.GetEndPoint());
                }
            }

            RefreshNeighborMatrix();
            RefreshGridSpatialIndex();
            return true;
        }

        private double CalculateNodeKi(int nodeIndex)
        {
            if (nodeIndex < 0 || nodeIndex >= this.NodeNumber)
            {
                throw new Exception("Index Out of Range!");
            }
            double ki = 0;
            foreach (LinkValuePair link in this.neighborMatrix[nodeIndex])
            {
                ki += 1.0 / link.Value;
            }
            return ki;
        }

        public IEnumerable<NodeCluster> CommunityDetection_SpatialDBScan(double maxWeight, double radius)
        {
            double sumWeights = 0;
            for (int i = 0; i < this.NodeNumber; i++)
            {
                List<LinkValuePair> links = this.neighborMatrix[i];
                for (int j = 0; j < links.Count; j++)
                {
                    sumWeights += 1.0 / links[j].Value;
                }
            }

            List<NodeCluster> communityList = new List<NodeCluster>();

            bool[] isVisited = new bool[this.NodeList.Count];

            int clusterID = 0;

            for (int i = 0; i < this.NodeList.Count; i++)
            {
                if (isVisited[i] == true) continue;

                NodeCluster newCluster = new NodeCluster(clusterID++);

                Queue<int> nodeIndexQueue = new Queue<int>();
                nodeIndexQueue.Enqueue(i);
                isVisited[i] = true;
                double centerSumX = 0;
                double centerSumY = 0;
                int nodeCount = 0;

                while (nodeIndexQueue.Count != 0)
                {
                    int nowNodeIndex = nodeIndexQueue.Dequeue();
                    newCluster.nodeIDList.Add(nowNodeIndex);
                    GeoNetworkNode nowNode = this.NodeList[nowNodeIndex] as GeoNetworkNode;
                    centerSumX += nowNode.X;
                    centerSumY += nowNode.Y;
                    nodeCount += 1;

                    // 当前社团的中心点
                    double centerX = centerSumX / nodeCount;
                    double centerY = centerSumY / nodeCount;

                    List<LinkValuePair> links = this.neighborMatrix[nowNodeIndex];

                    foreach (LinkValuePair link in links)
                    {
                        int endNodeIndex = link.EndNodeIndex;

                        if (isVisited[endNodeIndex] == true) continue;

                        double weights = link.Value;

                        if (weights > maxWeight) continue;

                        GeoNetworkNode nextNode = this.NodeList[endNodeIndex] as GeoNetworkNode;
                        double distance;
                        if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(new Point(centerX, centerY), nextNode);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(new Point(centerX, centerY), nextNode);
                        }

                        if (distance > radius) continue;

                        // 模块度增益底层指标，目标社团的内部连接和KI
                        ClusterLinks_Converse(newCluster, out var sigmaIn, out var sigmaTot);

                        double ki = CalculateNodeKi(endNodeIndex);

                        double kiin = 1.0 / distance;

                        double deltaQ_1 = ((sigmaIn + kiin) / (2 * sumWeights) - Math.Pow((sigmaTot + ki) / (2 * sumWeights), 2));
                        double deltaQ_2 = (sigmaIn / (2 * sumWeights)) - Math.Pow(sigmaTot / (2 * sumWeights), 2) - Math.Pow(ki / (2 * sumWeights), 2);
                        double deltaQ = deltaQ_1 - deltaQ_2;

                        if (deltaQ < 0)
                        {
                            continue;
                        } 

                        nodeIndexQueue.Enqueue(endNodeIndex);
                        isVisited[endNodeIndex] = true;
                    }
                }

                communityList.Add(newCluster);
            }

            return communityList;
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xmin = this.NodeList.Min(item => ((GeoNetworkNode)item).X);
            double xmax = this.NodeList.Max(item => ((GeoNetworkNode)item).X);
            double ymin = this.NodeList.Min(item => ((GeoNetworkNode)item).Y);
            double ymax = this.NodeList.Max(item => ((GeoNetworkNode)item).Y);
            return new BoundaryBox(xmin, ymin, xmax, ymax);
        }
    }
}
