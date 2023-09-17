using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage.Order
{
    public interface ITrajectoryOrder
    {
        string ExportString(char separator);

        string ExportTitle(char separator);

        CoordinateBase GetCoordinateSystem();

        IPoint GetStartPoint();

        IPoint GetEndPoint();

        bool SetCoordinateSystem(CoordinateBase coordinateSystem);

        BoundaryBox GetBoundaryBox();
    }
}
