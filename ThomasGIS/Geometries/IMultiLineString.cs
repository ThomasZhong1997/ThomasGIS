using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Geometries
{
    public interface IMultiLineString : IGeometry
    {
        IEnumerable<IPoint> GetPart(int index);

        double[] GetLength();

        IEnumerable<IEnumerable<IPoint>> GetPointList();
    }
}
