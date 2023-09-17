using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS._3DModel.Basic;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.Network;
using ThomasGIS.Vector;

namespace ThomasGIS._3DModel.Converter
{
    public static class ShapeMeshGenerator
    {
        public static TriMesh OptimizeTriMesh(TriMesh inputTriMesh)
        {
            HashSet<string> uniqueVertex = new HashSet<string>();
            HashSet<string> uniqueTexture = new HashSet<string>();
            List<string> uniqueVertexList = new List<string>();
            List<string> uniqueTextureList = new List<string>();
            TriMesh optimizedTriMesh = new TriMesh();
            for (int i = 0; i < inputTriMesh.VertexNumber; i++)
            {
                MeshVertex vertex = inputTriMesh.vertexList[i];
                string vertexUniqueID = vertex.ToString();

                if (!uniqueVertex.Contains(vertexUniqueID))
                {
                    uniqueVertex.Add(vertexUniqueID);
                    uniqueVertexList.Add(vertex.ToString());
                    optimizedTriMesh.AddVertex(vertex);
                }
            }

            for (int i = 0; i < inputTriMesh.TextureCoordinateNumber; i++)
            {
                IPoint uvPoint = inputTriMesh.textureCoordinateList[i];
                string textureUniqueID = uvPoint.GetX() + "-" + uvPoint.GetY();

                if (!uniqueTexture.Contains(textureUniqueID))
                {
                    uniqueTexture.Add(textureUniqueID);
                    uniqueTextureList.Add(textureUniqueID);
                    optimizedTriMesh.AddTextureCoordinate(uvPoint);
                }
            }

            for (int i = 0; i < inputTriMesh.FaceNumber; i++)
            {
                MeshFace oneFace = inputTriMesh.faceList[i];
                int originVertex_1 = oneFace.Vertex_1;
                int originVertex_2 = oneFace.Vertex_2;
                int originVertex_3 = oneFace.Vertex_3;

                string v1UniqueID = inputTriMesh.vertexList[originVertex_1].ToString();
                string v2UniqueID = inputTriMesh.vertexList[originVertex_2].ToString();
                string v3UniqueID = inputTriMesh.vertexList[originVertex_3].ToString();

                int newV1ID = uniqueVertexList.IndexOf(v1UniqueID);
                int newV2ID = uniqueVertexList.IndexOf(v2UniqueID);
                int newV3ID = uniqueVertexList.IndexOf(v3UniqueID);

                int texture_1 = oneFace.TextureCoordinate_1;
                int texture_2 = oneFace.TextureCoordinate_2;
                int texture_3 = oneFace.TextureCoordinate_3;

                string t1UniqueID = inputTriMesh.textureCoordinateList[texture_1].GetX() + "-" + inputTriMesh.textureCoordinateList[texture_1].GetY();
                string t2UniqueID = inputTriMesh.textureCoordinateList[texture_2].GetX() + "-" + inputTriMesh.textureCoordinateList[texture_2].GetY();
                string t3UniqueID = inputTriMesh.textureCoordinateList[texture_3].GetX() + "-" + inputTriMesh.textureCoordinateList[texture_3].GetY();

                int newT1ID = uniqueTextureList.IndexOf(t1UniqueID);
                int newT2ID = uniqueTextureList.IndexOf(t2UniqueID);
                int newT3ID = uniqueTextureList.IndexOf(t3UniqueID);

                optimizedTriMesh.AddFace(new MeshFace(newV1ID, newV2ID, newV3ID, newT1ID, newT2ID, newT3ID, -1, -1, -1, oneFace.MaterialIndex));
            }

            return optimizedTriMesh;
        }

