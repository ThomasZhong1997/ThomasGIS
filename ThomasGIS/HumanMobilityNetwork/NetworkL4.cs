using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Network;
using System.Linq;

namespace ThomasGIS.HumanMobilityNetwork
{
    public class NetworkL4 : GeoNetwork
    {
        // Params:
        // coordinateSystem: 网络的坐标系统
        public NetworkL4(CoordinateBase coordinateSystem) : base(coordinateSystem)
        {

        }

        public NetworkL3 GenerateUpperNetwork(int clusterNumber, string targetFlag)
        {            
            // 执行社团探测算法
            // IEnumerable<NodeCluster> clusters = CommunityDetection_GN(clusterNumber);
            // IEnumerable<NodeCluster> clusters = CommunityDetection_Louvain(1);
            IEnumerable<NodeCluster> clusters = CommunityDetection_SpatialDBScan(1000, 2000);

            foreach (NodeCluster cluster in clusters)
            {
                foreach (int nodeID in cluster.nodeIDList)
                {
                    this.NodeList[nodeID].SetProperty("Cluster", cluster.ID);
                }
            }

            // 新建L3级别对象
            NetworkL3 networkL3 = new NetworkL3(networkCoordinateSystem);

            // 计算每个社团的中心点位置与聚散度
            foreach (NodeCluster cluster in clusters)
            {
                // 中心点位置
                double clusterCenterX = 0;
                double clusterCenterY = 0;

                int gatherDegree = 0;
                int scatterDegree = 0;

                foreach (int nodeIndex in cluster.nodeIDList)
                {
                    // 隶属于社团的每个点坐标和属性值
                    GeoNetworkNode oneNode = NodeList[nodeIndex] as GeoNetworkNode;
                    clusterCenterX += oneNode.X;
                    clusterCenterY += oneNode.Y;

                    if (oneNode.GetProperty("Origin").ToString() == targetFlag)
                    {
                        scatterDegree += 1;
                    }

                    if (oneNode.GetProperty("Destination").ToString() == targetFlag)
                    {
                        gatherDegree += 1;
                    }
                }

                clusterCenterX /= cluster.nodeIDList.Count;
                clusterCenterY /= cluster.nodeIDList.Count;

                GeoNetworkNode newL3Node = new GeoNetworkNode(cluster.ID, clusterCenterX, clusterCenterY);

                newL3Node.SetProperty("CID", cluster.ID);
                // 添加节点的基本属性，内部的节点数量
                newL3Node.SetProperty("InnerNumber", cluster.nodeIDList.Count);
                // 添加聚度和散度
                newL3Node.SetProperty("Gather", gatherDegree);
                newL3Node.SetProperty("Scatter", scatterDegree);

                // 节点加入L3级网络
                networkL3.AddNode(newL3Node, cluster);
            }

            // 依次计算每个社团的中心点位置与基本属性
            foreach (NodeCluster cluster in clusters)
            {
                // 计算均质性
                int sameLink = 0;
                int differentLink = 0;

                int[] linkNumber = new int[clusters.Count()];

                foreach (int nodeIndex in cluster.nodeIDList)
                {
                    // 隶属于社团的每个点坐标和属性值
                    GeoNetworkNode oneNode = NodeList[nodeIndex] as GeoNetworkNode;

                    // 社团内部连线的虚实统计
                    List<LinkValuePair> nodeLinks = this.neighborMatrix[nodeIndex];
                    foreach (LinkValuePair pair in nodeLinks)
                    {
                        if (cluster.nodeIDList.Contains(pair.EndNodeIndex))
                        {
                            INetworkArc networkArc = ArcList[pair.ArcIndex];
                            if (Convert.ToInt32(networkArc.GetProperty("LinkType")) == 0)
                            {
                                differentLink += 1;
                            }
                            else
                            {
                                sameLink += 1;
                            }
                        }
                        else
                        {
                            // 社团至其它社团的连边统计
                            foreach (NodeCluster otherCluster in clusters)
                            {
                                if (cluster.ID != otherCluster.ID)
                                {
                                    if (otherCluster.nodeIDList.Contains(pair.EndNodeIndex))
                                    {
                                        linkNumber[otherCluster.ID] += 1;
                                    }
                                }
                            }
                        }
                    }
                }

                // 添加连边
                for (int i = cluster.ID; i < clusters.Count(); i++)
                {
                    if (linkNumber[i] != 0)
                    {
                        GeoNetworkNode startNode = networkL3.GetGeoNetworkNodeByID(cluster.ID);
                        GeoNetworkNode endNode = networkL3.GetGeoNetworkNodeByID(clusters.ElementAt(i).ID);
                        GeoNetworkArc newArc = new GeoNetworkArc(startNode, endNode);
                        newArc.SetProperty("Power", linkNumber[i]);
                        networkL3.AddArc(newArc);
                    }
                }

                // 添加社团均质性
                if (differentLink + sameLink == 0)
                {
                    networkL3.GetGeoNetworkNodeByID(cluster.ID).SetProperty("Homogeneity", -1);
                }
                else
                {
                    int gatherDegree = Convert.ToInt32(networkL3.GetGeoNetworkNodeByID(cluster.ID).GetProperty("Gather"));
                    int scatterDegree = Convert.ToInt32(networkL3.GetGeoNetworkNodeByID(cluster.ID).GetProperty("Scatter"));
                    double homogeneity = (1 - Math.Pow(Math.E, -Math.Abs(gatherDegree - scatterDegree)) / 2.0) * (sameLink) / (differentLink + sameLink);
                    networkL3.GetGeoNetworkNodeByID(cluster.ID).SetProperty("Homogeneity", homogeneity);
                }
            }

            return networkL3;
        }
    }
}
