using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public class GCJ02 : ITransform
    {
        private double TransformLat(double x, double y)
        {
            double longitude = x - 105.0;
            double latitude = y - 35.0;
            double ret = -100.0 + 2.0 * longitude + 3.0 * latitude + 0.2 * latitude * latitude + 0.1 * longitude * latitude + 0.2 * Math.Sqrt(Math.Abs(longitude));
            ret += (20.0 * Math.Sin(6.0 * longitude * Math.PI) + 20.0 * Math.Sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(latitude * Math.PI) + 40.0 * Math.Sin(latitude / 3.0 * Math.PI)) * 2.0 / 3.0;
            ret += (160.0 * Math.Sin(latitude / 12.0 * Math.PI) + 320.0 * Math.Sin(latitude * Math.PI / 30.0)) * 2.0 / 3.0;
            return ret;
        }

        private double TransformLon(double x, double y)
        {
            double longitude = x - 105.0;
            double latitude = y - 35.0;
            double ret = 300.0 + longitude + 2.0 * latitude + 0.1 * longitude * longitude + 0.1 * longitude * latitude + 0.1 * Math.Sqrt(Math.Abs(longitude));
            ret += (20.0 * Math.Sin(6.0 * longitude * Math.PI) + 20.0 * Math.Sin(2.0 * longitude * Math.PI)) * 2.0 / 3.0;
            ret += (20.0 * Math.Sin(longitude * Math.PI) + 40.0 * Math.Sin(longitude / 3.0 * Math.PI)) * 2.0 / 3.0;
            ret += (150.0 * Math.Sin(longitude / 12.0 * Math.PI) + 300.0 * Math.Sin(longitude / 30.0 * Math.PI)) * 2.0 / 3.0;
            return ret;
        }

        public void Toward(IPoint point)
        {
            double earth_radius = 6378245.0;
            double earth_eccentricity = 0.00669342162296594323;
            double dLat = TransformLat(point.GetX(), point.GetY());
            double dLon = TransformLon(point.GetX(), point.GetY());
            double radLat = point.GetY() / 180.0 * Math.PI;
            double magic = Math.Sin(radLat);
            magic = 1 - earth_eccentricity * magic * magic;
            double sqrtMagic = Math.Sqrt(magic);
            dLat = (dLat * 180.0) / ((earth_radius * (1 - earth_eccentricity)) / (magic * sqrtMagic) * Math.PI);
            dLon = (dLon * 180.0) / (earth_radius / sqrtMagic * Math.Cos(radLat) * Math.PI);
            point.SetY(point.GetY() + dLat);
            point.SetX(point.GetX() + dLon);
        }

        public void Backward(IPoint point)
        {
            double earthRadius = 6378245.0;
            double earthEccentricity = 0.00669342162296594323;
            double dLat = TransformLat(point.GetX(), point.GetY());
            double dLon = TransformLon(point.GetX(), point.GetY());
            double radLat = point.GetY() / 180.0 * Math.PI;
            double magic = Math.Sin(radLat);
            magic = 1.0 - earthEccentricity * magic * magic;
            double sqrt_magic = Math.Sqrt(magic);
            dLon = (dLon * 180.0) / (earthRadius / sqrt_magic * Math.Cos(radLat) * Math.PI);
            dLat = (dLat * 180.0) / ((earthRadius * (1 - earthEccentricity)) / (magic * sqrt_magic) * Math.PI);
            double magic_longitude = point.GetX() + dLon;
            double magic_latitude = point.GetY() + dLat;
            point.SetX(point.GetX() * 2 - magic_longitude);
            point.SetY(point.GetY() * 2 - magic_latitude);
        }
    }
}
