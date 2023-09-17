using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.Mesh.Vector;
using ThomasGIS.SpatialIndex;
using ThomasGIS.Vector;

namespace ThomasGIS.Network
{
    public class GeoNetwork3D : GeoNetwork
    {
        public GeoNetwork3D(CoordinateBase coordinateSystem) : base(coordinateSystem)
        {
        
        }

        public GeoNetwork3D(IShapefile roadShapefile3D, bool splitShapePoint = true) : base(roadShapefile3D.GetCoordinateRef())
        {
            if (splitShapePoint)
            {
                HashSet<string> pointSet = new HashSet<string>();
                // 从shapefile里拿到所有的道路节点构建顶点集合V
                for (int i = 0; i < roadShapefile3D.GetFeatureNumber(); i++)
                {
                    ShpPolyline3D polyline = roadShapefile3D.GetFeature(i) as ShpPolyline3D;

                    if (polyline == null) continue;

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
                            pointSet.Add($"{polyline.PointList[k].GetX()},{polyline.PointList[k].GetY()},{polyline.ZList[k]}");
                        }
                    }
                }

                List<string> pointStrList = pointSet.ToList();
                pointSet.Clear();

                BoundaryBox mbr = roadShapefile3D.GetBoundaryBox();
                double scale = (mbr.XMax - mbr.XMin) / 1000.0;
                if (roadShapefile3D.GetCoordinateRef().GetCoordinateType() == CoordinateType.Geographic)
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
                    double z = Convert.ToDouble(coordinate[2]);
                    GeoNetworkNode3D newNode = new GeoNetworkNode3D(this.NodeNumber, new Vector3D(x, y, z), null);
                    this.AddNode(newNode);
                    pointSpatialIndex.AddItem(newNode);
                }

                pointSpatialIndex.RefreshIndex();

