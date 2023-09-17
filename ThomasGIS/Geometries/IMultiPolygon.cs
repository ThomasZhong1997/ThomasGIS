using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;

namespace ThomasGIS.Geometries
{
    public interface IMultiPolygon : IGeometry
    {
        IEnumerable<IEnumerable<IPoint>> GetPolygon(int index);

        double[] GetArea(CoordinateType coordinateType = CoordinateType.Projected);

        IEnumerable<IEnumerable<IEnumerable<IPoint>>> GetPointList();
    }
}
