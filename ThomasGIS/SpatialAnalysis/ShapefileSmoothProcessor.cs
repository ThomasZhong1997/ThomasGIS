using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;

namespace ThomasGIS.SpatialAnalysis
{
    public static class ShapefileSmoothProcessor
    {
        public static bool _3DShapefileHeightSmooth_TwoSum(IShapefile shapefile)
        {
            if (shapefile.GetFeatureType() != 13 && shapefile.GetFeatureType() == 15)
            {
                return false;
            }

            if (shapefile.GetFeatureType() == 13)
            {
                for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline3D polyline3D = shapefile.GetFeature(i) as ShpPolyline3D;
                    if (polyline3D.PointNumber == 0)
                    {
                        shapefile.RemoveFeature(i);
                        i--;
                        continue;
                    }

                    for (int j = 0; j < polyline3D.PartNumber; j++)
                    {
                        int startIndex = polyline3D.PartList[j];
                        int endIndex = -1;
                        if (j == polyline3D.PartNumber - 1)
                        {
                            endIndex = polyline3D.PointNumber;
                        }
                        else
                        {
                            endIndex = polyline3D.PartList[j + 1];
                        }

                        List<double> cloneZList = new List<double>(polyline3D.ZList);
                        for (int k = startIndex + 1; k < endIndex - 1; k++)
                        {
                            double distance1 = -1;
                            double distance2 = -1;
                            if (shapefile.GetCoordinateRef().GetCoordinateType() == Coordinates.CoordinateType.Geographic)
                            {
                                distance1 = DistanceCalculator.SpatialDistanceGeo(polyline3D.GetPointByIndex(k - 1), polyline3D.GetPointByIndex(k));
                                distance2 = DistanceCalculator.SpatialDistanceGeo(polyline3D.GetPointByIndex(k), polyline3D.GetPointByIndex(k + 1));
                            }
                            else
                            {
                                distance1 = DistanceCalculator.SpatialDistance(polyline3D.GetPointByIndex(k - 1), polyline3D.GetPointByIndex(k));
                                distance2 = DistanceCalculator.SpatialDistance(polyline3D.GetPointByIndex(k), polyline3D.GetPointByIndex(k + 1));
                            }
                            double p1 = distance1 / (distance1 + distance2);
                            cloneZList[k] = polyline3D.ZList[k - 1] * (1 - p1) + polyline3D.ZList[k + 1] * p1;
                        }

                        polyline3D.ZList = cloneZList;
                    }
                }
            }
            else
            {
                throw new Exception("Not supported!");
            }

            return true;
        }

        public static bool _3DShapefileHeightSmooth_Avg(IShapefile shapefile)
        {
            if (shapefile.GetFeatureType() != 13 && shapefile.GetFeatureType() == 15)
            {
                return false;
            }

            if (shapefile.GetFeatureType() == 13)
            {
                for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline3D polyline3D = shapefile.GetFeature(i) as ShpPolyline3D;
                    if (polyline3D.PointNumber == 0)
                    {
                        shapefile.RemoveFeature(i);
                        i--;
                        continue;
                    }

                    for (int j = 0; j < polyline3D.PartNumber; j++)
                    {
                        int startIndex = polyline3D.PartList[j];
                        int endIndex = -1;
                        if (j == polyline3D.PartNumber - 1)
                        {
                            endIndex = polyline3D.PointNumber;
                        }
                        else
                        {
                            endIndex = polyline3D.PartList[j + 1];
                        }

                        List<double> cloneZList = new List<double>(polyline3D.ZList);
                        for (int k = startIndex + 1; k < endIndex - 1; k++)
                        {
                            double distance1 = -1;
                            double distance2 = -1;
                            if (shapefile.GetCoordinateRef().GetCoordinateType() == Coordinates.CoordinateType.Geographic)
                            {
                                distance1 = DistanceCalculator.SpatialDistanceGeo(polyline3D.GetPointByIndex(k - 1), polyline3D.GetPointByIndex(k));
                                distance2 = DistanceCalculator.SpatialDistanceGeo(polyline3D.GetPointByIndex(k), polyline3D.GetPointByIndex(k + 1));
                            }
                            else
                            {
                                distance1 = DistanceCalculator.SpatialDistance(polyline3D.GetPointByIndex(k - 1), polyline3D.GetPointByIndex(k));
                                distance2 = DistanceCalculator.SpatialDistance(polyline3D.GetPointByIndex(k), polyline3D.GetPointByIndex(k + 1));
                            }
                            double p1 = distance1 / (distance1 + distance2);
                            cloneZList[k] = ((polyline3D.ZList[k - 1] * (1 - p1) + polyline3D.ZList[k + 1] * p1) + polyline3D.ZList[k]) / 2.0;
                        }

                        polyline3D.ZList = cloneZList;
                    }
                }
            }
            else
            {
                throw new Exception("Not supported!");
            }

            return true;
        }
    }
}
