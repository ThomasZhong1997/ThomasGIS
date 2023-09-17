using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public class ShpNone : IShpGeometryBase
    {
        public IShpGeometryBase Clone()
        {
            return new ShpNone();
        }

        public int ContentLength()
        {
            return 2;
        }

        public string ExportToWkt()
        {
            return "";
        }

        string IGeometry.GetBaseGeometryType()
        {
            return "None";
        }

        BoundaryBox IGeometry.GetBoundaryBox()
        {
            return new BoundaryBox(0, 0, 0, 0);
        }

        ESRIShapeType IShpGeometryBase.GetFeatureType()
        {
            return ESRIShapeType.None;
        }

        string IGeometry.GetGeometryType()
        {
            return "ShpNone";
        }
    }
}
