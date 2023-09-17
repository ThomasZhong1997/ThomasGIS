using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.Projections
{
    public class TransverseMercatorProjection : IProjection
    {
        public double FalseEasting { get; set; }
        public double FalseNorthing { get; set; }
        public double CentralMeridian { get; set; }
        public double ScaleFactor { get; set; }
        public double LatitudeOfOrigin { get; set; }

        public string Name { get; set; } = "Transverse_Mercator";
        public string Type { get; set; } = "Transverse_Mercator";

        public TransverseMercatorProjection(string name, double latitudeOfOrigin, double centralMeridian, double scaleFactor = 1.0, double falseEasting = 0.0, double falseNorthing = 0.0)
        {
            this.Name = name;
            this.FalseEasting = falseEasting;
            this.FalseNorthing = falseNorthing;
            this.ScaleFactor = scaleFactor;
            this.CentralMeridian = centralMeridian / 180.0 * Math.PI;
            this.LatitudeOfOrigin = latitudeOfOrigin / 180.0 * Math.PI;
        }

        public bool SetParameters(Dictionary<string, double> inputParameter)
        {

            if (inputParameter.ContainsKey("central_meridian"))
            {
                this.CentralMeridian = inputParameter["central_meridian"] / 180.0 * Math.PI;
            }

            if (inputParameter.ContainsKey("latitude_of_origin"))
            {
                this.LatitudeOfOrigin = inputParameter["latitude_of_origin"] / 180.0 * Math.PI;
            }

            if (inputParameter.ContainsKey("false_northing"))
            {
                this.FalseNorthing = inputParameter["false_northing"];
            }

            if (inputParameter.ContainsKey("false_easting"))
            {
                this.FalseNorthing = inputParameter["false_easting"];
            }

            if (inputParameter.ContainsKey("scale_factor"))
            {
                this.ScaleFactor = inputParameter["scale_factor"];
            }

            return true;
        }

        public TransverseMercatorProjection(CoordinateBase coordinateBase)
        {
            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                ProjectedCoordinate trueCoordinate = coordinateBase as ProjectedCoordinate;
                this.Name = trueCoordinate.Name;
                this.Type = trueCoordinate.Projection;
                SetParameters(trueCoordinate.Parameters);
            }
        }

        void IProjection.Toward(IPoint point, double a, double flatten)
        {
            flatten = 1.0 / flatten;
            double b = a * (1 - flatten);
            double e2 = 2 * flatten - Math.Pow(flatten, 2);
            double A0 = 1 - e2 / 4.0 - 3.0 / 64.0 * Math.Pow(e2, 2) - 5.0 / 256.0 * Math.Pow(e2, 3);
            double A2 = 3.0 / 8.0 * (e2 + 1.0 / 4.0 * Math.Pow(e2, 2) + 15.0 / 128.0 * Math.Pow(e2, 3));
            double A4 = 15.0 / 256.0 * (Math.Pow(e2, 2) + 3.0 / 4.0 * Math.Pow(e2, 3));
            double A6 = 35.0 / 3072.0 * Math.Pow(e2, 3);
            double m0 = a * (A0 * LatitudeOfOrigin - A2 * Math.Sin(2 * LatitudeOfOrigin) + A4 * Math.Sin(4 * LatitudeOfOrigin) + A6 * Math.Sin(6 * LatitudeOfOrigin));

            double rou = a * (1 - e2) / Math.Pow((1 - e2 * Math.Pow(Math.Sin(point.GetY() / 180.0 * Math.PI), 2)), 3.0 / 2.0);
            double v = a / Math.Sqrt((1 - e2 * Math.Pow(Math.Sin(point.GetY() / 180.0 * Math.PI), 2)));
            double vx = v / rou;
            double t = Math.Tan(point.GetY() / 180.0 * Math.PI);

            double w = (point.GetX() / 180.0 * Math.PI) - CentralMeridian;

            double siny = Math.Sin(point.GetY() / 180.0 * Math.PI);
            double cosy = Math.Cos(point.GetY() / 180.0 * Math.PI);

            double term1 = Math.Pow(w, 2) / 2.0 * v * siny * cosy;
            double term2 = Math.Pow(w, 4) / 4.0 * v * siny * Math.Pow(cosy, 3) * (4 * vx * vx + vx - t * t);
            double term3 = Math.Pow(w, 6) / 720.0 * v * siny * Math.Pow(cosy, 5) * (8 * Math.Pow(vx, 4) * (11 - 24 * t * t) - 28.0 * Math.Pow(vx, 3) * (1 - 6 * t * t) + Math.Pow(vx, 2) * (1 - 32.0 * t * t) - vx * 2 * t * t + Math.Pow(t, 4));
            double term4 = Math.Pow(w, 8) / 40320.0 * v * siny * Math.Pow(cosy, 7) * (1385 - 3111 * t * t + 543 * Math.Pow(t, 4) - Math.Pow(t, 6));

            double m = a * (A0 * point.GetY() / 180.0 * Math.PI - A2 * Math.Sin(2 * point.GetY() / 180.0 * Math.PI) + A4 * Math.Sin(4 * point.GetY() / 180.0 * Math.PI) - A6 * Math.Sin(6 * point.GetY() / 180.0 * Math.PI));

            double N = FalseNorthing + ScaleFactor * (m - m0 + term1 + term2 + term3 + term4);

            double term5 = Math.Pow(w, 2) / 6.0 * Math.Pow(cosy, 2) * (vx - t * t);
            double term6 = Math.Pow(w, 4) / 120.0 * Math.Pow(cosy, 4) * (4 * Math.Pow(vx, 3) * (1 - 6 * t * t) + Math.Pow(vx, 2) * (1 + 8 * t * t) - vx * 2 * t * t + Math.Pow(t, 4));
            double term7 = Math.Pow(w, 6) / 5040.0 * Math.Pow(cosy, 6) * (61 - 479 * t * t + 179 * Math.Pow(t, 4) - Math.Pow(t, 6));

            double E = FalseEasting + ScaleFactor * v * w * cosy * (1 + term5 + term6 + term7);

            point.SetX(E);
            point.SetY(N);
        }

        void IProjection.Backward(IPoint point, double a, double flatten)
        {
            flatten = 1.0 / flatten;
            double b = a * (1 - flatten);
            double e2 = 2 * flatten - Math.Pow(flatten, 2);
            double A0 = 1 - e2 / 4.0 - 3.0 / 64.0 * Math.Pow(e2, 2) - 5.0 / 256.0 * Math.Pow(e2, 3);
            double A2 = 3.0 / 8.0 * (e2 + 1.0 / 4.0 * Math.Pow(e2, 2) + 15.0 / 128.0 * Math.Pow(e2, 3));
            double A4 = 15.0 / 256.0 * (Math.Pow(e2, 2) + 3.0 / 4.0 * Math.Pow(e2, 3));
            double A6 = 35.0 / 3072.0 * Math.Pow(e2, 3);
            double m0 = a * (A0 * LatitudeOfOrigin - A2 * Math.Sin(2 * LatitudeOfOrigin) + A4 * Math.Sin(4 * LatitudeOfOrigin) + A6 * Math.Sin(6 * LatitudeOfOrigin));

            double Np = point.GetY() - FalseNorthing;
            double mp = m0 + Np / ScaleFactor;
            double n = (a - b) / (a + b);
            double G = a * (1 - n) * (1 - n * n) * (1 + 9.0 / 4.0 * Math.Pow(n, 2) + 225.0 / 64.0 * Math.Pow(n, 4)) * (Math.PI / 180.0);
            double xi = mp * Math.PI / (180.0 * G);
            double lat_p = xi + (3.0 / 2.0 * n - 27.0 / 32.0 * Math.Pow(n, 3)) * Math.Sin(2 * xi) + (21.0 / 16.0 * Math.Pow(n, 2) - 55.0 / 32.0 * Math.Pow(n, 4)) * Math.Sin(4 * xi) + (151.0 / 96.0 * Math.Pow(n, 3)) * Math.Sin(6 * xi) + (1097.0 / 512.0 * Math.Pow(n, 4)) * Math.Sin(8 * xi);
            double roup = a * (1 - e2) / Math.Pow((1 - e2 * Math.Pow(Math.Sin(lat_p), 2)), 3.0 / 2.0);
            double vp = a / Math.Sqrt(1 - e2 * Math.Pow(Math.Sin(lat_p), 2));
            double vxp = vp / roup;
            double tp = Math.Tan(lat_p);
            double Ep = point.GetX() - FalseEasting;
            double x = Ep / (ScaleFactor * vp);

            double basement = tp / (ScaleFactor * roup);
            double term1 = basement * (Ep * x) / 2.0;
            double term2 = basement * Ep * Math.Pow(x, 3) / 24.0 * (-4 * Math.Pow(vxp, 2) + 9 * vxp * (1 - Math.Pow(tp, 2)) + 12 * Math.Pow(tp, 2));
            double term3 = basement * Ep * Math.Pow(x, 5) / 720.0 * (8 * Math.Pow(vxp, 4) * (11 - 24 * Math.Pow(tp, 2)) - 12 * Math.Pow(vxp, 3) * (21 - 71 * Math.Pow(tp, 2)) + 15 * Math.Pow(vxp, 2) * (15 - 98 * Math.Pow(tp, 2) + 15 * Math.Pow(tp, 4) + 180 * vxp * (5 * Math.Pow(tp, 2) - 3 * Math.Pow(tp, 4)) + 360.0 * Math.Pow(tp, 4)));
            double term4 = basement * Ep * Math.Pow(x, 7) / 40320.0 * (1385 + 3633 * Math.Pow(tp, 2) + 4095 * Math.Pow(tp, 4) + 1575 * Math.Pow(tp, 6));

            double lat = lat_p - term1 + term2 - term3 + term4;

            double term5 = x * (1.0 / Math.Cos(lat_p));
            double term6 = Math.Pow(x, 3) * (1.0 / Math.Cos(lat_p)) / 6.0 * (vxp + 2 * Math.Pow(tp, 2));
            double term7 = Math.Pow(x, 5) * (1.0 / Math.Cos(lat_p)) / 120.0 * (-4 * Math.Pow(vxp, 3) * (1 - 6 * Math.Pow(tp, 2)) + Math.Pow(vxp, 2) * (9 - 68 * Math.Pow(tp, 2)) + 72 * vxp * Math.Pow(tp, 2) + 24 * Math.Pow(tp, 4));
            double term8 = Math.Pow(x, 7) * (1.0 / Math.Cos(lat_p)) / 5040.0 * (61 + 662 * Math.Pow(tp, 2) + 1320 * Math.Pow(tp, 4) + 720 * Math.Pow(tp, 6));

            double lon = CentralMeridian + term5 - term6 + term7 - term8;

            point.SetX(lon * 180.0 / Math.PI);
            point.SetY(lat * 180.0 / Math.PI);
        }

        Dictionary<string, double> IProjection.GetWrittenParameters()
        {
            Dictionary<string, double> parameters = new Dictionary<string, double>();
            parameters.Add("central_meridian", this.CentralMeridian * 180.0 / Math.PI);
            parameters.Add("latitude_of_origin", this.LatitudeOfOrigin * 180.0 / Math.PI);
            parameters.Add("false_northing", this.FalseNorthing);
            parameters.Add("false_easting", this.FalseEasting);
            parameters.Add("scale_factor", this.ScaleFactor);
            return parameters;
        }

        string IProjection.GetProjectionName()
        {
            return this.Name;
        }

        string IProjection.GetProjectionType()
        {
            return this.Type;
        }
    }
}
