using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Network;

namespace ThomasGIS.TrajectoryPackage.MapMatch
{
    public interface IMapMatch
    {
        MapMatchResult MatchRoute(ITrajectory trajectory, double range, int K);

        GeoNetworkArc GetGeoNetworkArc(int index);
    }
}