        private static void CreateOneMesh(GeoNetwork geoNetwork, TriMesh newTriMesh, int level, double levelHeight, Dictionary<int, int> partIndex, int materialIndex)
        {
            BoundaryBox boundary = geoNetwork.GetBoundaryBox();

            int topStartIndex = newTriMesh.VertexNumber;
            for (int k = 0; k < geoNetwork.NodeNumber; k++)
            {
                IPoint topPoint = geoNetwork.GetGeoNetworkNode(k);
                double u = (topPoint.GetX() - boundary.XMin) / 10.0;
                double v = (topPoint.GetY() - boundary.YMin) / 10.0;
                newTriMesh.AddVertex(new MeshVertex(topPoint.GetX(), topPoint.GetY(), level * levelHeight, 0, 0, null));
                newTriMesh.AddTextureCoordinate(new Point(u, v));
            }

            int bottomStartIndex = newTriMesh.VertexNumber;
            for (int k = 0; k < geoNetwork.NodeNumber; k++)
            {
                IPoint topPoint = geoNetwork.GetGeoNetworkNode(k);
                double u = (topPoint.GetX() - boundary.XMin) / 10.0;
                double v = (topPoint.GetY() - boundary.YMin) / 10.0;
                newTriMesh.AddVertex(new MeshVertex(topPoint.GetX(), topPoint.GetY(), 0, 0, 0, null));
                newTriMesh.AddTextureCoordinate(new Point(u, v));
            }

            geoNetwork.RefreshNeighborMatrix();
            geoNetwork.DelaunaryTriangle();

            // 验证原边是否存在
            for (int k = 0; k < geoNetwork.NodeNumber; k++)
            {
                GeoNetworkNode n1 = geoNetwork.GetGeoNetworkNode(k);
                int n1Type = Convert.ToInt32(n1.GetProperty("Type"));
                int n1BaseIndex = partIndex[Math.Abs(n1Type)];

                int n2Index;
                GeoNetworkNode n2;
                if (k == geoNetwork.NodeNumber - 1)
                {
                    n2Index = n1BaseIndex;
                    n2 = geoNetwork.GetGeoNetworkNode(n2Index);
                }
                else
                {
                    n2Index = k + 1;
                    n2 = geoNetwork.GetGeoNetworkNode(n2Index);
                }

                int n2Type = Convert.ToInt32(n2.GetProperty("Type"));
                if (n1Type != n2Type)
                {
                    n2Index = n1BaseIndex;
                    n2 = geoNetwork.GetGeoNetworkNode(n2Index);
                }

                int edgeExist = geoNetwork.neighborMatrix[k].Where(item => item.EndNodeIndex == n2Index).Count();
                // 不存在就要生成这个边并且删除与其相交的边
                if (edgeExist == 0)
                {
                    for (int m = 0; m < geoNetwork.ArcNumber; m++)
                    {
                        GeoNetworkArc oneArc = geoNetwork.GetGeoNetworkArc(m);
                        bool isCross = TopoCalculator.IsCross2(n1, n2, oneArc.StartPoint, oneArc.EndPoint);
                        if (isCross)
                        {
                            // 相交的话把被相交的一条边变成两条边，但是看会不会重合，重合就不用加
                            GeoNetworkArc deleteArc = geoNetwork.GetGeoNetworkArc(m);
                            int deleteArcStartNodeID = deleteArc.GetStartNodeID();
                            int deleteArcEndNodeID = deleteArc.GetEndNodeID();

                            // 默认找 n1 吧，就是 j
                            if (geoNetwork.neighborMatrix[k].Where(item => item.EndNodeIndex == deleteArcStartNodeID).Count() == 0)
                            {
                                GeoNetworkNode deleteStartNode = geoNetwork.GetGeoNetworkNode(deleteArcStartNodeID);
                                geoNetwork.AddArc(new GeoNetworkArc(n1, deleteStartNode));
                            }

                            if (geoNetwork.neighborMatrix[k].Where(item => item.EndNodeIndex == deleteArcEndNodeID).Count() == 0)
                            {
                                GeoNetworkNode deleteEndNode = geoNetwork.GetGeoNetworkNode(deleteArcEndNodeID);
                                geoNetwork.AddArc(new GeoNetworkArc(n1, deleteEndNode));
                            }

                            geoNetwork.ArcList.RemoveAt(m);
                            m--;
                        }
                    }

                    geoNetwork.AddArc(new GeoNetworkArc(n1, n2));
                }
            }

            geoNetwork.RefreshNeighborMatrix();

            Stack<int> arcStack = new Stack<int>();
            arcStack.Push(0);
            bool[] isVisited = new bool[geoNetwork.ArcNumber];
            isVisited[0] = true;

            HashSet<string> faceFlag = new HashSet<string>();
            while (arcStack.Count > 0)
            {
                int arcIndex = arcStack.Pop();
                GeoNetworkArc oneArc = geoNetwork.GetGeoNetworkArc(arcIndex);
                int startNodeID = oneArc.GetStartNodeID();
                int endNodeID = oneArc.GetEndNodeID();

                HashSet<int> startNodeNeighbor = new HashSet<int>();
                HashSet<int> endNodeNeighbor = new HashSet<int>();

                List<LinkValuePair> startNodeLinks = geoNetwork.neighborMatrix[startNodeID];
                List<LinkValuePair> endNodeLinks = geoNetwork.neighborMatrix[endNodeID];

                foreach (LinkValuePair oneLink in startNodeLinks)
                {
                    startNodeNeighbor.Add(oneLink.EndNodeIndex);
                }

                foreach (LinkValuePair oneLink in endNodeLinks)
                {
                    endNodeNeighbor.Add(oneLink.EndNodeIndex);
                }

                startNodeNeighbor.IntersectWith(endNodeNeighbor);

                foreach (int topNodeIndex in startNodeNeighbor)
                {
                    List<int> triangleNodes = new List<int>();
                    List<IPoint> facePointList = new List<IPoint>();

                    triangleNodes.Add(startNodeID);
                    triangleNodes.Add(endNodeID);
                    triangleNodes.Add(topNodeIndex);
                    triangleNodes.Sort();

                    facePointList.Add(geoNetwork.GetGeoNetworkNode(triangleNodes[0]));
                    facePointList.Add(geoNetwork.GetGeoNetworkNode(triangleNodes[1]));
                    facePointList.Add(geoNetwork.GetGeoNetworkNode(triangleNodes[2]));

                    string uniqueID = triangleNodes[0].ToString() + "-" + triangleNodes[1].ToString() + "-" + triangleNodes[2].ToString();

                    if (!faceFlag.Contains(uniqueID))
                    {
                        faceFlag.Add(uniqueID);
                        double area = DistanceCalculator.SpatialArea(facePointList);
                        bool sameObject = true;
                        int baseType = Convert.ToInt32(geoNetwork.GetGeoNetworkNode(triangleNodes[0]).GetProperty("Type"));

                        for (int k = 1; k < triangleNodes.Count; k++)
                        {
                            int type = Convert.ToInt32(geoNetwork.GetGeoNetworkNode(triangleNodes[k]).GetProperty("Type"));
                            if (type != baseType)
                            {
                                sameObject = false;
                                break;
                            }
                        }

                        if ((sameObject == true && area > 0) || sameObject == false)
                        {
                            // 面积为负数就倒过来
                            if (area < 0)
                            {
                                newTriMesh.AddFace(new MeshFace(triangleNodes[0] + topStartIndex, triangleNodes[1] + topStartIndex, triangleNodes[2] + topStartIndex, triangleNodes[0] + topStartIndex, triangleNodes[1] + topStartIndex, triangleNodes[2] + topStartIndex, -1, -1, -1, materialIndex));
                                newTriMesh.AddFace(new MeshFace(triangleNodes[2] + bottomStartIndex, triangleNodes[1] + bottomStartIndex, triangleNodes[0] + bottomStartIndex, triangleNodes[2] + bottomStartIndex, triangleNodes[1] + bottomStartIndex, triangleNodes[0] + bottomStartIndex, -1, -1, -1, materialIndex));
                            }
                            else
                            {
                                newTriMesh.AddFace(new MeshFace(triangleNodes[2] + topStartIndex, triangleNodes[1] + topStartIndex, triangleNodes[0] + topStartIndex, triangleNodes[2] + topStartIndex, triangleNodes[1] + topStartIndex, triangleNodes[0] + topStartIndex, -1, -1, -1, materialIndex));
                                newTriMesh.AddFace(new MeshFace(triangleNodes[0] + bottomStartIndex, triangleNodes[1] + bottomStartIndex, triangleNodes[2] + bottomStartIndex, triangleNodes[0] + bottomStartIndex, triangleNodes[1] + bottomStartIndex, triangleNodes[2] + bottomStartIndex, -1, -1, -1, materialIndex));
                            }

                        }

                        int findIndex1 = startNodeLinks.Where(item => item.EndNodeIndex == topNodeIndex).First().ArcIndex;
                        int findIndex2 = endNodeLinks.Where(item => item.EndNodeIndex == topNodeIndex).First().ArcIndex;
                        if (!isVisited[findIndex1])
                        {
                            arcStack.Push(findIndex1);
                            isVisited[findIndex1] = true;
                        }

                        if (!isVisited[findIndex2])
                        {
                            arcStack.Push(findIndex2);
                            isVisited[findIndex2] = true;
                        }
                    }
                }
            }
        }

