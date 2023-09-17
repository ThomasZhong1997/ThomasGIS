using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public class NodeCluster
    {
        public int ID;
        public List<int> nodeIDList;

        public NodeCluster(int ID)
        {
            this.ID = ID;
            this.nodeIDList = new List<int>();
        }
    }
}