                // 构建点与边的关系
                for (int i = 0; i < roadShapefile3D.GetFeatureNumber(); i++)
                {
                    ShpPolyline3D polyline = roadShapefile3D.GetFeature(i) as ShpPolyline3D;

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
                            double startPointZ = polyline.ZList[k];
                            double endPointZ = polyline.ZList[k + 1];

                            if (startPoint.GetX() == endPoint.GetX() && startPoint.GetY() == endPoint.GetY()) continue;

                            IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(startPoint, scale);
                            IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(endPoint, scale);

                            GeoNetworkNode3D startNode = null;
                            GeoNetworkNode3D endNode = null;

                            foreach (int index in startNodeCandicateIndex)
                            {
                                GeoNetworkNode3D checkNode = this.NodeList[index] as GeoNetworkNode3D;
                                if (Math.Abs(checkNode.X - startPoint.GetX()) < 0.0000000001 && Math.Abs(checkNode.Y - startPoint.GetY()) < 0.0000000001 && Math.Abs(checkNode.Z - startPointZ) < 0.0000000001)
                                {
                                    startNode = checkNode;
                                    break;
                                }
                            }

                            foreach (int index in endNodeCandicateIndex)
                            {
                                GeoNetworkNode3D checkNode = this.NodeList[index] as GeoNetworkNode3D;
                                if (Math.Abs(checkNode.X - endPoint.GetX()) < 0.0000000001 && Math.Abs(checkNode.Y - endPoint.GetY()) < 0.0000000001 && Math.Abs(checkNode.Z - endPointZ) < 0.0000000001)
                                {
                                    endNode = checkNode;
                                    break;
                                }
                            }

                            if (startNode is null || endNode is null) throw new Exception("Big Error!");

                            Dictionary<string, object> dataField = new Dictionary<string, object>();
                            IEnumerable<string> fieldNames = roadShapefile3D.GetFieldNames();
                            foreach (string fieldName in fieldNames)
                            {
                                dataField.Add(fieldName, roadShapefile3D.GetFieldValueAsString(i, fieldName));
                            }
                            dataField.Add("SegmentID", i);

                            this.AddArc(new GeoNetworkArc3D(startNode, endNode, dataField));
                        }
                    }
                }
            }
        }

        public override BoundaryBox _CalculateBoundaryBox()
        {
            if (this.NodeList.Count == 0) return null;

            double xmax = this.NodeList.Max(node => ((GeoNetworkNode3D)node).GetX());
            double xmin = this.NodeList.Min(node => ((GeoNetworkNode3D)node).GetX());
            double ymax = this.NodeList.Max(node => ((GeoNetworkNode3D)node).GetY());
            double ymin = this.NodeList.Min(node => ((GeoNetworkNode3D)node).GetY());
            double zmin = this.NodeList.Min(node => ((GeoNetworkNode3D)node).GetZ());
            double zmax = this.NodeList.Max(node => ((GeoNetworkNode3D)node).GetZ());

            return new BoundaryBox(xmin, ymin, xmax, ymax, zmin, zmax);
        }

        public override bool AddNode(INetworkNode newNode)
        {
            if (newNode.GetType() != typeof(GeoNetworkNode3D))
            {
                throw new Exception("GeoNetwork3D仅支持添加GeoNetworkNode3D类型的节点！");
            }

            this.NodeList.Add(newNode);

            return true;
        }

        public override bool AddArc(INetworkArc newArc)
        {
            if (newArc.GetType() != typeof(GeoNetworkArc3D))
            {
                throw new Exception("GeoNetwork3D仅支持添加GeoNetworkArc3D类型的连边！");
            }

            this.ArcList.Add(newArc);

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
                GeoNetworkArc3D oneArc = this.ArcList[i] as GeoNetworkArc3D;

                IEnumerable<int> startNodeCandicateIndex = pointSpatialIndex.SearchID(new Point(oneArc.StartPoint.GetX(), oneArc.StartPoint.GetY()), this.pointSpatialIndex.GetScale());
                IEnumerable<int> endNodeCandicateIndex = pointSpatialIndex.SearchID(new Point(oneArc.EndPoint.GetX(), oneArc.EndPoint.GetY()), this.pointSpatialIndex.GetScale());

                int startNodeIndex = -1, endNodeIndex = -1;

                foreach (int index in startNodeCandicateIndex)
                {
                    GeoNetworkNode3D checkNode = this.NodeList[index] as GeoNetworkNode3D;
                    if (checkNode.GetID() == oneArc.GetStartNodeID())
                    {
                        startNodeIndex = index;
                        break;
                    }
                }

                foreach (int index in endNodeCandicateIndex)
                {
                    GeoNetworkNode3D checkNode = this.NodeList[index] as GeoNetworkNode3D;
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
                    // 算3D距离
                    double distance;
                    if (networkCoordinateSystem.GetCoordinateType() == CoordinateType.Projected)
                    {
                        distance = DistanceCalculator.SpatialDistance3D(oneArc.StartPoint as GeoNetworkNode3D, oneArc.EndPoint as GeoNetworkNode3D);
                    }
                    else
                    {
                        distance = DistanceCalculator.SpatialDistance3DGeo(oneArc.StartPoint as GeoNetworkNode3D, oneArc.EndPoint as GeoNetworkNode3D);
                    }
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, distance, i));
                    this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(startNodeIndex, distance, i));
                }
            }

            this.nowMatrixField = dataField;
            this.networkChangeFlag = false;
        }

        public override IShapefile ExportNodesToShapefile()
        {
            // 识别所有弧段中的属性
            Dictionary<string, DBFFieldType> fieldDict = new Dictionary<string, DBFFieldType>();

            for (int i = 0; i < NodeNumber; i++)
            {
                GeoNetworkNode3D oneNode = this.NodeList[i] as GeoNetworkNode3D;
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
            IShapefile nodeShapefile = VectorFactory.CreateShapefile(ESRIShapeType.PointZ);

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
                GeoNetworkNode3D oneNode = this.NodeList[i] as GeoNetworkNode3D;
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

        public override IShapefile ExportArcsToShapefile()
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
            IShapefile arcShapefile = VectorFactory.CreateShapefile(ESRIShapeType.PolylineZ);

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
                GeoNetworkArc3D oneArc = this.ArcList[i] as GeoNetworkArc3D;
                ShpPolyline3D newPolyline = new ShpPolyline3D();
                List<Point> pointList = new List<Point>();
                List<double> zList = new List<double>();
                List<double> mList = new List<double>();
                pointList.Add(new Point(oneArc.StartPoint.GetX(), oneArc.StartPoint.GetY()));
                pointList.Add(new Point(oneArc.EndPoint.GetX(), oneArc.EndPoint.GetY()));
                zList.Add(((GeoNetworkNode3D)oneArc.StartPoint).GetZ());
                zList.Add(((GeoNetworkNode3D)oneArc.EndPoint).GetZ());
                mList.Add(0);
                mList.Add(0);
                newPolyline.AddPart(pointList, zList, mList);
                int featureIndex = arcShapefile.AddFeature(newPolyline, oneArc.GetPropertySheet());
                arcShapefile.SetValue(featureIndex, "SNodeID", oneArc.GetStartNodeID());
                arcShapefile.SetValue(featureIndex, "ENodeID", oneArc.GetEndNodeID());
            }

            // 设置shapefile的坐标系统
            arcShapefile.SetCoordinateRef(this.networkCoordinateSystem);

            return arcShapefile;
        }

        public override sealed Route SolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
        {
            throw new Exception("Network3D中请使用参数为Vector3D的SolvePath方法求解");
        }

        public override sealed Route GreedySolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance)
        {
            throw new Exception("Network3D中请使用参数为Vector3D的SolvePath方法求解");
        }

        public Route SolvePath(Vector3D startPoint, Vector3D endPoint, string powerField, double tolerateDistance)
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
                GeoNetworkArc3D candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc3D;
                // 3D需要先判断层高！然后进一步看起始点和终止点位于哪条线上
                Vector3D arcStartLoc = (GeoNetworkNode3D)candicateArc.StartPoint;
                Vector3D arcEndLoc = (GeoNetworkNode3D)candicateArc.EndPoint;
                Vector3D direction = (arcEndLoc - arcStartLoc).Normalize();
                // 高度和平面距离按5：1给权重，优先平面近似的位置
                double differZ;
                if (startPoint.Z >= Math.Min(arcStartLoc.Z, arcEndLoc.Z) && startPoint.Z <= Math.Max(arcStartLoc.Z, arcEndLoc.Z))
                {
                    differZ = 0;
                }
                else
                {
                    differZ = Math.Min(Math.Abs(Math.Min(arcStartLoc.Z, arcEndLoc.Z) - startPoint.Z), Math.Abs(Math.Max(arcStartLoc.Z, arcEndLoc.Z) - startPoint.Z));
                }

                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, startPoint);
                    distance += 5 * differZ;
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, startPoint);
                    distance += 5 * differZ;
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
                GeoNetworkArc3D candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc3D;
                // 3D需要先判断层高！然后进一步看起始点和终止点位于哪条线上
                Vector3D arcStartLoc = (GeoNetworkNode3D)candicateArc.StartPoint;
                Vector3D arcEndLoc = (GeoNetworkNode3D)candicateArc.EndPoint;
                Vector3D direction = (arcEndLoc - arcStartLoc).Normalize();
                // 高度和平面距离按5：1给权重，优先平面近似的位置
                double differZ;
                if (endPoint.Z >= Math.Min(arcStartLoc.Z, arcEndLoc.Z) && endPoint.Z <= Math.Max(arcStartLoc.Z, arcEndLoc.Z))
                {
                    differZ = 0;
                }
                else
                {
                    differZ = Math.Min(Math.Abs(Math.Min(arcStartLoc.Z, arcEndLoc.Z) - endPoint.Z), Math.Abs(Math.Max(arcStartLoc.Z, arcEndLoc.Z) - endPoint.Z));
                }

                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, endPoint);
                    distance += 5.0 * differZ;
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, endPoint);
                    distance += 5.0 * differZ;
                }

                if (distance < minEndDistance)
                {
                    minEndDistanceIndex = candidateArcIndex;
                    minEndDistance = distance;
                }
            }

            GeoNetworkArc3D startArc = ArcList[minStartDistanceIndex] as GeoNetworkArc3D;
            GeoNetworkArc3D endArc = ArcList[minEndDistanceIndex] as GeoNetworkArc3D;

            GeoNetworkNode3D startArcStartNode = (GeoNetworkNode3D)startArc.GetStartPoint();
            GeoNetworkNode3D startArcEndNode = (GeoNetworkNode3D)startArc.GetEndPoint();
            GeoNetworkNode3D endArcStartNode = (GeoNetworkNode3D)endArc.GetStartPoint();
            GeoNetworkNode3D endArcEndNode = (GeoNetworkNode3D)endArc.GetEndPoint();

            Vector3D startTruePoint = DistanceCalculator.CrossPoint3D(startArcStartNode, startArcEndNode, startPoint);
            Vector3D endTruePoint = DistanceCalculator.CrossPoint3D(endArcStartNode, endArcEndNode, endPoint);

            double startCrossPointToStartNodeDistance, startCrossPointToEndNodeDistance;
            double endCrossPointToStartNodeDistance, endCrossPointToEndNodeDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3DGeo(startTruePoint, startArcStartNode);
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3DGeo(startTruePoint, startArcEndNode);
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3DGeo(endTruePoint, endArcStartNode);
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3DGeo(endTruePoint, endArcEndNode);
            }
            else
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3D(startTruePoint, startArcStartNode);
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3D(startTruePoint, startArcEndNode);
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3D(endTruePoint, endArcStartNode);
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3D(endTruePoint, endArcEndNode);
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

            route.RouteNodes.Insert(0, new GeoNetworkNode3D(-1, new Vector3D(startTruePoint), null));

            if (route.RouteArcs.Last() == minEndDistanceIndex)
            {
                route.RouteNodes.RemoveAt(route.RouteNodes.Count - 1);
                route.Impedance -= endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Add(new GeoNetworkNode3D(-1, new Vector3D(endTruePoint), null));

            return route;
        }

        public Route GreedySolvePath(Vector3D startPoint, Vector3D endPoint, string powerField, double tolerateDistance)
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
                GeoNetworkArc3D candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc3D;
                // 3D需要先判断层高！然后进一步看起始点和终止点位于哪条线上
                Vector3D arcStartLoc = (GeoNetworkNode3D)candicateArc.StartPoint;
                Vector3D arcEndLoc = (GeoNetworkNode3D)candicateArc.EndPoint;
                Vector3D direction = (arcEndLoc - arcStartLoc).Normalize();
                // 高度和平面距离按5：1给权重，优先平面近似的位置
                double differZ;
                if (startPoint.Z >= Math.Min(arcStartLoc.Z, arcEndLoc.Z) && startPoint.Z <= Math.Max(arcStartLoc.Z, arcEndLoc.Z))
                {
                    differZ = 0;
                }
                else
                {
                    differZ = Math.Min(Math.Abs(Math.Min(arcStartLoc.Z, arcEndLoc.Z) - startPoint.Z), Math.Abs(Math.Max(arcStartLoc.Z, arcEndLoc.Z) - startPoint.Z));
                }

                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, startPoint);
                    distance += 5 * differZ;
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, startPoint);
                    distance += 5 * differZ;
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
                GeoNetworkArc3D candicateArc = ArcList[candidateArcIndex] as GeoNetworkArc3D;
                // 3D需要先判断层高！然后进一步看起始点和终止点位于哪条线上
                Vector3D arcStartLoc = (GeoNetworkNode3D)candicateArc.StartPoint;
                Vector3D arcEndLoc = (GeoNetworkNode3D)candicateArc.EndPoint;
                Vector3D direction = (arcEndLoc - arcStartLoc).Normalize();
                // 高度和平面距离按5：1给权重，优先平面近似的位置
                double differZ;
                if (endPoint.Z >= Math.Min(arcStartLoc.Z, arcEndLoc.Z) && endPoint.Z <= Math.Max(arcStartLoc.Z, arcEndLoc.Z))
                {
                    differZ = 0;
                }
                else
                {
                    differZ = Math.Min(Math.Abs(Math.Min(arcStartLoc.Z, arcEndLoc.Z) - endPoint.Z), Math.Abs(Math.Max(arcStartLoc.Z, arcEndLoc.Z) - endPoint.Z));
                }

                double distance;
                if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                {
                    distance = DistanceCalculator.SpatialDistanceGeo(candicateArc, endPoint);
                    distance += 5.0 * differZ;
                }
                else
                {
                    distance = DistanceCalculator.SpatialDistance(candicateArc, endPoint);
                    distance += 5.0 * differZ;
                }

                if (distance < minEndDistance)
                {
                    minEndDistanceIndex = candidateArcIndex;
                    minEndDistance = distance;
                }
            }

            GeoNetworkArc3D startArc = ArcList[minStartDistanceIndex] as GeoNetworkArc3D;
            GeoNetworkArc3D endArc = ArcList[minEndDistanceIndex] as GeoNetworkArc3D;

            GeoNetworkNode3D startArcStartNode = (GeoNetworkNode3D)startArc.GetStartPoint();
            GeoNetworkNode3D startArcEndNode = (GeoNetworkNode3D)startArc.GetEndPoint();
            GeoNetworkNode3D endArcStartNode = (GeoNetworkNode3D)endArc.GetStartPoint();
            GeoNetworkNode3D endArcEndNode = (GeoNetworkNode3D)endArc.GetEndPoint();

            Vector3D startTruePoint = DistanceCalculator.CrossPoint3D(startArcStartNode, startArcEndNode, startPoint);
            Vector3D endTruePoint = DistanceCalculator.CrossPoint3D(endArcStartNode, endArcEndNode, endPoint);

            double startCrossPointToStartNodeDistance, startCrossPointToEndNodeDistance;
            double endCrossPointToStartNodeDistance, endCrossPointToEndNodeDistance;

            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3DGeo(startTruePoint, startArcStartNode);
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3DGeo(startTruePoint, startArcEndNode);
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3DGeo(endTruePoint, endArcStartNode);
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3DGeo(endTruePoint, endArcEndNode);
            }
            else
            {
                startCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3D(startTruePoint, startArcStartNode);
                startCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3D(startTruePoint, startArcEndNode);
                endCrossPointToStartNodeDistance = DistanceCalculator.SpatialDistance3D(endTruePoint, endArcStartNode);
                endCrossPointToEndNodeDistance = DistanceCalculator.SpatialDistance3D(endTruePoint, endArcEndNode);
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

            route.RouteNodes.Insert(0, new GeoNetworkNode3D(-1, new Vector3D(startTruePoint), null));

            if (route.RouteArcs.Last() == minEndDistanceIndex)
            {
                route.RouteNodes.RemoveAt(route.RouteNodes.Count - 1);
                route.Impedance -= endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }
            else
            {
                route.Impedance += endCrossPointToStartNodeDistance < endCrossPointToEndNodeDistance ? endCrossPointToStartNodeDistance : endCrossPointToEndNodeDistance;
            }

            route.RouteNodes.Add(new GeoNetworkNode3D(-1, new Vector3D(endTruePoint), null));

            return route;
        }

        public override Route SolvePath(int startNodeIndex, int endNodeIndex, string powerField)
        {
            GeoNetworkNode3D originNode = NodeList[startNodeIndex] as GeoNetworkNode3D;
            GeoNetworkNode3D destinationNode = NodeList[endNodeIndex] as GeoNetworkNode3D;

            double directDistance;
            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                directDistance = DistanceCalculator.SpatialDistance3DGeo(originNode, destinationNode);
            }
            else
            {
                directDistance = DistanceCalculator.SpatialDistance3D(originNode, destinationNode);
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

                    GeoNetworkNode3D nowNode = NodeList[nodeIndex] as GeoNetworkNode3D;
                    GeoNetworkNode3D targetNode = NodeList[endNodeIndex] as GeoNetworkNode3D;

                    // 距离终点的直线距离
                    if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                    {
                        H = DistanceCalculator.SpatialDistance3DGeo(nowNode, targetNode);
                    }
                    else
                    {
                        H = DistanceCalculator.SpatialDistance3D(nowNode, targetNode);
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

        public override Route GreedySolvePath(int startNodeIndex, int endNodeIndex, string powerField)
        {
            GeoNetworkNode3D originNode = NodeList[startNodeIndex] as GeoNetworkNode3D;
            GeoNetworkNode3D destinationNode = NodeList[endNodeIndex] as GeoNetworkNode3D;

            double directDistance;
            if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                directDistance = DistanceCalculator.SpatialDistance3DGeo(originNode, destinationNode);
            }
            else
            {
                directDistance = DistanceCalculator.SpatialDistance3D(originNode, destinationNode);
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

                    GeoNetworkNode3D nowNode = NodeList[nodeIndex] as GeoNetworkNode3D;
                    GeoNetworkNode3D targetNode = NodeList[endNodeIndex] as GeoNetworkNode3D;

                    // 距离终点的直线距离
                    if (this.networkCoordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
                    {
                        H = DistanceCalculator.SpatialDistance3DGeo(nowNode, targetNode);
                    }
                    else
                    {
                        H = DistanceCalculator.SpatialDistance3D(nowNode, targetNode);
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
    }
}