        public static MeshScene CreateBuilding(IShapefile shapefile, string heightFieldName, double levelHeight)
        {
            if (shapefile.GetCoordinateRef().GetCoordinateType() != Coordinates.CoordinateType.Projected)
            {
                Console.WriteLine("未经过投影的Shapefile，暂未支持！");
                return new MeshScene();
            }

            if (shapefile.GetFeatureType() != 5)
            {
                Console.WriteLine("请输入Polygon类型的建筑！");
                return new MeshScene();
            }

            MeshScene meshScene = new MeshScene();

            for(int i = 0; i < shapefile.GetFeatureNumber(); i++)
            {
                Random rd = new Random();
                int typeValue = rd.Next();
                int materialIndex = 2;
                if (typeValue % 3 == 0)
                {
                    materialIndex = 2;
                }
                else if (typeValue % 3 == 1)
                {
                    materialIndex = 6;
                }
                else
                {
                    materialIndex = 7;
                }

                TriMesh newTriMesh = new TriMesh();

                double height = shapefile.GetFieldValueAsDouble(i, heightFieldName);
                int level = (int)(height / levelHeight) + 1;

                ShpPolygon polygon = shapefile.GetFeature(i) as ShpPolygon;

                int partNumber = polygon.GetPartNumber();

                for (int m = 0; m < partNumber; m++)
                {
                    List<IPoint> pointList = polygon.GetPartByIndex(m).ToList();
                    double dColor = 255.0 / level;
                    double dAlpha = 1.0 / level;

                    // 建筑分层
                    for (int j = 0; j < level; j++)
                    {
                        int R = (int)dColor * j;
                        int G = (int)dColor * j;
                        int B = (int)dColor * j;
                        double Alpha = dAlpha * j;

                        double sumDistance = 0;

                        // 每层需要遍历一次所有点
                        for (int k = 0; k < pointList.Count - 1; k++)
                        {
                            IPoint sourcePoint = pointList[k];
                            IPoint targetPoint = pointList[k + 1];

                            double distance = DistanceCalculator.SpatialDistance(sourcePoint, targetPoint);

                            double lowHeight = j * levelHeight;
                            double highHeight = (j + 1) * levelHeight;

                            double startX = sumDistance / 10.0;
                            double endX = (sumDistance + distance) / 10.0;

                            double lX = sumDistance % 10;
                            double rX = (sumDistance + distance) % 10;

                            if (lX > 1.0 && lX < 4.5)
                            {
                                startX = startX - lX / 10.0 + 0.1;
                            }
                            else if (lX > 5.5 && lX < 9.0)
                            {
                                startX = startX - lX / 10.0 + 0.9;
                            }

                            if (rX > 1.0 && rX < 4.5)
                            {
                                endX = endX - rX / 10.0 + 0.1;
                            }
                            else if (rX > 5.5 && rX < 9.0)
                            {
                                endX = endX - rX / 10.0 + 0.9;
                            }

                            IPoint tc1 = new Point(startX, lowHeight / 8.99);
                            IPoint tc2 = new Point(startX, highHeight / 8.99);
                            IPoint tc3 = new Point(endX, lowHeight / 8.99);
                            IPoint tc4 = new Point(endX, highHeight / 8.99);

                            sumDistance += distance;

                            MeshVertex v1 = new MeshVertex(sourcePoint.GetX(), sourcePoint.GetY(), lowHeight, 0, 0, null);
                            MeshVertex v2 = new MeshVertex(sourcePoint.GetX(), sourcePoint.GetY(), highHeight, 0, 0, null);
                            MeshVertex v3 = new MeshVertex(targetPoint.GetX(), targetPoint.GetY(), lowHeight, 0, 0, null);
                            MeshVertex v4 = new MeshVertex(targetPoint.GetX(), targetPoint.GetY(), highHeight, 0, 0, null);

                            int v1Index = newTriMesh.AddVertex(v1);
                            int v2Index = newTriMesh.AddVertex(v2);
                            int v3Index = newTriMesh.AddVertex(v3);
                            int v4Index = newTriMesh.AddVertex(v4);

                            int tc1Index = newTriMesh.AddTextureCoordinate(tc1);
                            int tc2Index = newTriMesh.AddTextureCoordinate(tc2);
                            int tc3Index = newTriMesh.AddTextureCoordinate(tc3);
                            int tc4Index = newTriMesh.AddTextureCoordinate(tc4);

                            int materialID = -1;
                            if (height > 30 && distance < 5)
                            {
                                materialID = 1;
                            }
                            else if (height > 30 && distance >= 5)
                            {
                                materialID = 0;
                            }
                            else if (height <= 30 && distance < 5)
                            {
                                materialID = 5;
                            }
                            else
                            {
                                materialID = 4;
                            }

                            MeshFace f1 = new MeshFace(v1Index, v2Index, v3Index, tc1Index, tc2Index, tc3Index, -1, -1, -1, materialID);
                            MeshFace f2 = new MeshFace(v2Index, v4Index, v3Index, tc2Index, tc4Index, tc3Index, -1, -1, -1, materialID);
                            newTriMesh.AddFace(f1);
                            newTriMesh.AddFace(f2);
                        }
                    }
                }

                // 加个底和顶，MultiPolygon要分开求，遇到下一个正的或者最后一个Part为一次分隔
                List<double> areas = new List<double>(polygon.GetPartsArea());
                GeoNetwork geoNetwork = new GeoNetwork(shapefile.GetCoordinateRef());
                Dictionary<int, int> partIndex = new Dictionary<int, int>();

                for (int j = 0; j < areas.Count; j++)
                {
                    if (areas[j] > 0)
                    {
                        if (geoNetwork.NodeNumber > 0)
                        {
                            CreateOneMesh(geoNetwork, newTriMesh, level, levelHeight, partIndex, materialIndex);
                            geoNetwork = new GeoNetwork(shapefile.GetCoordinateRef());
                        }
                    }

                    List<IPoint> onePointList = polygon.GetPartByIndex(j).ToList();
                    partIndex.Add(j + 1, geoNetwork.NodeNumber);
                    for (int k = 0; k < onePointList.Count - 1; k++)
                    {
                        IPoint partPoint = onePointList[k];
                        Dictionary<string, object> properties = new Dictionary<string, object>();
                        if (areas[j] > 0)
                        {
                            properties.Add("Type", j + 1);
                        }
                        else
                        {
                            properties.Add("Type", -(j + 1));
                        }
                        geoNetwork.AddNode(new GeoNetworkNode(geoNetwork.NodeNumber, partPoint.GetX(), partPoint.GetY(), properties)); 
                    }
                }

                if (geoNetwork.NodeNumber > 0)
                {
                    CreateOneMesh(geoNetwork, newTriMesh, level, levelHeight, partIndex, materialIndex);
                }

                meshScene.AddTriMesh(OptimizeTriMesh(newTriMesh));
            }

            return meshScene;
        }

        public static MeshScene CreateRoad(IShapefile shapefile, string widthFieldName)
        {
            return null;
        }
    }
}
