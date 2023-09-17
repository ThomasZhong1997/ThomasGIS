using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Vector;
using static ThomasGIS.Mesh.Basic.TriMesh;

namespace ThomasGIS.Mesh.Geometry3D
{
    public class Plane
    {
        public Vector3D v1, v2, v3;
        public Vector3D Normal = new Vector3D(0, 0, 0);
        public double D;
        public double A => this.Normal.X;
        public double B => this.Normal.Y;
        public double C => this.Normal.Z;

        public Plane(Vector3D v1, Vector3D v2, Vector3D v3)
        {
            this.v1 = v1; 
            this.v2 = v2;
            this.v3 = v3;

            double A = v2.Y * v3.Z - v2.Y * v1.Z - v1.Y * v3.Z - v3.Y * v2.Z + v1.Y * v2.Z + v3.Y * v1.Z;
            double B = v3.X * v2.Z - v1.X * v2.Z - v3.X * v1.Z - v2.X * v3.Z + v2.X * v1.Z + v1.X * v3.Z;
            double C = v2.X * v3.Y - v2.X * v1.Y - v1.X * v3.Y - v3.X * v2.Y + v3.X * v1.Y + v1.X * v2.Y;

            double D1 = Math.Round(-(A * v1.X + B * v1.Y + C * v1.Z), 6);
            double D2 = Math.Round(-(A * v2.X + B * v2.Y + C * v2.Z), 6);
            double D3 = Math.Round(-(A * v3.X + B * v3.Y + C * v3.Z), 6);

            if (D1 != D2 || D2 != D3) throw new Exception("Plane Error");

            double sum = A * A + B * B + C * C;
            this.Normal.X = A / Math.Sqrt(sum);
            this.Normal.Y = B / Math.Sqrt(sum);
            this.Normal.Z = C / Math.Sqrt(sum);
            this.D = D1 / Math.Sqrt(sum);
        }
    }
}
