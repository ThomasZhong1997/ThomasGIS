using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.Grids.Basic
{
    public interface IRaster
    {
        bool ExportToGTiff(string filepath);

        bool ExportToENVIStandard(string filepath, ENVIEncodingType type);

        bool AddRasterBand(IRasterBand rasterBand);

        bool RemoveRasterBand(int index);

        IRasterBand GetRasterBand(int index);

        int GetRows();

        int GetCols();

        int GetRasterBandNumber();

        CoordinateBase GetCoordinateSystem();

        bool SetCoordinateSystem(CoordinateBase coordinateSystem);

        bool SetGeoTransform(double xMin, double yMax, double xScale, double yScale);

        double[] GetGeoTransform();

        BoundaryBox GetBoundaryBox();
    }
}
