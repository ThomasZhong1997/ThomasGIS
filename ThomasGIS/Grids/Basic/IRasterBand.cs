using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Grids.Basic
{
    public interface IRasterBand
    {
        bool WriteData(double[,] inputData);

        bool ReadData(out double[,] outputData);

        double At(int row, int col);

        int GetRows();

        int GetCols();
    }
}
