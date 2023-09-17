using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    // 网络中的最小三角形
    public class NetworkTriangleStruct
    {
        public int nodeIndex1;
        public int nodeIndex2;
        public int nodeIndex3;

        public NetworkTriangleStruct(int nodeIndex1, int nodeIndex2, int nodeIndex3)
        {
            this.nodeIndex1 = nodeIndex1;
            this.nodeIndex2 = nodeIndex2;
            this.nodeIndex3 = nodeIndex3;
        }
    }
}
