using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public interface IDirectedNetwork : INetwork
    {
        int InDegree(int nodeIndex);

        int OutDegree(int nodeIndex);
    }
}
