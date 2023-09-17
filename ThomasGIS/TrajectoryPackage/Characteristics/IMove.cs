using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public interface IMove
    {
        string GetTaxiID();

        double GetLength();

        double GetStartTime();

        double GetEndTime();

        double GetAverageSpeed();

        IEnumerable<IPoint> GetMovePoints();
    }
}
