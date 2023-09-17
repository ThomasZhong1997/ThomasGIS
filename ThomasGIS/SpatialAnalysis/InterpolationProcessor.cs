using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.DataStructure;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Grids.Basic;
using ThomasGIS.Helpers;
using ThomasGIS.SpatialIndex;
using ThomasGIS.Vector;

namespace ThomasGIS.SpatialAnalysis
{
    public class IndexElement : IComparable
    {
        public int Index;
        public double Value;

        public int CompareTo(object element)
        {
            return Value.CompareTo(((IndexElement)element).Value);
        }

        public IndexElement(int index, double value)
        {
            this.Index = index;
            this.Value = value;
        }
    }

    public static class InterpolationProcessor
    {
        // 参数：shapefile输入的shapefile对象，fieldName属性字段名，power反距离权重，searchRange搜索半径，maxCount最大邻接数量，boundaryBox栅格范围，scale栅格分辨率
        public static Raster IDW(IShapefile shapefile, string fieldName, double power = 2, double searchRange = -1, int maxCount = -1, BoundaryBox boundaryBox = null, double scale = -1, bool useSpatialIndex = true)
        {
            // 仅支持对点操作
            if (shapefile.GetFeatureType() != 1) return null;

            if (shapefile == null || fieldName == "") return null;

            if (boundaryBox == null)
            {
                boundaryBox = shapefile.GetBoundaryBox();
            }

            if (scale == -1)
            {
                scale = Math.Max(boundaryBox.XMax - boundaryBox.XMin, boundaryBox.YMax - boundaryBox.YMin) / 1000;
            }

            if (maxCount == -1)
            {
                maxCount = Math.Max(shapefile.GetFeatureNumber() / 3, 2);
            }

            int matrixCols = (int)((boundaryBox.XMax - boundaryBox.XMin) / scale + 1);
            int matrixRows = (int)((boundaryBox.YMax - boundaryBox.YMin) / scale + 1);

            Raster raster = new Raster(matrixRows, matrixCols, RasterDataType.DOUBLE, shapefile.GetCoordinateRef());
            raster.SetGeoTransform(boundaryBox.XMin, boundaryBox.YMax, scale, scale);

            RasterBand newRasterBand = new RasterBand(matrixRows, matrixCols);
            double[,] interpolationDataMatrix = new double[matrixRows, matrixCols];

            ISpatialIndex spatialIndex = null;

            if (useSpatialIndex)
            {
                spatialIndex = new GridSpatialIndex(boundaryBox, scale * 111000.0, shapefile.GetCoordinateRef());

                for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
                {
                    spatialIndex.AddItem(shapefile.GetFeature(i));
                }
                spatialIndex.RefreshIndex();
            }

            Parallel.For(0, matrixRows, i =>
            {
                Parallel.For(0, matrixCols, j =>
                {
                    double gridCenterX = boundaryBox.XMin + j * scale + 0.5 * scale;
                    double gridCenterY = boundaryBox.YMin + i * scale + 0.5 * scale;

                    PriorityQueue<IndexElement> distanceQueue = new PriorityQueue<IndexElement>(true);
                    Point centerPoint = new Point(gridCenterX, gridCenterY);

                    List<int> neighborPoints = new List<int>();

                    if (searchRange == -1 || useSpatialIndex == false)
                    {
                        for (int k = 0; k < shapefile.GetFeatureNumber(); k++)
                        {
                            neighborPoints.Add(k);
                        }
                    }
                    else
                    {
                        neighborPoints.AddRange(spatialIndex.SearchID(centerPoint, searchRange));
                    }

                    for (int k = 0; k < neighborPoints.Count; k++)
                    {
                        double distance;
                        if (shapefile.GetCoordinateRef() != null && shapefile.GetCoordinateRef().GetCoordinateType() == Coordinates.CoordinateType.Geographic)
                        {
                            distance = DistanceCalculator.SpatialDistanceGeo(centerPoint, shapefile.GetFeature(k) as ShpPoint);
                        }
                        else
                        {
                            distance = DistanceCalculator.SpatialDistance(centerPoint, shapefile.GetFeature(k) as ShpPoint);
                        }

                        distanceQueue.Add(new IndexElement(k, distance));
                    }

                    int minCount = Math.Min(distanceQueue.Count, maxCount);

                    List<IndexElement> elementList = new List<IndexElement>();
                    for (int k = 0; k < minCount; k++)
                    {
                        elementList.Add(distanceQueue.Pop());
                    }

                    double sumDistance = 0;
                    for (int k = 0; k < elementList.Count; k++)
                    {
                        double distance = elementList[k].Value;
                        if (distance < 1) distance = 1;
                        if (searchRange > 0 && distance > searchRange) break;
                        sumDistance += (1 / Math.Pow(distance, power));
                    }

                    double pixelValue = 0;
                    for (int k = 0; k < elementList.Count; k++)
                    {
                        IndexElement element = elementList[k];
                        int targetIndex = element.Index;
                        double distance = element.Value;
                        if (distance < 1) distance = 1;
                        if (searchRange > 0 && distance > searchRange) break;
                        double percent = (1.0 / Math.Pow(distance, power)) / sumDistance;
                        pixelValue += shapefile.GetFieldValueAsDouble(targetIndex, fieldName) * percent;
                    }

                    interpolationDataMatrix[matrixRows - 1 - i, j] = pixelValue;
                });
            });

            newRasterBand.WriteData(interpolationDataMatrix);
            raster.AddRasterBand(newRasterBand);
            return raster;
        }

        public static RasterBand Kriging(IShapefile shapefile, string fieldName, BoundaryBox boundaryBox, double scale)
        {
            return null;
        }
    }
}
