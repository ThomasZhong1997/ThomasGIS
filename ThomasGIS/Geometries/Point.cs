using ThomasGIS.Exceptions;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Vector;
using ThomasGIS.DataManagement;

namespace ThomasGIS.Geometries
{
    public class Point : List<double>, IPoint
    {
        public double X
        {
            get
            {
                return this[0];
            }
            set
            {
                this[0] = value;
            }
        }
        public double Y
        {
            get 
            {
                return this[1]; 
            }
            set 
            { 
                this[1] = value; 
            }
        }

        public Point()
        {
            this.Add(0);
            this.Add(0);
        }

        public Point(double x, double y) 
        {
            this.Add(x);
            this.Add(y);
        }

        public Point(string wkt)
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
                this.Add(Convert.ToDouble(coordinates[0]));
                this.Add(Convert.ToDouble(coordinates[1]));
            }
            catch (Exception exception)
            {
                exception.ToString();
                throw new Exception("WKT字符串错误");
            }
        }

        public Point(byte[] wkb)
        {
            byte byteOrder = wkb[0];

            ByteArrayReader wkbReader;
            if ((!BitConverter.IsLittleEndian && byteOrder == 0) || (BitConverter.IsLittleEndian && byteOrder == 1))
            {
                wkbReader = new ByteArrayReader(wkb, false);
            }
            else
            {
                wkbReader = new ByteArrayReader(wkb, true);
            }

            byteOrder= wkbReader.ReadByte();
            uint geometryType = wkbReader.ReadUInt();
            uint geometryNumber = wkbReader.ReadUInt();

            if (geometryType != 0x00000001)
            {
                throw new Exception("Error Point WKB Byte Array! Please Check!");
            }

            X = wkbReader.ReadDouble();
            Y = wkbReader.ReadDouble();
        }

        public Point(Point point)
        {
            this.Add(point.X);
            this.Add(point.Y);
        }

        public Point(IPoint point)
        {
            this.Add(point.GetX());
            this.Add(point.GetY());
        }

        public static Point ParseWkt(string wkt)
        {
            return new Point(wkt);
        }

        public virtual string ExportToWkt() 
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("POINT (");
            wktBuilder.Append(X);
            wktBuilder.Append(" ");
            wktBuilder.Append(Y);
            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public virtual string ExportToWktMysql()
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("POINT (");
            wktBuilder.Append(Y);
            wktBuilder.Append(" ");
            wktBuilder.Append(X);
            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public double GetX()
        {
            return this.X;
        }

        public double GetY()
        {
            return this.Y;
        }

        public bool SetX(double x)
        {
            this.X = x;
            return true;
        }

        public bool SetY(double y)
        {
            this.Y = y;
            return true;
        }

        public virtual string GetGeometryType()
        {
            return "Point";
        }

        public override bool Equals(object obj)
        {
            IPoint otherPoint = obj as IPoint;
            if (this.GetX() == otherPoint.GetX() && this.GetY() == otherPoint.GetY())
            {
                return true;
            }
            return false;
        }

        virtual public string GetBaseGeometryType()
        {
            return "Point";
        }

        public BoundaryBox GetBoundaryBox()
        {
            return new BoundaryBox(X, Y, X, Y);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return base.ToString();
        }

        public bool NearlyEqual(IPoint other, double tolerate = 1e-6)
        {
            if (Math.Abs(X - other.GetX()) < tolerate && Math.Abs(Y - other.GetY()) < tolerate)
            {
                return true;
            }

            return false;
        }

        public static IPoint MiddlePoint(IPoint p1, IPoint p2)
        {
            return new Point(p1.GetX() * 0.5 + p2.GetX() * 0.5, p1.GetY() * 0.5 + p2.GetY() * 0.5);
        }
    }
}
