using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;

namespace ThomasGIS.Geometries
{
    public interface ILineString : IGeometry
    {
        IPoint GetPointByIndex(int index);

        double GetLength(CoordinateType coordinateType = CoordinateType.Projected);

        IEnumerable<IPoint> GetPointList();
    }
}
