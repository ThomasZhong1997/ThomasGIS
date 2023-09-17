using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public interface IShpGeometryBase : IGeometry
    {
        int ContentLength();

        ESRIShapeType GetFeatureType();

        IShpGeometryBase Clone();
    }
}
