using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;

namespace ThomasGIS.SpatialAnalysis
{
    public static class BufferProcessor
    {
        public static IShapefile CreateBuffer_Self(IShapefile inputShapefile, double radius, int eachEdgePointNumber = 30)
        {
            int type = inputShapefile.GetFeatureType();

            switch (type)
            {
                case 1:
                    return CreatePointBuffer(inputShapefile, radius, eachEdgePointNumber);
                case 3:

                    break;
                case 5:

                    break;
                case 11:

                    break;
                case 13:

                    break;
                case 15:

                    break;
                default:

                    break;
            }

            return null;
        }

        public static IEnumerable<IPoint> PointToCircle(IPoint point, double radius, int eachEdgePointNumber = 10, CoordinateType coordinateType = CoordinateType.Projected)
        {
            double realYRadius;
            double realXRadius;
            if (coordinateType == CoordinateType.Geographic)
            {
                double meterPerDegreeX = 40075020 / 360.0;
                double meterPerDegreeY = 40009000 / 360.0;
                realYRadius = radius / meterPerDegreeY;
                realXRadius = radius / (meterPerDegreeX * Math.Cos(point.GetY() / 180.0 * Math.PI));
            }
            else
            {
                realYRadius = radius;
                realXRadius = radius;
            }

            // X 算到一半的高度，左上 X 一半 Y/2 的坐标
            double halfY_X = realXRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(point.GetY() + realYRadius * 0.5 - point.GetY(), 2) / realYRadius / realYRadius)) + point.GetX();
            double dx = (halfY_X - point.GetX()) / eachEdgePointNumber;
            double dy = (realYRadius * 0.5) / eachEdgePointNumber;

            List<IPoint> pointList = new List<IPoint>();

            // 0-45 x主导
            for (int j = 0; j < eachEdgePointNumber; j++)
            {
                double nx = point.GetX() + j * dx;
                double ny = realYRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(nx - point.GetX(), 2) / realXRadius / realXRadius)) + point.GetY();
                pointList.Add(new Point(nx, ny));
            }

            // 45-90 y主导
            for (int j = eachEdgePointNumber; j > 0; j--)
            {
                double ny = point.GetY() + j * dy;
                double nx = realXRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(ny - point.GetY(), 2) / realYRadius / realYRadius)) + point.GetX();
                pointList.Add(new Point(nx, ny));
            }

            // 90-135 y主导
            for (int j = 0; j < eachEdgePointNumber; j++)
            {
                double ny = point.GetY() - j * dy;
                double nx = realXRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(ny - point.GetY(), 2) / realYRadius / realYRadius)) + point.GetX();
                pointList.Add(new Point(nx, ny));
            }

            // 135-180 x主导
            for (int j = eachEdgePointNumber; j > 0; j--)
            {
                double nx = point.GetX() + j * dx;
                double ny = -realYRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(nx - point.GetX(), 2) / realXRadius / realXRadius)) + point.GetY();
                pointList.Add(new Point(nx, ny));
            }

            // 180-225 x主导
            for (int j = 0; j < eachEdgePointNumber; j++)
            {
                double nx = point.GetX() - j * dx;
                double ny = -realYRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(nx - point.GetX(), 2) / realXRadius / realXRadius)) + point.GetY();
                pointList.Add(new Point(nx, ny));
            }

            // 225-270 y主导
            for (int j = eachEdgePointNumber; j > 0; j--)
            {
                double ny = point.GetY() - j * dy;
                double nx = -realXRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(ny - point.GetY(), 2) / realYRadius / realYRadius)) + point.GetX();
                pointList.Add(new Point(nx, ny));
            }

            // 270-315 y主导
            for (int j = 0; j < eachEdgePointNumber; j++)
            {
                double ny = point.GetY() + j * dy;
                double nx = -realXRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(ny - point.GetY(), 2) / realYRadius / realYRadius)) + point.GetX();
                pointList.Add(new Point(nx, ny));
            }

            // 315-360 x主导
            for (int j = eachEdgePointNumber; j >= 0; j--)
            {
                double nx = point.GetX() - j * dx;
                double ny = realYRadius * Math.Sqrt(Math.Abs(1 - Math.Pow(nx - point.GetX(), 2) / realXRadius / realXRadius)) + point.GetY();
                pointList.Add(new Point(nx, ny));
            }

            return pointList;
        }

        private static IShapefile CreatePointBuffer(IShapefile inputShapefile, double radius, int eachEdgePointNumber = 10)
        {
            IShapefile bufferShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);
            bufferShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());

            for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
            {
                ShpPoint shpPoint = inputShapefile.GetFeature(i) as ShpPoint;
                ShpPolygon newShpPolygon = new ShpPolygon();
                IEnumerable<IPoint> pointList = PointToCircle(shpPoint, radius, eachEdgePointNumber, inputShapefile.GetCoordinateRef().GetCoordinateType());
                newShpPolygon.AddPart(pointList);
                bufferShapefile.AddFeature(newShpPolygon);
            }

            return bufferShapefile;
        }

        private static IShapefile CreatePolylineBuffer(IShapefile inputShapefile, double radius, int eachEdgePointNumber = 10)
        {
            IShapefile bufferShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);
            bufferShapefile.SetCoordinateRef(inputShapefile.GetCoordinateRef());
            CoordinateType coordinateType = inputShapefile.GetCoordinateRef().GetCoordinateType();

            for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
            {
                ShpPolyline polyline = inputShapefile.GetFeature(i) as ShpPolyline;

                for (int j = 0; j < polyline.PartNumber; j++)
                {
                    List<IPoint> pointList = polyline.GetPart(j).ToList();
                    // 线的顶端是一个半圆
                    IPoint firstPoint = pointList[0];
                    IPoint secondPoint = pointList[1];

                    // 方向角
                    double angle;
                    if (coordinateType == CoordinateType.Geographic)
                    {
                        angle = DistanceCalculator.DirectionAngleGeo(firstPoint, secondPoint);
                    }
                    else
                    {
                        angle = DistanceCalculator.DirectionAngle(firstPoint, secondPoint);
                    }

                    // 垂直于方向角画半圆，换个思路，线和面都是把圆和长方形做Union，所以先写Union算法
                    
                }
            }

            return bufferShapefile;
        }

        public static IShapefile CreateBuffer_GEOS(IShapefile inputShapefile, double radius)
        {
            IShapefile bufferShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);
            bufferShapefile.CopyFieldInformation(inputShapefile);
            List<string> bufferFieldNames = bufferShapefile.GetFieldNames().ToList();

            for (int i = 0; i < inputShapefile.GetFeatureNumber(); i++)
            {
                IShpGeometryBase bufferGeometry = TopoCalculator.Buffer(inputShapefile.GetFeature(i), radius, inputShapefile.GetCoordinateRef()) as IShpGeometryBase;
                bufferShapefile.AddFeature(bufferGeometry);
                foreach (string fieldName in bufferFieldNames)
                {
                    byte[] fieldValueBuffer = inputShapefile.GetFieldValueAsByte(i, fieldName);
                    bufferShapefile.SetValue2(i, fieldName, fieldValueBuffer);
                }
            }

            return bufferShapefile;
        }
    }
}
