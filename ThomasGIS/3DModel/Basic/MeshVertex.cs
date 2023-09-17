using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS._3DModel.Basic
{
    public class MeshVertex : Point3D
    {
        public Color4 Color;
        public double U;
        public double V;

        public MeshVertex(double x, double y, double z) : base(x, y, z)
        {
            this.U = 0;
            this.V = 0;
            this.Color = null;
        }

        public MeshVertex(double x, double y, double z, double u, double v) : base(x, y, z)
        {
            this.U = u;
            this.V = v;
            this.Color = null;
        }

        public MeshVertex(double x, double y, double z, double u, double v, Color4 color) : base(x, y, z)
        {
            this.U = u;
            this.V = v;
            this.Color = color;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(this.X);
            sb.Append("-");
            sb.Append(this.Y);
            sb.Append("-");
            sb.Append(this.Z);
            sb.Append("-");
            sb.Append(this.U);
            sb.Append("-");
            sb.Append(this.V);

            if (this.Color != null)
            {
                sb.Append("-");
                sb.Append(this.Color.Red);
                sb.Append("-");
                sb.Append(this.Color.Green);
                sb.Append("-");
                sb.Append(this.Color.Blue);
                sb.Append("-");
                sb.Append(this.Color.Alpha);
            }

            return sb.ToString();
        }
    }
}
