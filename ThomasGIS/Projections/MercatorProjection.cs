using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public class MercatorProjection : IProjection
    {
        public double FalseEasting { get; set; } = 0;
        public double FalseNorthing { get; set; } = 0;
        public double CentralMeridian { get; set; } = 0;
        public double StandardParallel { get; set; } = 0;
        public string Name { get; set; } = "Mercator";
        public string Type { get; set; } = "Mercator";

        public string GetProjectionName()
        {
            return Name;
        }

        public string GetProjectionType()
        {
            return Type;
        }

        public MercatorProjection(string name, double B0, double L0, double xOffset = 0, double yOffset = 0)
        {
            this.Name = name;
            this.Type = "Mercator";
            this.CentralMeridian = L0;
            this.StandardParallel = B0;
            this.FalseEasting = xOffset;
            this.FalseNorthing = yOffset;
        }

        public bool SetParameters(Dictionary<string, double> inputParameter)
        {
            if (inputParameter.ContainsKey("central_meridian"))
            {
                this.CentralMeridian = inputParameter["central_meridian"];
            }

            if (inputParameter.ContainsKey("standard_parallel_1"))
            {
                this.StandardParallel = inputParameter["standard_parallel_1"];
            }

            if (inputParameter.ContainsKey("false_northing"))
            {
                this.FalseNorthing = inputParameter["false_northing"];
            }

            if (inputParameter.ContainsKey("false_easting"))
            {
                this.FalseNorthing = inputParameter["false_easting"];
            }

            return true;
        }

        public MercatorProjection(CoordinateBase coordinateBase)
        {
            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                ProjectedCoordinate trueCoordinate = coordinateBase as ProjectedCoordinate;
                this.Name = trueCoordinate.Name;
                this.Type = trueCoordinate.Projection;
                SetParameters(trueCoordinate.Parameters);
            }
        }

        public Dictionary<string, double> GetWrittenParameters()
        {
            Dictionary<string, double> parameters = new Dictionary<string, double>();
            parameters.Add("central_meridian", this.CentralMeridian);
            parameters.Add("standard_parallel_1", this.StandardParallel);
            parameters.Add("false_northing", this.FalseNorthing);
            parameters.Add("false_easting", this.FalseEasting);
            return parameters;
        }

        public void Toward(IPoint point, double a = 6378137, double flatten = 298.257223563)
        {
            // 短半轴
            double b = a - (1.0 / flatten * a);
            // 第一偏心率
            double e = Math.Pow(a * a - b * b, 0.5) / a;
            // 第二偏心率
            double ep = Math.Pow(a * a - b * b, 0.5) / b;

            double K = ((a * a / b) / Math.Sqrt(1 + Math.Pow(ep, 2) * Math.Pow(Math.Cos(this.StandardParallel / 180.0 * Math.PI), 2))) * Math.Cos(this.StandardParallel / 180.0 * Math.PI);
            double projX = K * (point.GetX() / 180.0 * Math.PI - this.CentralMeridian / 180.0 * Math.PI) + this.FalseEasting;
            double projY = K * Math.Log((Math.Tan(Math.PI / 4.0 + point.GetY() / 180.0 * Math.PI / 2.0)) * Math.Pow(((1 - e * Math.Sin(point.GetY() / 180.0 * Math.PI)) / (1 + e * Math.Sin(point.GetY() / 180.0 * Math.PI))), e / 2)) + FalseNorthing;
            point.SetX(projX);
            point.SetY(projY);
        }

        public void Backward(IPoint point, double a = 6378137, double flatten = 298.257223563)
        {
            // 短半轴
            double b = a - (1.0 / flatten * a);
            // 第一偏心率
            double e = Math.Pow(a * a - b * b, 0.5) / a;
            // 第二偏心率
            double ep = Math.Pow(a * a - b * b, 0.5) / b;

            point.SetX(point.GetX() - this.FalseEasting);
            point.SetY(point.GetY() -  this.FalseNorthing);

            double K = ((a * a / b) / Math.Sqrt(1 + Math.Pow(ep, 2) * Math.Pow(Math.Cos(this.StandardParallel / 180.0 * Math.PI), 2))) * Math.Cos(this.StandardParallel / 180.0 * Math.PI);
            point.SetX(((point.GetX() / K) + (this.CentralMeridian / 180.0 * Math.PI)) * 180.0 / Math.PI);
            double latitude = 0;
            double newLatitude = Math.PI / 2.0 - 2.0 * Math.Atan(Math.Exp(-point.GetY() / K) * Math.Exp(e / 2.0 * Math.Log((1 - e * Math.Sin(latitude)) / (1 + e * Math.Sin(latitude)))));
            while (Math.Abs(latitude - newLatitude) > 0.00001)
            {
                latitude = newLatitude;
                newLatitude = Math.PI / 2.0 - 2.0 * Math.Atan(Math.Exp(-point.GetY() / K) * Math.Exp(e / 2.0 * Math.Log((1 - e * Math.Sin(latitude)) / (1 + e * Math.Sin(latitude)))));
            }
            point.SetY(newLatitude * 180.0 / Math.PI);
        }
    }
}
