using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.Projections
{
    public class LambertProjection : IProjection
    {
        public string Name { get; set; } = "Lambert_Conformal_Conic";

        public string Type { get; set; } = "Lambert_Conformal_Conic";

        public double StandardParallel_1 { get; set; } = 60.0;

        public double StandardParallel_2 { get; set; } = 60.0;

        public double CentralMeridian { get; set; } = 0.0;

        public double FalseEasting { get; set; } = 0.0;

        public double FalseNorthing { get; set; } = 0.0;

        public double ScaleFactor { get; set; } = 1.0;

        public double LatitudeOfOrigin { get; set; } = 0.0;

        public LambertProjection(string name, double originLatitude, double originLongitude, double standard_1, double standard_2, double scale = 1.0, double falseEasting = 0.0, double falseNorthing = 0.0)
        {
            this.Name = name;
            this.LatitudeOfOrigin = originLatitude / 180.0 * Math.PI;
            this.CentralMeridian = originLongitude / 180.0 * Math.PI;
            this.StandardParallel_1 = standard_1 / 180.0 * Math.PI;
            this.StandardParallel_2 = standard_2 / 180.0 * Math.PI;
            this.ScaleFactor = scale;
            this.FalseEasting = falseEasting;
            this.FalseNorthing = falseNorthing;
        }

        public bool SetParameters(Dictionary<string, double> inputParameter)
        {
            if (inputParameter.ContainsKey("central_meridian"))
            {
                this.CentralMeridian = inputParameter["central_meridian"] / 180.0 * Math.PI;
            }

            if (inputParameter.ContainsKey("latitude_of_origin"))
            {
                this.CentralMeridian = inputParameter["latitude_of_origin"] / 180.0 * Math.PI;
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

            if (inputParameter.ContainsKey("standard_parallel_1"))
            {
                this.ScaleFactor = inputParameter["standard_parallel_1"] / 180.0 * Math.PI;
            }

            if (inputParameter.ContainsKey("standard_parallel_2"))
            {
                this.ScaleFactor = inputParameter["standard_parallel_2"] / 180.0 * Math.PI;
            }

            return true;
        }

        public LambertProjection(CoordinateBase coordinateBase)
        {
            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                ProjectedCoordinate trueCoordinate = coordinateBase as ProjectedCoordinate;
                this.Name = trueCoordinate.Name;
                this.Type = trueCoordinate.Projection;
                SetParameters(trueCoordinate.Parameters);
            }
        }

        void IProjection.Backward(IPoint point, double a, double flatten)
        {
            flatten = 1.0 / flatten;
            double e = Math.Sqrt(2 * flatten - flatten * flatten);
            double m1 = Math.Cos(StandardParallel_1) / Math.Sqrt(1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(StandardParallel_1), 2));
            double m2 = Math.Cos(StandardParallel_2) / Math.Sqrt(1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(StandardParallel_2), 2));

            double t0 = Math.Tan(Math.PI / 4.0 - LatitudeOfOrigin / 2.0) / Math.Pow((1 - e * Math.Sin(LatitudeOfOrigin)) / (1 + e * Math.Sin(LatitudeOfOrigin)), e / 2);
            double t1 = Math.Tan(Math.PI / 4.0 - StandardParallel_1 / 2.0) / Math.Pow((1 - e * Math.Sin(StandardParallel_1)) / (1 + e * Math.Sin(StandardParallel_1)), e / 2);
            double t2 = Math.Tan(Math.PI / 4.0 - StandardParallel_2 / 2.0) / Math.Pow((1 - e * Math.Sin(StandardParallel_2)) / (1 + e * Math.Sin(StandardParallel_2)), e / 2);

            double n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));

            if (StandardParallel_1 == StandardParallel_2) n = 1;

            double F = m1 / (n * Math.Pow(t1, n));

            double rou0 = a * F * Math.Pow(t0, n);

            double Np = (point.GetY() - FalseNorthing) / ScaleFactor;
            double Ep = (point.GetX() - FalseEasting) / ScaleFactor;

            double roup = Math.Sqrt(Math.Pow(Ep, 2) + Math.Pow(rou0 - Np, 2));

            if (n < 0) roup = -roup;

            double tp = Math.Pow(roup / (a * F), 1 / n);
            double rp = Math.Atan(Ep / (rou0 - Np));

            double lat = Math.PI - 2 * Math.Atan(tp);
            while (true)
            {
                double nextLat = Math.PI - 2 * Math.Atan(tp * Math.Pow((1 - e * Math.Sin(lat)) / (1 + e * Math.Sin(lat)), e / 2));
                if (nextLat - lat < 0.000000001)
                {
                    break;
                }
                lat = nextLat;
            }

            double lon = rp / n + CentralMeridian;

            point.SetX(lon);
            point.SetY(lat);
        }

        string IProjection.GetProjectionName()
        {
            return this.Name;
        }

        string IProjection.GetProjectionType()
        {
            return this.Type;
        }

        Dictionary<string, double> IProjection.GetWrittenParameters()
        {
            Dictionary<string, double> parameters = new Dictionary<string, double>();
            parameters.Add("central_meridian", this.CentralMeridian * 180.0 / Math.PI);
            parameters.Add("latitude_of_origin", this.LatitudeOfOrigin * 180.0 / Math.PI);
            parameters.Add("false_northing", this.FalseNorthing);
            parameters.Add("false_easting", this.FalseEasting);
            parameters.Add("scale_factor", this.ScaleFactor);
            parameters.Add("standard_parallel_1", this.StandardParallel_1 * 180.0 / Math.PI);
            parameters.Add("standard_parallel_2", this.StandardParallel_2 * 180.0 / Math.PI);
            return parameters;
        }

        void IProjection.Toward(IPoint point, double a, double flatten)
        {
            flatten = 1.0 / flatten;
            double e = Math.Sqrt(2 * flatten - flatten * flatten);
            double m1 = Math.Cos(StandardParallel_1) / Math.Sqrt(1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(StandardParallel_1), 2));
            double m2 = Math.Cos(StandardParallel_2) / Math.Sqrt(1 - Math.Pow(e, 2) * Math.Pow(Math.Sin(StandardParallel_2), 2));

            double t0 = Math.Tan(Math.PI / 4.0 - LatitudeOfOrigin / 2.0) / Math.Pow((1 - e * Math.Sin(LatitudeOfOrigin)) / (1 + e * Math.Sin(LatitudeOfOrigin)), e / 2);
            double t1 = Math.Tan(Math.PI / 4.0 - StandardParallel_1 / 2.0) / Math.Pow((1 - e * Math.Sin(StandardParallel_1)) / (1 + e * Math.Sin(StandardParallel_1)), e / 2);
            double t2 = Math.Tan(Math.PI / 4.0 - StandardParallel_2 / 2.0) / Math.Pow((1 - e * Math.Sin(StandardParallel_2)) / (1 + e * Math.Sin(StandardParallel_2)), e / 2);

            double n = (Math.Log(m1) - Math.Log(m2)) / (Math.Log(t1) - Math.Log(t2));

            if (StandardParallel_1 == StandardParallel_2) n = 1;

            double F = m1 / (n * Math.Pow(t1, n));

            double rou0 = a * F * Math.Pow(t0, n);

            double t = Math.Tan(Math.PI / 4.0 - point.GetY() / 180.0 * Math.PI / 2.0) / Math.Pow((1 - e * Math.Sin(point.GetY() / 180.0 * Math.PI)) / (1 + e * Math.Sin(point.GetY() / 180.0 * Math.PI)), e / 2);
            double rou = a * F * Math.Pow(t, n);
            double r = n * (point.GetX() / 180.0 * Math.PI - CentralMeridian);
            double N = FalseNorthing + ScaleFactor * (rou0 - rou * Math.Cos(r));
            double E = FalseEasting + ScaleFactor * (rou * Math.Sin(r));

            point.SetX(E);
            point.SetY(N);
        }
    }
}
