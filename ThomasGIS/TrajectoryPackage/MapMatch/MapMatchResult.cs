using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.TrajectoryPackage.MapMatch
{
    public class MapMatchResult
    {
        public List<GnssPoint> gnssPointList;
        public List<int> segmentList;

        public MapMatchResult()
        {
            gnssPointList = new List<GnssPoint>();
            segmentList = new List<int>();
        }
    }
}
