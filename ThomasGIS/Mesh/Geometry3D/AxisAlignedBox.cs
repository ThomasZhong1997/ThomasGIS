using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Mesh.Geometry3D
{
    public class AxisAlignedBox
    {
        public double XMin, XMax, YMin, YMax, ZMin, ZMax;

        public AxisAlignedBox(double xmin, double xmax, double ymin, double ymax, double zmin, double zmax)
        {
            XMin = xmin;
            XMax = xmax;
            YMin = ymin;
            YMax = ymax;
            ZMin = zmin;
            ZMax = zmax;
        }
    }
}
