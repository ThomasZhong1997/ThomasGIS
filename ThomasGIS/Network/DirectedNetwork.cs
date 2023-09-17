using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThomasGIS.Network
{
    public class DirectedNetwork : Network, IDirectedNetwork
    {
        public List<bool> isDelete = new List<bool>();

        public int InDegree(int nodeIndex)
        {
            int nodeID = this.NodeList[nodeIndex].GetID();
            IEnumerable<INetworkArc> linkArcs = this.ArcList.Where(oneArc => oneArc.GetEndNodeID() == nodeID);
            return linkArcs.Count();
        }

        public int OutDegree(int nodeIndex)
        {
            int nodeID = this.NodeList[nodeIndex].GetID();
            IEnumerable<INetworkArc> linkArcs = this.ArcList.Where(oneArc => oneArc.GetStartNodeID() == nodeID);
            return linkArcs.Count();
        }

        public override void RefreshNeighborMatrix(string dataField = null)
        {
            if (dataField == null || dataField == "")
            {
                dataField = nowMatrixField;
            }

            // 不重新建，

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

                // 有向图加半边
                if (dataField != null)
                {
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, Convert.ToDouble(oneArc.GetProperty(dataField).ToString()), i));
                }
                else
                {
                    this.neighborMatrix[startNodeIndex].Add(new LinkValuePair(endNodeIndex, 1, i));
                }
            }

            this.nowMatrixField = dataField;
            this.networkChangeFlag = false;
        }
    }
}
