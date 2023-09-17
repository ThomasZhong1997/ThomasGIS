using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Geometry3D
{
    public class Sphere
    {
        public Vector3D CenterLocation;
        public double Radius;

        public Sphere(Vector3D center, double radius)
        {
            this.CenterLocation = center;
            this.Radius = radius;
        }
    }
}
