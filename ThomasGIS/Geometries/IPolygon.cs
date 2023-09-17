 using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;

namespace ThomasGIS.Geometries
{
    public interface IPolygon : IGeometry
    {
        IEnumerable<IPoint> GetPartByIndex(int index);

        double GetArea(CoordinateType coordianteType = CoordinateType.Projected);

        IEnumerable<IEnumerable<IPoint>> GetPointList();
    }
}
