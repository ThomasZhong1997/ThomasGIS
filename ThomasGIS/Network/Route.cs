using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public class Route
    {
        public readonly List<INetworkNode> RouteNodes = new List<INetworkNode>();
        public readonly List<int> RouteArcs = new List<int>();
        public double Impedance;
        public bool isExist;

        public Route(IEnumerable<INetworkNode> nodeList, IEnumerable<int> arcList, double impedance, bool isExist)
        {
            this.RouteNodes.AddRange(nodeList);
            this.RouteArcs.AddRange(arcList);
            this.Impedance = impedance;
            this.isExist = isExist;
        }
    }
}
