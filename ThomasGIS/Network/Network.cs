using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;

namespace ThomasGIS.Network
{
    public enum NetworkType
    {
        Directed,
        Undirected,
        Unknown
    }

    public class LinkValuePair
    {
        public readonly int EndNodeIndex;
        public readonly double Value;
        public readonly int ArcIndex;

        public LinkValuePair(int nodeIndex, double value, int arcIndex)
        {
            this.EndNodeIndex = nodeIndex;
            this.Value = value;
            this.ArcIndex = arcIndex;
        }
    }

    public class DijkstraResult
    {
        public List<int>[] routeNodeList = null;
        public List<int>[] routeArcList = null;
        public double[] distance = null;
    }

    public abstract class Network : INetwork
    {
        public List<INetworkNode> NodeList { get; } = null;

        public List<INetworkArc> ArcList { get; } = null;

        public int NodeNumber => NodeList.Count;

        public int ArcNumber => ArcList.Count;

        // 用于计算最短路径的稀疏邻接矩阵
        public List<List<LinkValuePair>> neighborMatrix = null;
        public bool networkChangeFlag = true;
        public string nowMatrixField = "";

        // 依据当前的节点与边的某项属性生成邻接矩阵
        public virtual void RefreshNeighborMatrix(string dataField = null)
        {
            if (dataField == null || dataField == "")
            {
                dataField = nowMatrixField;
            }

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
                INetworkArc oneArc = this.ArcList[i];

                // 找到每个Arc对应的NodeID对应的NodeIndex
                IEnumerable<INetworkNode> startNode = this.NodeList.Where(oneNode => oneNode.GetID() == oneArc.GetStartNodeID());
                IEnumerable<INetworkNode> endNode = this.NodeList.Where(oneNode => oneNode.GetID() == oneArc.GetEndNodeID());

                // 如果在节点列表中找不到则直接跳过
                if (startNode.Count() == 0 || endNode.Count() == 0) continue;

                int startNodeIndex = NodeList.IndexOf(startNode.First());
                int endNodeIndex = NodeList.IndexOf(endNode.First());

                // 如果边的属性表中不包含该字段，则直接跳过
                if (!oneArc.GetKeys().Contains(dataField)) continue;

                if (dataField != null)
                {
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, Convert.ToDouble(oneArc.GetProperty(dataField).ToString()), i));
                    this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(startNodeIndex, Convert.ToDouble(oneArc.GetProperty(dataField).ToString()), i));
                }
                else
                {
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, 1, i));
                    this.neighborMatrix[endNodeIndex].Add(new LinkValuePair(startNodeIndex, 1, i));
                }
            }

            this.nowMatrixField = dataField;
            this.networkChangeFlag = false;
        }

        public Network()
        {
            this.NodeList = new List<INetworkNode>();
            this.ArcList = new List<INetworkArc>();
        }

        public Network(IEnumerable<INetworkNode> nodeList, IEnumerable<INetworkArc> arcList)
        {
            this.NodeList = new List<INetworkNode>();
            this.NodeList.AddRange(nodeList);
            this.ArcList = new List<INetworkArc>();
            this.ArcList.AddRange(arcList);
        }

        public virtual bool AddArc(INetworkArc newArc)
        {
            this.ArcList.Add(newArc);
            networkChangeFlag = true;
            return true;
        }

        public virtual bool AddArcs(IEnumerable<INetworkArc> arcList)
        {
            this.ArcList.AddRange(arcList);
            networkChangeFlag = true;
            return true;
        }

        public virtual bool AddNode(INetworkNode newNode)
        {
            this.NodeList.Add(newNode);
            networkChangeFlag = true;
            return true;
        }

        public virtual bool AddNodes(IEnumerable<INetworkNode> nodeList)
        {
            this.NodeList.AddRange(nodeList);
            networkChangeFlag = true;
            return true;
        }

        public bool DeleteArcByIndex(int index)
        {
            if (index < 0 || index >= ArcNumber) throw new Exception("索引值超出界限！");
            this.ArcList.RemoveAt(index);
            networkChangeFlag = true;
            return true;
        }

        public bool DeleteArcByNodeID(int nodeID)
        {
            List<INetworkArc> deleteArcs = this.ArcList.Where(oneArc => (oneArc.GetStartNodeID() == nodeID || oneArc.GetEndNodeID() == nodeID)).ToList();
            foreach (INetworkArc oneArc in deleteArcs)
            {
                this.ArcList.Remove(oneArc);
            }
            networkChangeFlag = true;
            return true;
        }

        // 按Node的Index删除，顺便删除与其相连的弧段
        public bool DeleteNodeByIndex(int index)
        {
            if (index < 0 || index >= NodeNumber) throw new Exception("索引值超出界限！");

            INetworkNode nowNode = NodeList[index];
            int nodeID = nowNode.GetID();
            // 先删除与该节点相连的全部弧段
            DeleteArcByNodeID(nodeID);
            // 再删除该节点
            this.NodeList.RemoveAt(index);
            networkChangeFlag = true;
            return true;
        }

        // 按Node的ID删除，顺便删除与其相连的弧段
        public bool DeleteNodeByID(int nodeID)
        {
            DeleteArcByNodeID(nodeID);
            List<INetworkNode> deleteNodes = this.NodeList.Where(oneNode => oneNode.GetID() == nodeID).ToList();
            foreach (INetworkNode oneNode in deleteNodes)
            {
                this.NodeList.Remove(oneNode);
            }
            networkChangeFlag = true;
            return true;
        }

        // 节点的度
        // 输入参数：起始节点、终止节点、阻抗字段
        // 返回参数：整形，节点的度
        public virtual int Degree(int nodeIndex)
        {
            int nodeID = this.NodeList[nodeIndex].GetID();
            IEnumerable<INetworkArc> linkArcs = this.ArcList.Where(oneArc => (oneArc.GetStartNodeID() == nodeID || oneArc.GetEndNodeID() == nodeID));
            return linkArcs.Count();
        }

        // 获取网络的类型
        public virtual NetworkType GetNetworkType()
        {
            return NetworkType.Unknown;
        }

        // 解析两点间的路径
        // 输入参数：起始节点、终止节点、阻抗字段
        // 返回参数：Route对象，包含最短距离，构成最短路径的节点序列，构成最短路径的弧段序列
        public virtual Route SolvePath(int startNodeIndex, int endNodeIndex, string powerField = null)
        {
            // 基准的方法使用迪杰斯特拉，GeoNetwork里节点具备地理坐标后再重写为A*算法
            if (neighborMatrix == null || networkChangeFlag == true || nowMatrixField != powerField)
            {
                RefreshNeighborMatrix(powerField);
            }

            DijkstraResult dijkResult = Dijkstra(startNodeIndex);

            List<INetworkNode> nodeList = new List<INetworkNode>();
            List<int> arcIndexList = new List<int>();

            foreach (int nodeIndex in dijkResult.routeNodeList[endNodeIndex])
            {
                nodeList.Add(NodeList[nodeIndex]);
            }

            foreach (int arcIndex in dijkResult.routeArcList[endNodeIndex])
            {
                arcIndexList.Add(arcIndex);
            }

            return new Route(nodeList, arcIndexList, dijkResult.distance[endNodeIndex], true);
        }

        // 节点的集聚系数
        // 输入参数：节点的Index编号
        // 返回参数：返回双精度浮点型集聚系数
        public virtual double ClusterCoefficient(int nodeIndex)
        {
            if (neighborMatrix == null || networkChangeFlag == true)
            {
                RefreshNeighborMatrix();
            }

            List<LinkValuePair> neighborLinks = this.neighborMatrix[nodeIndex];

            HashSet<int> neighborNodeIDSet = new HashSet<int>();

            foreach (LinkValuePair pair in neighborLinks)
            {
                neighborNodeIDSet.Add(pair.EndNodeIndex);
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

        // 广度遍历网络
        protected List<NodeCluster> BFS()
        {
            List<NodeCluster> clusters = new List<NodeCluster>();
            // 标识数组
            bool[] flagList = new bool[NodeNumber];
            for (int i = 0; i < NodeNumber; i++)
            {
                flagList[i] = false;
            }

            // 使用队列执行广度遍历
            for (int i = 0; i < NodeNumber; i++)
            {
                if (flagList[i] == true) continue;

                Queue<int> DFSStack = new Queue<int>();
                DFSStack.Enqueue(i);
                flagList[i] = true;
                NodeCluster newCluster = new NodeCluster(clusters.Count);

                while (DFSStack.Count > 0)
                {
                    int nowNode = DFSStack.Dequeue();
                    newCluster.nodeIDList.Add(nowNode);

                    List<LinkValuePair> linkPairs = neighborMatrix[nowNode];
                    foreach (LinkValuePair onePair in linkPairs)
                    {
                        int nextIndex = onePair.EndNodeIndex;
                        if (flagList[nextIndex] == true) continue;
                        DFSStack.Enqueue(nextIndex);
                        flagList[nextIndex] = true;
                    }
                }

                clusters.Add(newCluster);
            }

            return clusters;
        }

        // 深度遍历网络
        protected List<NodeCluster> DFS()
        {
            List<NodeCluster> clusters = new List<NodeCluster>();
            // 标识数组
            bool[] flagList = new bool[NodeNumber];
            for (int i = 0; i < NodeNumber; i++)
            {
                flagList[i] = false;
            }

            // 使用栈执行深度遍历
            for (int i = 0; i < NodeNumber; i++)
            {
                if (flagList[i] == true) continue;

                Stack<int> DFSStack = new Stack<int>();
                DFSStack.Push(i);
                flagList[i] = true;
                NodeCluster newCluster = new NodeCluster(clusters.Count);

                while (DFSStack.Count > 0)
                {
                    int nowNode = DFSStack.Pop();
                    newCluster.nodeIDList.Add(nowNode);
                    
                    List<LinkValuePair> linkPairs = neighborMatrix[nowNode];
                    foreach (LinkValuePair onePair in linkPairs)
                    {
                        int nextIndex = onePair.EndNodeIndex;
                        if (flagList[nextIndex] == true) continue;
                        DFSStack.Push(nextIndex);
                        flagList[nextIndex] = true;
                    }
                }

                clusters.Add(newCluster);
            }

            return clusters;
        }

        // 以分裂的方式计算社团
        // 输入参数：可容忍的最大社团数量，该方法得到的社团数量不会大于maxClusterNumber且不会小于由DFS/BFS遍历产生的社团数量
        public IEnumerable<NodeCluster> CommunityDetection_GN(int maxClusterNumber)
        {
            // 若以分裂的方式计算社团，则需要首先将所有的节点视为一个整体？
            // 建议先广度遍历图，得到全部对象，已分裂的单独归类，得到初始Community解
            // 刷新一遍邻接矩阵，很重要
            if (networkChangeFlag == true || neighborMatrix == null || nowMatrixField == "")
            {
                RefreshNeighborMatrix();
            }

            // 使用广度优先搜索遍历，得到初始社团状态
            List<NodeCluster> originClusters = BFS();
            double networkModularity = GlobleModularity(originClusters);

            List<double> modularityList = new List<double>();
            List<List<NodeCluster>> historyClusterResult = new List<List<NodeCluster>>();

            modularityList.Add(networkModularity);
            historyClusterResult.Add(originClusters);

            while (originClusters.Count < maxClusterNumber)
            {
                // 每次需要删除经过次数最高的弧段，申请空间并初始化为0
                List<int> arcDegreeList = new List<int>();
                for (int i = 0; i < ArcNumber; i++)
                {
                    arcDegreeList.Add(0);
                }

                // 计算弧段间的最短路径经过的arcIndex序列
                for (int i = 0; i < NodeNumber; i++)
                {
                    DijkstraResult oneNodeRoutes = Dijkstra(i);
                    for (int j = 0; j < oneNodeRoutes.distance.Length; j++)
                    {
                        if (oneNodeRoutes.distance[j] == Double.MaxValue) continue;

                        foreach (int arcIndex in oneNodeRoutes.routeArcList[j])
                        {
                            arcDegreeList[arcIndex] += 1;
                        }
                    }
                }

                if (originClusters.Count == this.NodeNumber) break;

                // 找到最大的Index弧段
                int maxPowerArcIndex = arcDegreeList.IndexOf(arcDegreeList.Max());

                // 在邻接矩阵中删除该弧段
                INetworkArc deleteArc = ArcList[maxPowerArcIndex];
                LinkValuePair deleteLink = neighborMatrix[deleteArc.GetStartNodeID()].Where(item => item.EndNodeIndex == deleteArc.GetEndNodeID()).ToList()[0];
                this.neighborMatrix[deleteArc.GetStartNodeID()].Remove(deleteLink);

                deleteLink = neighborMatrix[deleteArc.GetEndNodeID()].Where(item => item.EndNodeIndex == deleteArc.GetStartNodeID()).ToList()[0];
                this.neighborMatrix[deleteArc.GetEndNodeID()].Remove(deleteLink);

                // 重新遍历得到社团对象，计算模块度，加入历史对象
                originClusters = BFS();
                networkModularity = GlobleModularity(originClusters);
                modularityList.Add(networkModularity);
                historyClusterResult.Add(originClusters);
            }

            int maxModularityIndex = modularityList.IndexOf(modularityList.Max());

            // 恢复被删除过的邻接矩阵
            RefreshNeighborMatrix();
            return historyClusterResult[maxModularityIndex];
        }

        // 计算复杂网络的全局模块度
        // 输入参数：社团划分的结果序列，多个NodeCluster，每个NodeCluster中包含属于该社区的节点ID
        public double GlobleModularity(List<NodeCluster> communities)
        {
            // 刷新一遍邻接矩阵，很重要
            if (networkChangeFlag == true || neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            // 计算m值
            double m = 0;

            // 依次处理每个社团中包含的节点
            foreach (NodeCluster oneCluster in communities)
            {
                foreach (int nodeIndex in oneCluster.nodeIDList)
                {
                    // 在邻接矩阵中取出与其相连的所有弧段（无向图）
                    List<LinkValuePair> linkPairs = this.neighborMatrix[nodeIndex];
                    foreach (LinkValuePair onePair in linkPairs)
                    {
                        m += onePair.Value;
                    }
                }
            }

            m /= 2;

            // 计算Q值
            double Q = 0;

            // 依次处理每个社团中包含的节点
            foreach (NodeCluster oneCluster in communities)
            {
                foreach (int nodeIndex in oneCluster.nodeIDList)
                {
                    // 在邻接矩阵中取出与其相连的所有弧段（无向图）
                    List<LinkValuePair> linkPairs = this.neighborMatrix[nodeIndex];

                    double k_i = 0;
                    double k_j = 0;

                    // k_i是与节点i相连的所有弧段的权重和
                    foreach (LinkValuePair onePair in linkPairs)
                    {
                        k_i += onePair.Value;
                    }

                    // A_ij是i与j间弧段的权重，
                    foreach (LinkValuePair onePair in linkPairs)
                    {
                        // 不在一个社团里可以跳过
                        int neighborIndex = onePair.EndNodeIndex;
                        if (!oneCluster.nodeIDList.Contains(neighborIndex)) continue;

                        double A_ij = onePair.Value;

                        List<LinkValuePair> neighborLinkPairs = this.neighborMatrix[neighborIndex];

                        // k_j是与节点j相连的所有弧段的权重和
                        foreach (LinkValuePair neighborPair in neighborLinkPairs)
                        {
                            k_j += neighborPair.Value;
                        }

                        Q += (A_ij - k_i * k_j / (2 * m));
                    }
                }
            }

            return Q /= (2 * m);
        }

        public DijkstraResult Dijkstra(int index)
        {
            // 初始全部给最大值

            bool[] flag = new bool[NodeNumber];

            DijkstraResult result = new DijkstraResult();
            // 路径
            result.distance = new double[NodeNumber];
            result.routeNodeList = new List<int>[NodeNumber];
            result.routeArcList = new List<int>[NodeNumber];

            for (int i = 0; i < NodeNumber; i++)
            {
                result.distance[i] = Double.MaxValue;
                flag[i] = false;
                result.routeNodeList[i] = new List<int>();
                result.routeArcList[i] = new List<int>();
            }

            // 首先以自己为起点出发
            List<LinkValuePair> targetNodeLinks = neighborMatrix[index];
            flag[index] = true;
            foreach (LinkValuePair onePair in targetNodeLinks)
            {
                result.distance[onePair.EndNodeIndex] = onePair.Value;
                result.routeNodeList[onePair.EndNodeIndex].Add(index);
                result.routeNodeList[onePair.EndNodeIndex].Add(onePair.EndNodeIndex);
                result.routeArcList[onePair.EndNodeIndex].Add(onePair.ArcIndex);
            }

            // 处理N-1次
            for (int i = 0; i < NodeNumber - 1; i++)
            {
                // 找到Distance中未处理过的距离最小值
                double minDistance = Double.MaxValue;
                int nextNodeID = -1;
                for (int j = 0; j < NodeNumber; j++)
                {
                    if (flag[j] == true || result.distance[j] == Double.MaxValue) continue;
                    if (minDistance > result.distance[j])
                    {
                        minDistance = result.distance[j];
                        nextNodeID = j;
                    }
                }

                if (nextNodeID == -1) break;

                List<LinkValuePair> tempNodeLinks = neighborMatrix[nextNodeID];
                flag[nextNodeID] = true;
               
                foreach (LinkValuePair onePair in tempNodeLinks)
                {
                    double newDistance = onePair.Value + result.distance[nextNodeID];
                    // 如果新的路径比旧的路径更短，则替换
                    if (newDistance < result.distance[onePair.EndNodeIndex] && flag[onePair.EndNodeIndex] == false)
                    {
                        result.distance[onePair.EndNodeIndex] = newDistance;
                        result.routeNodeList[onePair.EndNodeIndex].Clear();
                        result.routeNodeList[onePair.EndNodeIndex].AddRange(result.routeNodeList[nextNodeID]);
                        result.routeNodeList[onePair.EndNodeIndex].Add(onePair.EndNodeIndex);

                        result.routeArcList[onePair.EndNodeIndex].Clear();
                        result.routeArcList[onePair.EndNodeIndex].AddRange(result.routeArcList[nextNodeID]);
                        result.routeArcList[onePair.EndNodeIndex].Add(onePair.ArcIndex);
                    }
                }
            }

            return result;
        }

        public INetworkNode GetNodeByIndex(int nodeIndex)
        {
            if (nodeIndex < -this.NodeNumber || nodeIndex >= this.NodeNumber) throw new IndexOutOfRangeException();
            if (nodeIndex < 0) nodeIndex += this.NodeNumber;
            return this.NodeList[nodeIndex];
        }

        public INetworkNode GetNodeByID(int ID)
        {
            IEnumerable<INetworkNode> nodeList = this.NodeList.Where(item => item.GetID() == ID);
            if (nodeList.Count() == 0) return null;
            return nodeList.First();
        }

        public INetworkArc GetArcByIndex(int arcIndex)
        {
            if (arcIndex < -this.ArcNumber || arcIndex >= this.ArcNumber) throw new IndexOutOfRangeException();
            if (arcIndex < 0) arcIndex += this.ArcNumber;
            return this.ArcList[arcIndex];
        }

        public int DegreeCentrality(int nodeIndex)
        {
            if (this.neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            return Degree(nodeIndex);
        }

        public bool DegreeCentrality()
        {
            if (this.NodeNumber == 0) return false;

            Parallel.For(0, this.NodeList.Count, i =>
            {
                INetworkNode node = this.GetNodeByIndex(i);
                node.SetProperty("DegreeCen", DegreeCentrality(i));
            });

            return true;
        }

        public double ClosestCentrality(int nodeIndex)
        {
            if (this.neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            DijkstraResult routeResult =  this.Dijkstra(nodeIndex);

            double sumDistance = 0;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                if (i == nodeIndex) continue;

                if (routeResult.distance[i] != Double.MaxValue)
                {
                    sumDistance += 1.0 / routeResult.distance[i];
                }
            }

            if (sumDistance == 0) return -1;

            return sumDistance;
        }

        public bool KatzCentrality(double alpha)
        {
            if (this.NodeNumber == 0) return false;

            Parallel.For(0, this.NodeList.Count, i =>
            {
                INetworkNode node = this.GetNodeByIndex(i);
                node.SetProperty("KatzCen", KatzCentrality(i, alpha));
            });
            return true;
        }

        // Katz中心性
        public double KatzCentrality(int nodeIndex, double alpha)
        {
            if (this.neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            DijkstraResult routeResult = this.Dijkstra(nodeIndex);

            double katzResult = 0;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                if (i == nodeIndex) continue;

                if (routeResult.distance[i] != Double.MaxValue)
                {
                    katzResult += Math.Pow(alpha, routeResult.routeArcList[i].Count);
                }
            }

            return katzResult;
        }

        // 集聚系数
        public bool ClusterCoefficient()
        {
            if (this.NodeNumber == 0) return false;

            Parallel.For(0, this.NodeList.Count, i =>
            {
                INetworkNode node = this.GetNodeByIndex(i);
                node.SetProperty("ClusterC", ClusterCoefficient(i));
            });
            return true;
        }

        // 接近中心性，得到网络中每个节点到其余可到达节点最短距离倒数之和
        public bool ClosestCentrality()
        {
            if (this.NodeNumber == 0) return false;

            Parallel.For(0, this.NodeList.Count, i =>
            {
                INetworkNode node = this.GetNodeByIndex(i);
                node.SetProperty("ClosestCen", ClosestCentrality(i));
            });
            return true;
        }

        // 中介中心性，得到网络中所有节点被最短路径经过的次数
        public bool BetweenCentrality()
        {
            if (this.NodeNumber == 0) return false;

            int[] betweenCentralityList = new int[this.NodeNumber];
            for (int i = 0; i < this.NodeNumber; i++)
            {
                betweenCentralityList[i] = 0;
            }

            Parallel.For(0, this.NodeList.Count, i =>
            {
                DijkstraResult result = this.Dijkstra(i);

                lock (this)
                {
                    for (int j = 0; j < this.NodeNumber; j++)
                    {
                        if (i == j) continue;

                        if (result.distance[j] != Double.MaxValue)
                        {
                            for (int k = 1; k < result.routeNodeList[j].Count - 1; k++)
                            {
                                betweenCentralityList[result.routeNodeList[j][k]] += 1;
                            }
                        }
                    }
                }
            });

            for (int i = 0; i < this.NodeNumber; i++)
            {
                this.GetNodeByIndex(i).SetProperty("BetweenCen", betweenCentralityList[i]);
            }

            return true;
        }

        // 网络的随机游走特征，得到网络中所有节点被随机路径经过的次数
        public bool RandomWalk(int level)
        {
            if (this.neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            if (this.NodeNumber == 0) return false;

            int[] nodeCrossTimes = new int[this.NodeNumber];

            for (int i = 0; i < this.NodeNumber; i++)
            {
                nodeCrossTimes[i] = 0;
            }

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int nowLevel = 0;
                int nowIndex = i;
                Random rd = new Random();
                while (nowLevel < level)
                {
                    List<LinkValuePair> links = this.neighborMatrix[nowIndex];
                    if (links.Count == 0) break;
                    int targetArcIndex = rd.Next(0, links.Count - 1);
                    LinkValuePair targetArc = links[targetArcIndex];
                    nowIndex = targetArc.EndNodeIndex;
                    nowLevel += 1;
                    nodeCrossTimes[nowIndex] += 1;
                }
            }

            for (int i = 0; i < this.NodeNumber; i++)
            {
                this.NodeList[i].SetProperty($"RdWalk_{level}", nodeCrossTimes[i]);
            }

            return true;
        }

        public double RandomWalkLength(int level)
        {
            if (this.neighborMatrix == null)
            {
                RefreshNeighborMatrix();
            }

            if (this.NodeNumber == 0 || this.ArcNumber == 0) return -1;

            Decimal sumLength = Decimal.Zero;

            for (int i = 0; i < this.NodeNumber; i++)
            {
                int nowLevel = 0;
                int nowIndex = i;
                Random rd = new Random();
                while (nowLevel < level)
                {
                    List<LinkValuePair> links = this.neighborMatrix[nowIndex];
                    if (links.Count == 0) break;
                    int targetArcIndex = rd.Next(0, links.Count - 1);
                    LinkValuePair targetArc = links[targetArcIndex];
                    nowIndex = targetArc.EndNodeIndex;
                    nowLevel += 1;
                    sumLength = Decimal.Add(sumLength, Decimal.Parse(targetArc.Value.ToString()));
                }
            }

            return Decimal.ToDouble(sumLength) / this.NodeNumber;
        }

        public int GetArcNumber()
        {
            return this.ArcNumber;
        }

        public int GetNodeNumber()
        {
            return this.NodeNumber;
        }

        protected virtual void ClusterLinks_Converse(NodeCluster nodeCluster, out double inWeights, out double outWeights)
        {
            inWeights = 0;
            outWeights = 0;

            for (int i = 0; i < nodeCluster.nodeIDList.Count; i++)
            {
                int innerNodeIndex = nodeCluster.nodeIDList[i];
                List<LinkValuePair> links = this.neighborMatrix[innerNodeIndex];
                foreach (LinkValuePair oneLink in links)
                {
                    int targetNodeIndex = oneLink.EndNodeIndex;
                    if (nodeCluster.nodeIDList.Contains(targetNodeIndex))
                    {
                        inWeights += 1.0 / oneLink.Value;
                    }
                    else
                    {
                        outWeights += 1.0 / oneLink.Value;
                    }
                }
            }

            // 无向图内部节点会加两遍，所以要除以二
            inWeights /= 2.0;
        }

        protected virtual void ClusterLinks(NodeCluster nodeCluster, out double inWeights, out double outWeights)
        {
            inWeights = 0;
            outWeights = 0;

            for (int i = 0; i < nodeCluster.nodeIDList.Count; i++)
            {
                int innerNodeIndex = nodeCluster.nodeIDList[i];
                List<LinkValuePair> links = this.neighborMatrix[innerNodeIndex];
                foreach (LinkValuePair oneLink in links)
                {
                    int targetNodeIndex = oneLink.EndNodeIndex;
                    if (nodeCluster.nodeIDList.Contains(targetNodeIndex))
                    {
                        inWeights += oneLink.Value;
                    }
                    else
                    {
                        outWeights += oneLink.Value;
                    }
                }
            }

            // 无向图内部节点会加两遍，所以要除以二
            inWeights /= 2.0;
        }

        protected virtual double WeightsBetweenClusters(NodeCluster c1, NodeCluster c2)
        {
            double linkWeight = 0;
            for (int i = 0; i < c1.nodeIDList.Count; i++)
            {
                int innerNodeIndex = c1.nodeIDList[i];
                List<LinkValuePair> links = this.neighborMatrix[innerNodeIndex];
                foreach (LinkValuePair oneLink in links)
                {
                    int targetNodeIndex = oneLink.EndNodeIndex;
                    int targetClusterID = (int)this.NodeList[targetNodeIndex].GetProperty("Cluster");

                    if (targetClusterID == c2.ID)
                    {
                        linkWeight += 1.0 / oneLink.Value;
                    }
                }
            }

            return linkWeight;
        }

        public IEnumerable<NodeCluster> CommunityDetection_Louvain(int maxLevel)
        {
            if (this.neighborMatrix == null || this.networkChangeFlag)
            {
                this.RefreshNeighborMatrix();
            }

            double sumWeights = 0;
            for (int i = 0; i < this.NodeNumber; i++)
            {
                List<LinkValuePair> links = this.neighborMatrix[i];
                for (int j = 0; j < links.Count; j++)
                {
                    sumWeights += 1.0 / links[j].Value;
                }
            }

            // 初始化所有的网络节点为独立的社团
            List<NodeCluster> baseClusterList = new List<NodeCluster>();
            for (int i = 0; i < this.NodeNumber; i++)
            {
                baseClusterList.Add(new NodeCluster(i));
                baseClusterList[i].nodeIDList.Add(i);
                // 一开始社团编号就是自己
                this.NodeList[i].SetProperty("Cluster", i);
            }

            List<double> globalSolution = new List<double>();

            // 最多做几层聚类，每聚类一次需要评估一次全局模块度并记录
            for (int nowLevelNumber = 0; nowLevelNumber < maxLevel; nowLevelNumber++)
            {
                // 用于记录社团是否被访问过的数组
                bool[] visitedFlagList = new bool[baseClusterList.Count];

                // 循环处理每个社团，被归并过的社团在下一轮进行处理
                for (int i = 0; i < baseClusterList.Count; i++)
                {
                    if (visitedFlagList[i]) continue;

                    // 如果当前社团已经空了说明已经被合并了
                    NodeCluster nowCluster = baseClusterList[i];
                    if (nowCluster.nodeIDList.Count == 0) continue;

                    // 每个社团的 ki 是相对固定的可以先算
                    // ki 是连接到社团 i 的权重和
                    ClusterLinks_Converse(nowCluster, out var nowClusterInnerWeights, out var ki);

                    List<double> deltaQList = new List<double>();
                    for (int j = 0; j < baseClusterList.Count; j++)
                    {
                        // 如果这个Cluster已经空了，那就不可能了，说明已经被删掉了
                        // Cluster不和自己计算
                        NodeCluster targetCluster = baseClusterList[j];
                        if (i == j || targetCluster.nodeIDList.Count == 0)
                        {
                            deltaQList.Add(Double.MinValue);
                        }
                        // 开始计算
                        else
                        {
                            // 计算 Σin 和 Σtot
                            ClusterLinks_Converse(targetCluster, out var sigmaIn, out var sigmaTot);
                            // 计算 ki,in
                            double kiin = WeightsBetweenClusters(nowCluster, targetCluster);

                            double deltaQ_1 = ((sigmaIn + kiin) / (2 * sumWeights) - Math.Pow((sigmaTot + ki) / (2 * sumWeights), 2));
                            double deltaQ_2 = (sigmaIn / (2 * sumWeights)) - Math.Pow(sigmaTot / (2 * sumWeights), 2) - Math.Pow(ki / (2 * sumWeights), 2);
                            double deltaQ = deltaQ_1 - deltaQ_2;
                            deltaQList.Add(deltaQ);
                        }
                    }

                    // 找到增益最大的 Q， 若 Q 大于 0 则合并两个社团
                    double maxQ = deltaQList.Max();
                    if (maxQ > 0)
                    {
                        int mergeIndex = deltaQList.IndexOf(maxQ);
                        Console.WriteLine(i.ToString() + "-" + mergeIndex.ToString());
                        // 将当前社团的内容写入目标社团，并修改节点中Cluster的ID
                        for (int j = 0; j < nowCluster.nodeIDList.Count; j++)
                        {
                            baseClusterList[mergeIndex].nodeIDList.Add(nowCluster.nodeIDList[j]);
                            this.NodeList[nowCluster.nodeIDList[j]].SetProperty("Cluster", baseClusterList[mergeIndex].ID);
                        }
                        // 清除当前社团内的全部节点，使之成为一个空社团
                        nowCluster.nodeIDList.Clear();
                        // 被合并的对象在本轮中不进行主动归并
                        visitedFlagList[mergeIndex] = true;
                    }
                }

                double globleModularity = this.GlobleModularity(baseClusterList);
                globalSolution.Add(globleModularity);
            }

            int index = 0;
            while (index < baseClusterList.Count)
            {
                if (baseClusterList[index].nodeIDList.Count == 0)
                {
                    baseClusterList.RemoveAt(index);
                }
                else
                {
                    index += 1;
                }
            }

            for (int i = 0; i < baseClusterList.Count; i++)
            {
                baseClusterList[i].ID = i;
                for (int j = 0; j < baseClusterList[i].nodeIDList.Count; j++)
                {
                    this.NodeList[baseClusterList[i].nodeIDList[j]].SetProperty("Cluster", i);
                }
            }

            return baseClusterList;
        }
    }
}
