using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Vector
{
    public static class ShapefileProcessor
    {
        /// <summary>
        /// 多部件至单部件，DeepCopy
        /// </summary>
        /// <param name="inputShapefile"></param>
        /// <returns></returns>
        public static IShapefile MultipartsToSinglePart(IShapefile inputShapefile)
        {
            List<string> fieldNames = inputShapefile.GetFieldNames().ToList();
            if (inputShapefile.GetFeatureType() == 1)
            {
                IShapefile pointShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Point);
                pointShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                pointShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    IPoint nowPoint = inputShapefile.GetFeature(i) as Point;
                    ShpPoint newPoint = new ShpPoint(nowPoint);
                    pointShapefile.AddFeature(newPoint);
                    foreach (string fieldName in fieldNames)
                    {
                        pointShapefile.SetValue2(pointShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                    }
                }
                return pointShapefile;
            }
            else if (inputShapefile.GetFeatureType() == 3)
            {
                IShapefile polylineShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);
                polylineShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                polylineShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline nowPolyline = inputShapefile.GetFeature(i) as ShpPolyline;
                    for (int j = 0; j < nowPolyline.PartNumber; j++)
                    {
                        ShpPolyline newPolyline = new ShpPolyline();
                        int startIndex = nowPolyline.PartList[j];
                        int endIndex;
                        if (j == nowPolyline.PartNumber - 1)
                        {
                            endIndex = nowPolyline.PointNumber;
                        }
                        else
                        {
                            endIndex = nowPolyline.PartList[j + 1];
                        }

                        List<IPoint> newPointList = new List<IPoint>();
                        for (int k = startIndex; k < endIndex; k++)
                        {
                            newPointList.Add(new Point(nowPolyline.PointList[k]));
                        }
                        newPolyline.AddPart(newPointList);
                        polylineShapefile.AddFeature(newPolyline);
                        foreach (string fieldName in fieldNames)
                        {
                            polylineShapefile.SetValue2(polylineShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                        }
                    }
                }
                return polylineShapefile;
            }
            else if (inputShapefile.GetFeatureType() == 5)
            {
                IShapefile polygonShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);
                polygonShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                polygonShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    ShpPolygon nowPolygon = inputShapefile.GetFeature(i) as ShpPolygon;
                    for (int j = 0; j < nowPolygon.PartNumber; j++)
                    {
                        ShpPolygon newPolygon = new ShpPolygon();
                        int startIndex = nowPolygon.PartList[j];
                        int endIndex;
                        if (j == nowPolygon.PartNumber - 1)
                        {
                            endIndex = nowPolygon.PointNumber;
                        }
                        else
                        {
                            endIndex = nowPolygon.PartList[j + 1];
                        }

                        List<IPoint> newPointList = new List<IPoint>();
                        for (int k = startIndex; k < endIndex; k++)
                        {
                            newPointList.Add(new Point(nowPolygon.PointList[k]));
                        }
                        newPolygon.AddPart(newPointList);
                        polygonShapefile.AddFeature(newPolygon);
                        foreach (string fieldName in fieldNames)
                        {
                            polygonShapefile.SetValue2(polygonShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                        }
                    }
                }
                return polygonShapefile;
            }
            else if (inputShapefile.GetFeatureType() == 11)
            {
                IShapefile point3DShapefile = VectorFactory.CreateShapefile(ESRIShapeType.PointZ);
                point3DShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                point3DShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    ShpPoint3D nowPoint = inputShapefile.GetFeature(i) as ShpPoint3D;
                    ShpPoint3D newPoint = new ShpPoint3D(nowPoint.GetX(), nowPoint.GetY(), nowPoint.Z, nowPoint.M);
                    point3DShapefile.AddFeature(newPoint);
                    foreach (string fieldName in fieldNames)
                    {
                        point3DShapefile.SetValue2(point3DShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                    }
                }
                return point3DShapefile;
            }
            else if (inputShapefile.GetFeatureType() == 13)
            {
                IShapefile polyline3DShapefile = VectorFactory.CreateShapefile(ESRIShapeType.PolylineZ);
                polyline3DShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                polyline3DShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    ShpPolyline3D nowPolyline3D = inputShapefile.GetFeature(i) as ShpPolyline3D;
                    for (int j = 0; j < nowPolyline3D.PartNumber; j++)
                    {
                        ShpPolyline3D newPolyline3D = new ShpPolyline3D();
                        int startIndex = nowPolyline3D.PartList[j];
                        int endIndex;
                        if (j == nowPolyline3D.PartNumber - 1)
                        {
                            endIndex = nowPolyline3D.PointNumber;
                        }
                        else
                        {
                            endIndex = nowPolyline3D.PartList[j + 1];
                        }

                        List<IPoint> newPointList = new List<IPoint>();
                        for (int k = startIndex; k < endIndex; k++)
                        {
                            newPointList.Add(new Point(nowPolyline3D.PointList[k]));
                            newPolyline3D.ZList.Add(nowPolyline3D.ZList[k]);
                            newPolyline3D.MList.Add(nowPolyline3D.MList[k]);
                        }
                        newPolyline3D.AddPart(newPointList);
                        polyline3DShapefile.AddFeature(newPolyline3D);
                        foreach (string fieldName in fieldNames)
                        {
                            polyline3DShapefile.SetValue2(polyline3DShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                        }
                    }
                }
                return polyline3DShapefile;
            }
            else if (inputShapefile.GetFeatureType() == 15)
            {
                IShapefile polygon3DShapefile = VectorFactory.CreateShapefile(ESRIShapeType.PolygonZ);
                polygon3DShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
                polygon3DShapefile.CopyFieldInformation(inputShapefile);
                for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
                {
                    ShpPolygon3D nowPolygon3D = inputShapefile.GetFeature(i) as ShpPolygon3D;
                    for (int j = 0; j < nowPolygon3D.PartNumber; j++)
                    {
                        ShpPolygon3D newPolygon3D = new ShpPolygon3D();
                        int startIndex = nowPolygon3D.PartList[j];
                        int endIndex;
                        if (j == nowPolygon3D.PartNumber - 1)
                        {
                            endIndex = nowPolygon3D.PointNumber;
                        }
                        else
                        {
                            endIndex = nowPolygon3D.PartList[j + 1];
                        }

                        List<IPoint> newPointList = new List<IPoint>();
                        for (int k = startIndex; k < endIndex; k++)
                        {
                            newPointList.Add(new Point(nowPolygon3D.PointList[k]));
                            newPolygon3D.ZList.Add(nowPolygon3D.ZList[k]);
                            newPolygon3D.MList.Add(nowPolygon3D.MList[k]);
                        }
                        newPolygon3D.AddPart(newPointList);
                        polygon3DShapefile.AddFeature(newPolygon3D);
                        foreach (string fieldName in fieldNames)
                        {
                            polygon3DShapefile.SetValue2(polygon3DShapefile.GetFeatureNumber() - 1, fieldName, inputShapefile.GetFieldValueAsByte(i, fieldName));
                        }
                    }
                }
                return polygon3DShapefile;
            }
            else
            {
                throw new Exception("Unsupported Shapefile Type!");
            }
        }

        /// <summary>
        /// 对较规则的Polygon3D进行重采样，条件是有且仅能检测到四个直角，对两个长边进行重采样
        /// </summary>
        public static ShpPolygon3D ResamplePolygon3D(ShpPolygon3D shpPolygon3D)
        {
            ShpPolygon3D newPolygon3D = new ShpPolygon3D();

            List<int> cornerIndexList = new List<int>();

            int uniquePointNumber = shpPolygon3D.PointNumber - 1;
            for (int i = 0; i < uniquePointNumber; i++)
            {
                int prevIndex = (i - 1 + uniquePointNumber) % uniquePointNumber;
                int nextIndex = (i + 1) % uniquePointNumber;
                IPoint prevPoint = shpPolygon3D.GetPointByIndex(prevIndex);
                IPoint nowPoint = shpPolygon3D.GetPointByIndex(i);
                IPoint nextPoint = shpPolygon3D.GetPointByIndex(nextIndex);

                Vector2D prevPointLoc = new Vector2D(prevPoint);
                Vector2D nowPointLoc = new Vector2D(nowPoint);
                Vector2D nextPointLoc = new Vector2D(nextPoint);

                Vector2D toPrevVector = prevPointLoc - nowPointLoc;
                Vector2D toNextVector = nextPointLoc - nowPointLoc;

                double dot = Vector2D.Dot(toPrevVector, toNextVector);
                double cosAngle = dot / (toPrevVector.Length() * toNextVector.Length());
                double angle = Math.Acos(cosAngle) / Math.PI * 180.0;

                if (angle > 70.0 && angle < 110.0)
                {
                    cornerIndexList.Add(i);
                }
            }

            if (cornerIndexList.Count != 4) return shpPolygon3D;

            List<double> distanceList = new List<double>();
            List<int> indexList = new List<int>();
            for (int i = 0; i < 4; i++)
            {
                int nowIndex = cornerIndexList[i];
                int nextIndex = cornerIndexList[(i + 1) % 4];
                
            }

            throw new NotImplementedException();
        }
    }
}
