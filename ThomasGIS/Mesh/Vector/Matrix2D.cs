using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Mesh.Vector
{
    public class Matrix2D : List<double>
    {
        public Matrix2D() : base()
        {
            for (int i = 0; i < 4; i++)
            {
                this.Add(0);
            }
        }

        public Matrix2D(double a, double b, double c, double d)
        {
            this.Add(a);
            this.Add(b);
            this.Add(c);
            this.Add(d);
        }

        public Matrix2D(params double[] values)
        {
            for (int i = 0; i < values.Length; i++)
            {
                this.Add(values[i]);
            }

            while (this.Count < 4)
            {
                this.Add(0);
            }
        }

        public static Matrix2D Zero => new Matrix2D();

        public int Rows => 2;

        public int Cols => 2;

        public static Matrix2D Rotate2D(double radian)
        {
            Matrix2D result = new Matrix2D();
            result[0] = Math.Cos(radian);
            result[1] = Math.Sin(radian);
            result[2] = -Math.Sin(radian);
            result[3] = Math.Cos(radian);
            return result;
        }

        public Vector2D Dot(Vector2D vector)
        {
            Vector2D result = new Vector2D();
            for (int i = 0; i < Rows; i++)
            {
                double sum = 0;
                for (int j = 0; j < Cols; j++)
                {
                    int mIndex = i * Rows + j;
                    sum += this[mIndex] * vector[j];
                }
                result[i] = sum;
            }
            return result;
        }

        public Matrix2D Dot(Matrix2D other)
        {
            Matrix2D result = new Matrix2D();
            for (int i = 0; i < Rows; i++)
            {
                for (int j = 0; j < Cols; j++)
                {
                    double sum = 0;
                    for (int k = 0; k < Rows; k++)
                    {
                        int m1Index = i * Rows + k;
                        int m2Index = k * Rows + j;
                        sum += this[m1Index] * other[m2Index];
                    }
                    result[i * Rows + j] = sum;
                }
            }
            return result;
        }

        public static Vector2D operator *(Matrix2D m, Vector2D v)
        {
            return m.Dot(v);
        }

        public static Matrix2D operator *(Matrix2D m1, Matrix2D m2)
        {
            return m1.Dot(m2);
        }
    }
}
