using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Geometries
{
    public interface IPoint3D : IPoint
    {
        double GetZ();

        void SetZ(double z);
    }
}
