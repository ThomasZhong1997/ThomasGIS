using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.Mesh.Vector
{
    public class Vector2D : Point
    {
        public Vector2D(double x, double y) : base(x, y)
        {

        }

        public Vector2D(IPoint point) : base(point)
        {
            
        }

        public Vector2D() : base(0, 0)
        {
            
        }

        public static Vector2D Zero => new Vector2D(0, 0);

        public double Dot(Vector2D other)
        {
            return this.X * other.X + this.Y * other.Y;
        }

        public double Cross(Vector2D other)
        {
            return this.X * other.Y - this.Y * other.X;
        }

        public double Length()
        {
            return Math.Sqrt(this.X * this.X + this.Y * this.Y);
        }

        public double LengthSquared()
        {
            return (this.X * this.X + this.Y * this.Y);
        }

        public Vector2D Normalize()
        {
            double length = this.Length();
            return new Vector2D(this.X / length, this.Y / length);
        }

        public static double Dot(Vector2D v1, Vector2D v2)
        {
            return v1.X * v2.X + v1.Y * v2.Y;
        }

        public static double Cross(Vector2D v1, Vector2D v2)
        {
            return v1.X * v2.Y - v1.Y * v2.X;
        }

        public static double Length(Vector2D v)
        {
            return Math.Sqrt(v.X * v.X + v.Y * v.Y);
        }

        public static double LengthSquared(Vector2D v)
        {
            return v.X * v.X + v.Y * v.Y;
        }

        public static Vector2D Normalize(Vector2D v)
        {
            double length = Vector2D.Length(v);
            return new Vector2D(v.X / length, v.Y / length);
        }

        public static Vector2D operator -(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X - v2.X, v1.Y - v2.Y);
        }

        public static Vector2D operator -(Vector2D v)
        {
            return new Vector2D(-v.X, -v.Y);
        }

        public static Vector2D operator *(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X * v2.X, v1.Y * v2.Y);
        }

        public static Vector2D operator +(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X + v2.X, v1.Y + v2.Y);
        }

        public static Vector2D operator /(Vector2D v1, Vector2D v2)
        {
            return new Vector2D(v1.X / v2.X, v1.Y / v2.Y);
        }

        public static Vector2D operator *(Vector2D v1, double v)
        {
            return new Vector2D(v1.X * v, v1.Y * v);
        }

        public static Vector2D operator /(Vector2D v1, double v)
        {
            return new Vector2D(v1.X / v, v1.Y / v);
        }
    }
}
