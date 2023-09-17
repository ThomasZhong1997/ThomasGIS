using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public class ShpPoint : Point, IShpPoint
    {
        public ShpPoint(double x, double y) : base(x, y)
        {
            
        }

        public ShpPoint(string wkt) : base(wkt)
        {
            
        }

        public ShpPoint(IPoint point) : base(point)
        {
            
        }

        public virtual int ContentLength()
        {
            return 2 + 4 + 4;
        }

        public override string GetGeometryType()
        {
            return "ShpPoint";
        }

        public virtual ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.Point;
        }

        public virtual IShpGeometryBase Clone()
        {
            return new ShpPoint(this);
        }
    }
}
