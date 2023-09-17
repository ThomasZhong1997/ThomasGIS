using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Mesh.Vector
{
    public class Vector4D : Vector3D
    {
        public double M => this[3];

        public Vector4D() : base(0, 0, 0)
        {
            this.Add(0);
        }

        public Vector4D(double x, double y, double z, double m) : base(x, y, z)
        {
            this.Add(m);
        }

        public Vector4D(Vector3D vec3d, double m) : base(vec3d)
        {
            this.Add(m);
        }

        public static Vector4D operator +(Vector4D v1, Vector4D v2)
        {
            Vector4D result = new Vector4D();
            for (int i = 0; i < 4; i++)
            {
                result[i] = v1[i] + v2[i];
            }
            return result;
        }

        public static Vector4D operator -(Vector4D v1, Vector4D v2)
        {
            Vector4D result = new Vector4D();
            for (int i = 0; i < 4; i++)
            {
                result[i] = v1[i] - v2[i];
            }
            return result;
        }

        public static Vector4D operator *(Vector4D v1, Vector4D v2)
        {
            Vector4D result = new Vector4D();
            for (int i = 0; i < 4; i++)
            {
                result[i] = v1[i] * v2[i];
            }
            return result;
        }

        public static Vector4D operator /(Vector4D v1, Vector4D v2)
        {
            Vector4D result = new Vector4D();
            for (int i = 0; i < 4; i++)
            {
                result[i] = v1[i] / v2[i];
            }
            return result;
        }

        public static double Dot(Vector4D v1, Vector4D v2)
        {
            double sum = 0;
            for (int i = 0; i < 4; i++)
            {
                sum += v1[i] * v2[i];
            }
            return sum;
        }

        public double Dot(Vector4D v)
        {
            double sum = 0;
            for (int i = 0; i < 4; i++)
            {
                sum += v[i] * this[i];
            }
            return sum;
        }

        public Vector4D Dot(Matrix4D m)
        {
            Vector4D result = new Vector4D();
            result[0] = this[0] * m[0] + this[1] * m[4] + this[2] * m[8] + this[3] * m[12];
            result[1] = this[0] * m[1] + this[1] * m[5] + this[2] * m[9] + this[3] * m[13];
            result[2] = this[0] * m[2] + this[1] * m[6] + this[2] * m[10] + this[3] * m[14];
            result[3] = this[0] * m[3] + this[1] * m[7] + this[2] * m[11] + this[3] * m[15];
            return result;
        }
    }
}
