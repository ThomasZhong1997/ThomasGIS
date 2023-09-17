using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.Mesh.Vector
{
    public class Vector3D : Point3D
    {
        public Vector3D(double x, double y, double z) : base(x, y, z)
        {

        }

        public Vector3D(Vector3D clone) : base()
        {
            this[0] = clone.X;
            this[1] = clone.Y;
            this[2] = clone.Z;
        }

        public Vector3D(IPoint point, double z) : base(point.GetX(), point.GetY(), z)
        {
            
        }

        public Vector3D(Vector2D v2d, double z) : base(v2d.X, v2d.Y, z)
        {
            
        }

        public Vector3D Normalize()
        {
            double magnitude = Math.Sqrt(X * X + Y * Y + Z * Z);
            Vector3D result = new Vector3D(X / magnitude, Y / magnitude, Z / magnitude);
            return result;
        }

        public static Vector3D Max(Vector3D prev, Vector3D now)
        {
            double maxX = Math.Max(prev.X, now.X);
            double maxY = Math.Max(prev.Y, now.Y);
            double maxZ = Math.Max(prev.Z, now.Z);
            return new Vector3D(maxX, maxY, maxZ);
        }

        public static Vector3D Min(Vector3D prev, Vector3D now)
        {
            double minX = Math.Min(prev.X, now.X);
            double minY = Math.Min(prev.Y, now.Y);
            double minZ = Math.Min(prev.Z, now.Z);
            return new Vector3D(minX, minY, minZ);
        }

        public static Vector3D operator- (Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X - v2.X, v1.Y - v2.Y, v1.Z - v2.Z);
        }

        public static Vector3D operator +(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X + v2.X, v1.Y + v2.Y, v1.Z + v2.Z);
        }

        public static Vector3D operator *(Vector3D v1, double v2)
        {
            return new Vector3D(v1.X * v2, v1.Y * v2, v1.Z * v2);
        }

        public static Vector3D operator *(double v2, Vector3D v1)
        {
            return new Vector3D(v1.X * v2, v1.Y * v2, v1.Z * v2);
        }

        public static Vector3D operator *(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X * v2.X, v1.Y * v2.Y, v1.Z * v2.Z);
        }

        public static double Dot(Vector3D v1, Vector3D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
        }

        public double Length()
        {
            return Math.Sqrt(this.X * this.X + this.Y * this.Y + this.Z * this.Z);
        }

        public double Dot(Vector3D v1)
        {
            return v1.X * this.X + v1.Y * this.Y + v1.Z * this.Z;
        }

        public Vector3D Cross(Vector3D v1)
        {
            return new Vector3D(this.Y * v1.Z - v1.Y * this.Z, v1.X * this.Z - this.X * v1.Z, this.X * v1.Y - v1.X * this.Y);
        }

        public static Vector3D Cross(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.Y * v2.Z - v2.Y * v1.Z, v2.X * v1.Z - v1.X * v2.Z, v1.X * v2.Y - v2.X * v1.Y);
        }

        public static Vector3D operator /(Vector3D v1, Vector3D v2)
        {
            return new Vector3D(v1.X / v2.X, v1.Y / v2.Y, v1.Z / v2.Z);
        }

        public static Vector3D operator /(Vector3D v1, double v2)
        {
            return new Vector3D(v1.X / v2, v1.Y / v2, v1.Z / v2);
        }

        public static Vector3D operator /(double v2, Vector3D v1)
        {
            return new Vector3D(v1.X / v2, v1.Y / v2, v1.Z / v2);
        }

        public static Vector3D operator -(Vector3D a)
        {
            return new Vector3D(-a.X, -a.Y, -a.Z);
        }

        public static bool operator ==(Vector3D v1, Vector3D v2)
        {
            if (v1 is null && v2 is null) return true;

            if (v1[0] == v2[0] && v1[1] == v2[1] && v1[2] == v2[2])
            {
                return true;
            }
            return false;
        }

        public static bool operator !=(Vector3D v1, Vector3D v2)
        {
            if (v1 == null && v2 == null) return false;

            if (v1[0] == v2[0] && v1[1] == v2[1] && v1[2] == v2[2])
            {
                return false;
            }
            return true;
        }

        public double DistanceWith(Vector3D other)
        {
            double distance = Math.Sqrt(Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2) + Math.Pow(Z - other.Z, 2));
            return distance;
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public override string ToString()
        {
            return X.ToString() + "," + Y.ToString() + "," + Z.ToString();
        }

        public static Vector3D Zero
        {
            get
            {
                return new Vector3D(0, 0, 0);
            }
        }

        public Vector3D Cross()
        {
            Vector3D result = Vector3D.Zero;

            if (this[0] == 0 && this[1] == 0 && this[2] == 0) return result;

            if (this[0] != 0 && this[1] != 0)
            {
                result[0] = 1.0 / this[0];
                result[1] = -1.0 / this[1];
                result[2] = 0.0;
            }
            else if (this[1] != 0 && this[2] != 0)
            {
                result[0] = 0.0;
                result[1] = 1.0 / this[1];
                result[2] = -1.0 / this[2];
            }
            else if (this[0] != 0 && this[2] != 0)
            {
                result[0] = 1.0 / this[0];
                result[1] = 0.0;
                result[2] = -1.0 / this[2];
            }
            else if (this[0] != 0)
            {
                result[1] = 1.0;
            }
            else if (this[1] != 0)
            {
                result[2] = 1.0;
            }
            else if (this[2] != 0)
            {
                result[0] = 1.0;
            }

            return result;
        }

        public static double Length(Vector3D vec3d)
        {
            return vec3d.Length();
        }
    }
}
