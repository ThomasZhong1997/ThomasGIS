using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public class GaussKrugerProjection : IProjection
    {
        public double CentralMeridian { get; set; } = 0;
        public double FalseEasting { get; set; } = 0;
        public double FalseNorthing { get; set; } = 0;
        public string Name { get; set; } = "Gauss_Kruger";
        public string Type { get; set; } = "Gauss_Kruger";

        public string GetProjectionName()
        {
            return Name;
        }

        public string GetProjectionType()
        {
            return Type;
        }

        public GaussKrugerProjection(string name, double L0, double xOffset = 500000, double yOffset = 0)
        {
            this.Name = name;
            this.Type = "Gauss_Kruger";
            this.CentralMeridian = L0;
            this.FalseEasting = xOffset;
            this.FalseNorthing = yOffset;
        }

        public bool SetParameters(Dictionary<string, double> inputParameter)
        {
            if (inputParameter.ContainsKey("central_meridian"))
            {
                this.CentralMeridian = inputParameter["central_meridian"];
            }

            if (inputParameter.ContainsKey("false_northing"))
            {
                this.FalseNorthing = inputParameter["false_northing"];
            }

            if (inputParameter.ContainsKey("false_easting"))
            {
                this.FalseEasting = inputParameter["false_easting"];
            }

            return true;
        }

        public GaussKrugerProjection(CoordinateBase coordinateBase)
        {
            if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
            {
                ProjectedCoordinate trueCoordinate = coordinateBase as ProjectedCoordinate;
                this.Name = trueCoordinate.Name;
                this.Type = trueCoordinate.Projection;
                SetParameters(trueCoordinate.Parameters);
            }
        }

        public void Toward(IPoint point, double a = 6378137, double flatten = 298.257223563)
        {
            // 短半轴
            double b = a - (1.0 / flatten * a);
            // 第一偏心率的平方
            double e = Math.Pow(Math.Pow(a * a - b * b, 0.5) / a, 2);
            // 第二偏心率的平方
            double ep = Math.Pow(Math.Pow(a * a - b * b, 0.5) / b, 2);

            // 原始经纬度
            double B = point.GetY() / 180.0 * Math.PI;
            double L = point.GetX() / 180.0 * Math.PI;

            // 辅助计算公式
            double W = Math.Sqrt(1 - e * Math.Sin(B) * Math.Sin(B));
            double n2 = ep * Math.Cos(B) * Math.Cos(B);
            double t = Math.Tan(B);

            // 曲率半径
            double N = a / W;
            double M = a * (1 - e) / Math.Pow(W, 3);
            double M0 = a * (1 - e);

            double Ac = 1.0 + 3.0 / 4.0 * e + 45.0 / 64.0 * Math.Pow(e, 2) + 175.0 / 256.0 * Math.Pow(e, 3) + 11025.0 / 16384.0 * Math.Pow(e, 4) + 43659.0 / 65536.0 * Math.Pow(e, 5);
            double Bc = 3.0 / 4.0 * e + 15.0 / 16.0 * Math.Pow(e, 2) + 525.0 / 512.0 * Math.Pow(e, 3) + 2205.0 / 2048.0 * Math.Pow(e, 4) + 72765.0 / 65536.0 * Math.Pow(e, 5);
            double Cc = 15.0 / 64.0 * Math.Pow(e, 2) + 105.0 / 256.0 * Math.Pow(e, 3) + 2205.0 / 4096.0 * Math.Pow(e, 4) + 10395.0 / 16384.0 * Math.Pow(e, 5);
            double Dc = 35.0 / 512.0 * Math.Pow(e, 3) + 315.0 / 2048.0 * Math.Pow(e, 4) + 31185.0 / 131072.0 * Math.Pow(e, 5);
            double Ec = 315.0 / 16384.0 * Math.Pow(e, 4) + 3465.0 / 65536.0 * Math.Pow(e, 5);
            double Fc = 693.0 / 131072.0 * Math.Pow(e, 5);
            double alpha = Ac * M0;
            double Beta = -1.0 / 2.0 * Bc * M0;
            double Gamma = 1.0 / 4.0 * Cc * M0;
            double Delte = -1.0 / 6.0 * Dc * M0;
            double Epsilon = 1.0 / 8.0 * Ec * M0;
            double Zeta = -1.0 / 10.0 * Fc * M0;

            // 子午弧长
            double X = alpha * B + Beta * Math.Sin(2 * B) + Gamma * Math.Sin(4 * B) + Delte * Math.Sin(6 * B) + Epsilon * Math.Sin(8 * B) + Zeta * Math.Sin(10 * B);

            // 经差
            double l = L - this.CentralMeridian / 180.0 * Math.PI;
            // 辅助量
            double a0 = X;
            double a1 = N * Math.Cos(B);
            double a2 = 1.0 / 2.0 * N * Math.Pow(Math.Cos(B), 2) * t;
            double a3 = 1.0 / 6.0 * N * Math.Pow(Math.Cos(B), 3) * (1.0 - Math.Pow(t, 2) + n2);
            double a4 = 1.0 / 24.0 * N * Math.Pow(Math.Cos(B), 4) * (5.0 - Math.Pow(t, 2) + 9.0 * n2 + 4.0 * Math.Pow(n2, 2)) * t;
            double a5 = 1.0 / 120.0 * N * Math.Pow(Math.Cos(B), 5) * (5.0 - 18.0 * Math.Pow(t, 2) + Math.Pow(t, 4) + 14.0 * n2 - 58.0 * n2 * Math.Pow(t, 2));
            double a6 = 1.0 / 720.0 * N * Math.Pow(Math.Cos(B), 6) * (61.0 - 58.0 * Math.Pow(t, 2) + Math.Pow(t, 4) + 270.0 * n2 - 330.0 * n2 * Math.Pow(t, 2)) * t;

            point.SetY(a0 + a2 * Math.Pow(l, 2) + a4 * Math.Pow(l, 4) + a6 * Math.Pow(l, 6) + FalseNorthing);
            point.SetX(a1 * l + a3 * Math.Pow(l, 3) + a5 * Math.Pow(l, 5) + FalseEasting);
        }

        public void Backward(IPoint point, double a = 6378137, double flatten = 298.257223563)
        {
            // 短半轴
            double b = a - (1.0 / flatten * a);
            // 第一偏心率的平方
            double e = Math.Pow(Math.Pow(a * a - b * b, 0.5) / a, 2);
            // 第二偏心率的平方
            double ep = Math.Pow(Math.Pow(a * a - b * b, 0.5) / b, 2);

            double m0 = a * (1 - e);
            double m2 = 3.0 / 2.0 * e * m0;
            double m4 = 5.0 / 4.0 * e * m2;
            double m6 = 7.0 / 6.0 * e * m4;
            double m8 = 9.0 / 8.0 * e * m6;

            double a0 = m0 + 0.5 * m2 + 3.0 / 8.0 * m4 + 5.0 / 16.0 * m6 + 35.0 / 128.0 * m8;
            double a2 = 0.5 * m2 + 0.5 * m4 + 15.0 / 32.0 * m6 + 7.0 / 16.0 * m8;
            double a4 = 1.0 / 8.0 * m4 + 3.0 / 16.0 * m6 + 7.0 / 32.0 * m8;
            double a6 = 1.0 / 32.0 * m6 + 1.0 / 16.0 * m8;
            double a8 = 1 / 128.0 * m8;

            double A1 = a * (1 - e) * (1 + 3.0 / 4.0 * e + 45.0 / 64.0 * e * e + 175.0 / 256.0 * e * e * e + 11025.0 / 16384.0 * e * e * e * e);
            double A2 = a * (1 - e) * (3.0 / 4.0 * e + 15.0 / 16.0 * e * e + 525.0 / 512.0 * e * e * e + 2205.0 / 2048.0 * e * e * e * e);
            double A3 = a * (1 - e) * (15.0 / 64.0 * e * e + 105.0 / 256.0 * e * e * e + 2205.0 / 4096.0 * e * e * e * e);
            double A4 = a * (1 - e) * (35.0 / 512.0 * e * e * e + 315.0 / 2048.0 * e * e * e * e);

            point.SetY(point.GetY() - this.FalseNorthing);
            point.SetX(point.GetX() - this.FalseEasting);

            double B0 = (point.GetY()) / a0;

            while (true)
            {
                double F = -a2 / 2.0 * Math.Sin(2 * B0) + a4 / 4.0 * Math.Sin(4 * B0) - a6 / 6.0 * Math.Sin(6 * B0) + a8 / 8.0 * Math.Sin(8 * B0);
                double BX = (point.GetY() - F) / a0;
                if (Math.Abs(B0 - BX) < 0.0000000001)
                {
                    B0 = BX;
                    break;
                }
                B0 = BX;
            }


            double W = Math.Sqrt(1 - e * Math.Pow(Math.Sin(B0), 2));
            double Mf = a * (1 - e) / (W * W * W);
            double Nf = a / W;
            double tf = Math.Tan(B0);
            double nf = Math.Sqrt(ep) * Math.Cos(B0);

            double B = B0 - tf * Math.Pow(point.GetX(), 2) / (2.0 * Mf * Nf) + tf * (5.0 + 3.0 * Math.Pow(tf, 2) + Math.Pow(nf, 2) - 9.0 * Math.Pow(nf, 2) * Math.Pow(tf, 2)) * Math.Pow(point.GetX(), 4) / (24.0 * Mf * Math.Pow(Nf, 3)) - tf * (61.0 + 90.0 * Math.Pow(tf, 2) + 45.0 * Math.Pow(tf, 4)) * Math.Pow(point.GetX(), 6) / (720.0 * Mf * Math.Pow(Nf, 5));
            double L = (1.0 / Math.Cos(B0)) * (point.GetX() / Nf) * (1.0 - 1.0 / 6.0 * (1.0 + 2.0 * tf * tf + nf * nf) * Math.Pow(point.GetX() / Nf, 2) + 1.0 / 120.0 * (5.0 + 28.0 * tf * tf + 24.0 * Math.Pow(tf, 4) + 6.0 * nf * nf + 8.0 * nf * nf * tf * tf) * Math.Pow(point.GetX() / Nf, 4));

            point.SetX(this.CentralMeridian + L / Math.PI * 180.0);
            point.SetY(B / Math.PI * 180.0);
        }

        public Dictionary<string, double> GetWrittenParameters()
        {
            Dictionary<string, double> parameters = new Dictionary<string, double>();
            parameters.Add("central_meridian", this.CentralMeridian);
            parameters.Add("false_northing", this.FalseNorthing);
            parameters.Add("false_easting", this.FalseEasting);
            parameters.Add("latitude_of_origin", 0.0);
            parameters.Add("scale_factor", 1.0);
            return parameters;
        }
    }
}
