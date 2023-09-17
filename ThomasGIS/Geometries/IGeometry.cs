using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Geometries
{
    public interface IGeometry
    {
        string ExportToWkt();

        string GetGeometryType();

        string GetBaseGeometryType();

        BoundaryBox GetBoundaryBox();
    }
}
