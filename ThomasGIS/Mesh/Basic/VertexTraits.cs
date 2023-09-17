using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Basic
{
    public class VertexTraits
    {
        public Vector3D Position;
        public Vector3D Normal;
        public double MaxCurvature;
        public double MinCurvature;
        public double GaussianCurvature;
        public int FixedIndex = -1;
        public int SelectedFlag = 0;

        public VertexTraits(double x, double y, double z)
        {
            Position = new Vector3D(x, y, z);
        }

        public VertexTraits(Vector3D v3d)
        {
            Position = new Vector3D(v3d.X, v3d.Y, v3d.Z);
        }
    }
}
