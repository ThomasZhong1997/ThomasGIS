using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public class BD09 : ITransform
    {
        public void Toward(IPoint point)
        {
            double PI = 3.141592653589793 * 3000.0 / 180.0;
            double x = point.GetX();
            double y = point.GetY();
            double z = Math.Sqrt(x * x + y * y) + 0.00002 * Math.Sin(y * PI);
            double theta = Math.Atan2(y, x) + 0.000003 * Math.Cos(x * PI);

            point.SetX(z * Math.Cos(theta) + 0.0065);
            point.SetY(z * Math.Sin(theta) + 0.006);
        }

        public void Backward(IPoint point)
        {
            double PI = 3.141592653589793 * 3000.0 / 180.0;
            double x = point.GetX() - 0.0065;
            double y = point.GetY() - 0.006;
            double z = Math.Sqrt(x * x + y * y) - 0.00002 * Math.Sin(y * PI);
            double theta = Math.Atan2(y, x) - 0.000003 * Math.Cos(x * PI);

            point.SetX(z * Math.Cos(theta));
            point.SetY(z * Math.Sin(theta));
        }
    }
}
