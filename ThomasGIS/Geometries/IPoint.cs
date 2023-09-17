using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Geometries
{
    public interface IPoint : IGeometry
    {
        // 计算当前点对象的shapefile文件中对应的字节长度
        double GetX();
        double GetY();
        bool SetX(double x);
        bool SetY(double y);
        bool NearlyEqual(IPoint other, double tolerate = 1e-6);
    }
}
