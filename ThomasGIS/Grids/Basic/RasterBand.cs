using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Grids.Basic
{
    public class RasterBand : IRasterBand
    {
        public int Rows { get; } = 0;
        public int Cols { get; } = 0;

        private double[,] innerData = null;

        public RasterBand(int rows, int cols)
        {
            this.Rows = rows;
            this.Cols = cols;
            this.innerData = new double[rows, cols];
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(RasterBand)) return false;

            RasterBand otherRasterBand = obj as RasterBand;

            if (this.Rows != otherRasterBand.Rows || this.Cols != otherRasterBand.Cols) return false;

            for (int i = 0; i < this.Rows; i++)
            {
                for (int j = 0; j < this.Cols; j++)
                {
                    if (this.At(i, j) != otherRasterBand.At(i, j)) return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public bool WriteData(double[,] inputData)
        {
            for (int i = 0; i < inputData.GetLength(0); i++)
            {
                for (int j = 0; j < inputData.GetLength(1); j++)
                {
                    innerData[i, j] = inputData[i, j];
                }
            }
            return true;
        }

        public bool ReadData(out double[,] outputData)
        {
            outputData = new double[Rows, Cols];

            for (int i = 0; i < innerData.GetLength(0); i++)
            {
                for (int j = 0; j < innerData.GetLength(1); j++)
                {
                    outputData[i, j] = innerData[i, j];
                }
            }
            return true;
        }

        // 运算符重载
        public static RasterBand operator+ (RasterBand band1, RasterBand band2)
        {
            if (band1 != band2) throw new Exception("适用于两个波段数据间的数值运算必须保证两个波段具有相同的行数和列数！");

            double[,] leftData = null;
            double[,] rightData = null;

            band1.ReadData(out leftData);
            band2.ReadData(out rightData);

            for (int i = 0; i < band1.Rows; i++)
            {
                for (int j = 0; j < band1.Cols; j++)
                {
                    leftData[i, j] += rightData[i, j];
                }
            }

            RasterBand result = new RasterBand(band1.Rows, band1.Cols);
            result.WriteData(leftData);

            return result;
        }

        public static RasterBand operator- (RasterBand band1, RasterBand band2)
        {
            if (band1 != band2) throw new Exception("适用于两个波段数据间的数值运算必须保证两个波段具有相同的行数和列数！");

            double[,] leftData = null;
            double[,] rightData = null;

            band1.ReadData(out leftData);
            band2.ReadData(out rightData);

            for (int i = 0; i < band1.Rows; i++)
            {
                for (int j = 0; j < band1.Cols; j++)
                {
                    leftData[i, j] -= rightData[i, j];
                }
            }

            RasterBand result = new RasterBand(band1.Rows, band1.Cols);
            result.WriteData(leftData);

            return result;
        }

        public static bool operator== (RasterBand band1, RasterBand band2)
        {
            if (band1.Rows == band2.Rows && band1.Cols == band2.Cols) return true;
            return false;
        }

        public static bool operator !=(RasterBand band1, RasterBand band2)
        {
            if (band1.Rows == band2.Rows && band1.Cols == band2.Cols) return false;
            return true;
        }

        public static RasterBand operator* (RasterBand band1, RasterBand band2)
        {
            if (band1 != band2) throw new Exception("适用于两个波段数据间的数值运算必须保证两个波段具有相同的行数和列数！");

            double[,] leftData = null;
            double[,] rightData = null;

            band1.ReadData(out leftData);
            band2.ReadData(out rightData);

            for (int i = 0; i < band1.Rows; i++)
            {
                for (int j = 0; j < band1.Cols; j++)
                {
                    leftData[i, j] *= rightData[i, j];
                }
            }

            RasterBand result = new RasterBand(band1.Rows, band1.Cols);
            result.WriteData(leftData);

            return result;
        }

        public static RasterBand operator/ (RasterBand band1, RasterBand band2)
        {
            if (band1 != band2) throw new Exception("适用于两个波段数据间的数值运算必须保证两个波段具有相同的行数和列数！");

            double[,] leftData = null;
            double[,] rightData = null;

            band1.ReadData(out leftData);
            band2.ReadData(out rightData);

            for (int i = 0; i < band1.Rows; i++)
            {
                for (int j = 0; j < band1.Cols; j++)
                {
                    if (rightData[i, j] == 0)
                    {
                        leftData[i, j] = Double.MaxValue;
                    }
                    else
                    {
                        leftData[i, j] /= rightData[i, j];
                    }
                }
            }

            RasterBand result = new RasterBand(band1.Rows, band1.Cols);
            result.WriteData(leftData);

            return result;
        }

        public double At(int row, int col)
        {
            if (row < 0 || row >= this.Rows || col < 0 || col >= this.Cols)
            {
                return 0;
            }

            return this.innerData[row, col];
        }

        public int GetRows()
        {
            return this.Rows;
        }

        public int GetCols()
        {
            return this.Cols;
        }
    }
}
