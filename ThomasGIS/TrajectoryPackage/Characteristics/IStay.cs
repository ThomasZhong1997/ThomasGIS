using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public interface IStay
    {
        string GetID();

        double GetStartTime();

        double GetEndTime();

        double GetArea();

        IEnumerable<GnssPoint> GetStayPoints();

        IEnumerable<IPoint> GetPointEnumerable();
    }
}
