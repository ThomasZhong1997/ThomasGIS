using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Network;

namespace ThomasGIS.HumanMobilityNetwork
{
    public class NetworkL3 : GeoNetwork
    {
        public Dictionary<int, NodeCluster> nextLevelInfo = null;

        public NetworkL3(CoordinateBase coordinateSystem) : base(coordinateSystem)
        {
            nextLevelInfo = new Dictionary<int, NodeCluster>();
        }

        public override bool AddNode(INetworkNode newNode)
        {
            throw new Exception("Network L3不支持直接添加节点，请使用AddNode的重载方法！");
        }

        public bool AddNode(INetworkNode newNode, NodeCluster cluster)
        {
            this.NodeList.Add(newNode);
            this.nextLevelInfo.Add(newNode.GetID(), cluster);
            return true;
        }
    }
}