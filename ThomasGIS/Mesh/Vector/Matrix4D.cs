using MathNet.Numerics.LinearAlgebra;
using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Mesh.Vector
{
    public class Matrix4D : List<double>
    {
        public Matrix4D()
        {
            for (int i = 0; i < 16; i++)
            {
                this.Add(0);
            }
        }

        public static Matrix4D ZeroMatrix => new Matrix4D();

        public static Matrix4D operator+ (Matrix4D mat1, Matrix4D mat2)
        {
            Matrix4D result = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 16; i++)
            {
                result[i] = mat1[i] + mat2[i];
            }
            return result;
        }

        public static Matrix4D operator- (Matrix4D mat1, Matrix4D mat2)
        {
            Matrix4D result = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 16; i++)
            {
                result[i] = mat1[i] - mat2[i];
            }
            return result;
        }

        public static Matrix4D operator* (Matrix4D mat1, Matrix4D mat2)
        {
            Matrix4D result = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 16; i++)
            {
                result[i] = mat1[i] * mat2[i];
            }
            return result;
        }

        public static Matrix4D operator/ (Matrix4D mat1, Matrix4D mat2)
        {
            Matrix4D result = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 16; i++)
            {
                result[i] = mat1[i] / mat2[i];
            }
            return result;
        }

        public static Matrix4D Dot(Matrix4D mat1, Matrix4D mat2)
        {
            Matrix4D result = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 16; i++)
            {
                int row = i / 4;
                int col = i % 4;
                result[i] = mat1[row] * mat2[0 * 4 + col] + mat1[row + 1] * mat2[1 * 4 + col] + mat1[row + 2] * mat2[2 * 4 + col] + mat1[row + 3] * mat2[3 * 4 + col];
            }
            return result;
        }

        public double SubDeterminate(int a1, int a2, int a3, int b1, int b2, int b3, int c1, int c2, int c3)
        {
            double a11 = this[a1];
            double a12 = this[a2];
            double a13 = this[a3];
            double a21 = this[b1];
            double a22 = this[b2];
            double a23 = this[b3];
            double a31 = this[c1];
            double a32 = this[c2];
            double a33 = this[c3];

            return a11 * a22 * a33 + a12 * a23 * a31 + a13 * a21 * a32 - a13 * a22 * a31 - a12 * a21 * a33 - a11 * a23 * a32;
        }

        public Vector4D Dot(Vector4D v)
        {
            Vector4D result = new Vector4D();
            result[0] = this[0] * v[0] + this[1] * v[1] + this[2] * v[2] + this[3] * v[3];
            result[1] = this[4] * v[0] + this[5] * v[1] + this[6] * v[2] + this[7] * v[3];
            result[2] = this[8] * v[0] + this[9] * v[1] + this[10] * v[2] + this[11] * v[3];
            result[3] = this[12] * v[0] + this[13] * v[1] + this[14] * v[2] + this[15] * v[3];
            return result;
        }

        public double Det()
        {
            double sum = this[0] * this.SubDeterminate(5, 6, 7, 9, 10, 11, 13, 14, 15) 
                - this[1] * this.SubDeterminate(4, 6, 7, 8, 10, 11, 12, 14, 15) 
                + this[2] * this.SubDeterminate(4, 5, 7, 8, 9, 11, 12, 13, 15) 
                - this[3] * this.SubDeterminate(4, 5, 6, 8, 9, 10, 12, 13, 14);
            return sum;
        }

        public Matrix4D Adjugate()
        {
            double det = this.Det();
            if (det == 0) throw new Exception("Matrix don't have Adjugate Matrix!");

            Matrix4D result = Matrix4D.ZeroMatrix;
            result[0] = this.SubDeterminate(5, 6, 7, 9, 10, 11, 13, 14, 15);
            result[4] = -this.SubDeterminate(4, 6, 7, 8, 10, 11, 12, 14, 15);
            result[8] = this.SubDeterminate(4, 5, 7, 8, 9, 11, 12, 13, 15);
            result[12] = -this.SubDeterminate(4, 5, 6, 8, 9, 10, 12, 13, 14);
            result[1] = -this.SubDeterminate(1, 2, 3, 9, 10, 11, 13, 14, 15);
            result[5] = this.SubDeterminate(0, 2, 3, 8, 10, 11, 12, 14, 15);
            result[9] = -this.SubDeterminate(0, 1, 3, 8, 9, 11, 12, 13, 15);
            result[13] = this.SubDeterminate(0, 1, 2, 8, 9, 10, 12, 13, 14);
            result[2] = this.SubDeterminate(1, 2, 3, 5, 6, 7, 13, 14, 15);
            result[6] = -this.SubDeterminate(0, 2, 3, 4, 6, 7, 12, 14, 15);
            result[10] = this.SubDeterminate(0, 1, 3, 4, 5, 7, 12, 13, 15);
            result[14] = -this.SubDeterminate(0, 1, 2, 4, 5, 6, 12, 13, 14);
            result[3] = -this.SubDeterminate(1, 2, 3, 5, 6, 7, 9, 10, 11);
            result[7] = this.SubDeterminate(0, 2, 3, 4, 6, 7, 8, 10, 11);
            result[11] = -this.SubDeterminate(0, 1, 3, 4, 5, 7, 8, 9, 11);
            result[15] = this.SubDeterminate(0, 1, 2, 4, 5, 6, 8, 9, 10);

            return result;
        }

        public Matrix4D Inv()
        {
            Matrix4D result = Adjugate();
            double det = this.Det();
            for (int i = 0; i < 16; i++)
            {
                result[i] /= det;
            }
            return result;
        }
    }
}
