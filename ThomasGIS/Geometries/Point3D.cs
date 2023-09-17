using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Geometries
{
    public class Point3D : Point, IPoint3D
    {
        public double Z
        {
            get
            {
                return this[2];
            }
            set
            {
                this[2] = value;
            }
        }

        public Point3D(double x, double y, double z) : base(x, y)
        {
            this.Add(z);
        }

        public Point3D(double x, double y) : base(x, y)
        {
            this.Add(0);
        }

        public Point3D() : base(0, 0)
        {
            this.Add(0);
        }

        public Point3D(string wkt) : base()
        {
            // "POINT (x y)"
            wkt = wkt.ToUpper();
            string subTypeString = wkt.Substring(0, 5);

            if (subTypeString != "POINT")
            {
                throw new Exception("不符合的WKT字符串类型");
            }

            // 防止用户输入的WKT字符串不标准
            try
            {
                // 移除POINT标识
                string wktContent = wkt.Substring(5, wkt.Length - 5);
                // 移除前后可能存在的空格
                wktContent = wktContent.Trim();
                // 移除前后括号
                wktContent = wktContent.Substring(1, wktContent.Length - 2);
                // 按空格划分x和y
                string[] coordinates = wktContent.Trim(' ').Split(' ');
                // 赋值
                X = Convert.ToDouble(coordinates[0]);
                Y = Convert.ToDouble(coordinates[1]);

                if (coordinates.Length < 3)
                {
                    Z = 0;
                }
                else
                {
                    Z = Convert.ToDouble(coordinates[2]);
                }
            }
            catch (Exception exception)
            {
                exception.ToString();
                throw new Exception("WKT字符串错误");
            }
        }

        public override string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("POINT (");
            wktBuilder.Append(X);
            wktBuilder.Append(" ");
            wktBuilder.Append(Y);
            wktBuilder.Append(" ");
            wktBuilder.Append(Z);
            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public override string ExportToWktMysql()
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("POINT (");
            wktBuilder.Append(Y);
            wktBuilder.Append(" ");
            wktBuilder.Append(X);
            wktBuilder.Append(" ");
            wktBuilder.Append(Z);
            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public override string GetGeometryType()
        {
            return "Point3D";
        }

        public double GetZ()
        {
            return this.Z;
        }

        public void SetZ(double z)
        {
            this.Z = z;
        }
    }
}
