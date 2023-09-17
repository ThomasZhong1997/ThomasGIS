using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public class ShpPoint3D : ShpPoint
    {
        public double Z;
        public double M;

        public ShpPoint3D(double x, double y, double z, double m) : base(x, y)
        {
            this.Z = z;
            this.M = m;
        }

        public ShpPoint3D(ShpPoint3D clone) : base(clone.X, clone.Y)
        {
            this.Z = clone.Z;
            this.M = clone.M;
        }

        public override ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.PointZ;
        }

        public override string GetGeometryType()
        {
            return "ShpPoint3D";
        }

        public override int ContentLength()
        {
            return 2 + 4 + 4 + 4 + 4;
        }

        public override IShpGeometryBase Clone()
        {
            return new ShpPoint3D(this);
        }
    }
}
