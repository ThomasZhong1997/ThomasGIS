using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Mesh.Vector;
using ThomasGIS.Vector;

namespace ThomasGIS.Helpers
{
    public enum BufferType
    {
        Left,
        Right,
        All
    }

    public static class TopoCalculator
    {
        // firstGeometry 是否在 secondGeometry 内
        // 基于 GEOS 库实现，类型太多
        public static bool IsContain(IGeometry firstGeometry, IGeometry secondGeometry)
        {
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(firstGeometry);
            iGeospatial.Geometries.Geometry geom2 = GeometryConverter.ConvertToGEOSGeometry(secondGeometry);
            return geom2.Contains(geom1);
        }

        public static bool IsIntersection(IGeometry firstGeometry, IGeometry secondGeometry)
        {
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(firstGeometry);
            iGeospatial.Geometries.Geometry geom2 = GeometryConverter.ConvertToGEOSGeometry(secondGeometry);
            return geom2.Intersects(geom1);
        }

        public static IEnumerable<IPoint> GrahamCreateBoundary(List<IPoint> pointList)
        {
            // 点集中的点数小于3，无法构成面，直接返回
            if (pointList.Count <= 2)
            {
                return new List<IPoint>();
            }

            Stack<IPoint> resultPointList = new Stack<IPoint>();

            // 找到左下方的点
            IPoint leftDownPoint = pointList[0];
            int leftDownPointIndex = 0;
            for (int i = 1; i < pointList.Count; i++)
            {
                IPoint nowPoint = pointList[i];
                if (nowPoint.GetY() < leftDownPoint.GetY())
                {
                    leftDownPoint = nowPoint;
                    leftDownPointIndex = i;
                }
                else
                {
                    if (nowPoint.GetY() == leftDownPoint.GetY() && nowPoint.GetX() < leftDownPoint.GetX())
                    {
                        leftDownPoint = nowPoint;
                        leftDownPointIndex = i;
                    }
                }
            }

            // 起始点入栈
            resultPointList.Push(leftDownPoint);

            // 角度数组与索引数组用于排序
            List<double> angleList = new List<double>();
            List<int> indexList = new List<int>();
            List<double> distanceList = new List<double>();

            for (int i = 0; i < pointList.Count; i++)
            {
                if (i == leftDownPointIndex) continue;
                IPoint nowPoint = pointList[i];

                if (nowPoint.GetX() == leftDownPoint.GetX())
                {
                    angleList.Add(90.0);
                }
                else
                {
                    double k = (nowPoint.GetY() - leftDownPoint.GetY()) / (nowPoint.GetX() - leftDownPoint.GetX());
                    double angle = Math.Atan(k);
                    angle = angle < 0 ? 180.0 + angle / Math.PI * 180.0 : angle / Math.PI * 180.0;
                    angleList.Add(angle);
                }

                distanceList.Add(DistanceCalculator.SpatialDistance(leftDownPoint, nowPoint));
                indexList.Add(i);
            }

            // 垃圾的冒泡排序
            for (int i = 0; i < angleList.Count; i++)
            {
                for (int j = i + 1; j < angleList.Count; j++)
                {
                    if (angleList[i] > angleList[j])
                    {
                        double tempAngle = angleList[i];
                        angleList[i] = angleList[j];
                        angleList[j] = tempAngle;

                        int tempIndex = indexList[i];
                        indexList[i] = indexList[j];
                        indexList[j] = tempIndex;

                        double tempDistance = distanceList[i];
                        distanceList[i] = distanceList[j];
                        distanceList[j] = tempDistance;
                    }
                    else
                    {
                        if (angleList[i] == angleList[j])
                        {
                            if (distanceList[i] > distanceList[j])
                            {
                                double tempAngle = angleList[i];
                                angleList[i] = angleList[j];
                                angleList[j] = tempAngle;

                                int tempIndex = indexList[i];
                                indexList[i] = indexList[j];
                                indexList[j] = tempIndex;

                                double tempDistance = distanceList[i];
                                distanceList[i] = distanceList[j];
                                distanceList[j] = tempDistance;
                            }
                        }
                    }
                }
            }

            // 前两个入栈
            resultPointList.Push(pointList[indexList[0]]);
            resultPointList.Push(pointList[indexList[1]]);

            for (int i = 2; i < angleList.Count; i++)
            {
                IPoint nextPoint = pointList[indexList[i]];
                IPoint middlePoint = resultPointList.Pop();
                IPoint prevPoint = resultPointList.Pop();

                double angle1 = DistanceCalculator.HorizonAngle(prevPoint, middlePoint);
                double angle2 = DistanceCalculator.HorizonAngle(middlePoint, nextPoint);

                if (angle2 <= angle1)
                {
                    resultPointList.Push(prevPoint);
                    i--;
                }
                else
                {
                    resultPointList.Push(prevPoint);
                    resultPointList.Push(middlePoint);
                    resultPointList.Push(nextPoint);
                }
            }

            resultPointList.Push(leftDownPoint);

            return resultPointList;
        }

        // 判断点在线的左边还是右边
        public static int PointSingleLinePosition(ISingleLine singleLine, IPoint point)
        {
            double s = (singleLine.GetStartPoint().GetX() - point.GetX()) * (singleLine.GetEndPoint().GetY() - point.GetY()) - (singleLine.GetStartPoint().GetY() - point.GetY()) * (singleLine.GetEndPoint().GetX() - point.GetX());

            if (s == 0) return 0;

            if (s > 0)
            {
                return 1;
            }

            return -1;
        }

        // 判断两个线段是否相交，求交点后判断
        public static bool IsCross(IPoint line1StartPoint, IPoint line1EndPoint, IPoint line2StartPoint, IPoint line2EndPoint)
        {
            // 求第一条线的斜率
            // 如果第一条线是个竖线，没斜率
            if (line1StartPoint.GetX() == line1EndPoint.GetX())
            {
                // 并且第二条线也是竖线，平行
                if (line2StartPoint.GetX() == line2EndPoint.GetX())
                {
                    return false;
                }
                // 第二条线不是竖线
                else
                {
                    // y = k2x + b2
                    double k2 = (line2EndPoint.GetY() - line2StartPoint.GetY()) / (line2EndPoint.GetX() - line2StartPoint.GetX());
                    double b2 = line2EndPoint.GetY() - k2 * line2EndPoint.GetX();

                    double crossY = k2 * line1StartPoint.GetX() + b2;
                    if (crossY >= Math.Min(line1StartPoint.GetY(), line1EndPoint.GetY()) && crossY <= Math.Max(line1StartPoint.GetY(), line1EndPoint.GetY()))
                    {
                        return true;
                    }
                }
            }
            // 第一条线有斜率
            else
            {
                double k1 = (line1EndPoint.GetY() - line1StartPoint.GetY()) / (line1EndPoint.GetX() - line1StartPoint.GetX());
                double b1 = line1EndPoint.GetY() - k1 * line1EndPoint.GetX();

                // 第二条线是竖线
                if (line2EndPoint.GetX() == line2StartPoint.GetX())
                {
                    double crossY = k1 * line2EndPoint.GetX() + b1;
                    if (crossY >= Math.Min(line2StartPoint.GetY(), line2EndPoint.GetY()) && crossY <= Math.Max(line2StartPoint.GetY(), line2EndPoint.GetY()))
                    {
                        return true;
                    }
                }
                else
                {
                    // y2 = k2x + b2
                    double k2 = (line2EndPoint.GetY() - line2StartPoint.GetY()) / (line2EndPoint.GetX() - line2StartPoint.GetX());
                    double b2 = line2EndPoint.GetY() - k2 * line2EndPoint.GetX();

                    double crossX = (b2 - b1) / (k1 - k2);
                    double crossY = k1 * crossX + b1;

                    bool flagLine1 = false;
                    if (crossY >= Math.Min(line1StartPoint.GetY(), line1EndPoint.GetY()) && crossY <= Math.Max(line1StartPoint.GetY(), line1EndPoint.GetY()) && crossX >= Math.Min(line1StartPoint.GetX(), line1EndPoint.GetX()) && crossX <= Math.Max(line1StartPoint.GetX(), line1EndPoint.GetX()))
                    {
                        flagLine1 = true;
                    }

                    bool flagLine2 = false;
                    if (crossY >= Math.Min(line2StartPoint.GetY(), line2EndPoint.GetY()) && crossY <= Math.Max(line2StartPoint.GetY(), line2EndPoint.GetY()) && crossX >= Math.Min(line2StartPoint.GetX(), line2EndPoint.GetX()) && crossX <= Math.Max(line2StartPoint.GetX(), line2EndPoint.GetX()))
                    {
                        flagLine2 = true;
                    }

                    return flagLine1 && flagLine2;
                }
            }

            return false;
        }

        public static bool IsCross2(IPoint line1StartPoint, IPoint line1EndPoint, IPoint line2StartPoint, IPoint line2EndPoint)
        {
            if (line1StartPoint.GetX() == line2StartPoint.GetX() && line1StartPoint.GetY() == line2StartPoint.GetY()) return false;
            if (line1StartPoint.GetX() == line2EndPoint.GetX() && line1StartPoint.GetY() == line2EndPoint.GetY()) return false;
            if (line1EndPoint.GetX() == line2StartPoint.GetX() && line1EndPoint.GetY() == line2StartPoint.GetY()) return false;
            if (line1EndPoint.GetX() == line2EndPoint.GetX() && line1EndPoint.GetY() == line2EndPoint.GetY()) return false;

            return IsCross(line1StartPoint, line1EndPoint, line2StartPoint, line2EndPoint);
        }

        public static IPoint CrossPoint(IPoint line1StartPoint, IPoint line1EndPoint, IPoint line2StartPoint, IPoint line2EndPoint)
        {
            // 求第一条线的斜率
            // 如果第一条线是个竖线，没斜率
            if (line1StartPoint.GetX() == line1EndPoint.GetX())
            {
                // 并且第二条线也是竖线，平行
                if (line2StartPoint.GetX() == line2EndPoint.GetX())
                {
                    return null;
                }
                // 第二条线不是竖线
                else
                {
                    // y = k2x + b2
                    double k2 = (line2EndPoint.GetY() - line2StartPoint.GetY()) / (line2EndPoint.GetX() - line2StartPoint.GetX());
                    double b2 = line2EndPoint.GetY() - k2 * line2EndPoint.GetX();

                    double crossY = k2 * line1StartPoint.GetX() + b2;
                    if (crossY >= Math.Min(line1StartPoint.GetY(), line1EndPoint.GetY()) && crossY <= Math.Max(line1StartPoint.GetY(), line1EndPoint.GetY()))
                    {
                        return new Point(line1StartPoint.GetX(), crossY);
                    }
                }
            }
            // 第一条线有斜率
            else
            {
                double k1 = (line1EndPoint.GetY() - line1StartPoint.GetY()) / (line1EndPoint.GetX() - line1StartPoint.GetX());
                double b1 = line1EndPoint.GetY() - k1 * line1EndPoint.GetX();

                // 第二条线是竖线
                if (line2EndPoint.GetX() == line2StartPoint.GetX())
                {
                    double crossY = k1 * line2EndPoint.GetX() + b1;
                    if (crossY >= Math.Min(line2StartPoint.GetY(), line2EndPoint.GetY()) && crossY <= Math.Max(line2StartPoint.GetY(), line2EndPoint.GetY()))
                    {
                        return new Point(line2EndPoint.GetX(), crossY);
                    }
                }
                else
                {
                    // y2 = k2x + b2
                    double k2 = (line2EndPoint.GetY() - line2StartPoint.GetY()) / (line2EndPoint.GetX() - line2StartPoint.GetX());
                    double b2 = line2EndPoint.GetY() - k2 * line2EndPoint.GetX();

                    double crossX = Math.Round((b2 - b1) / (k1 - k2), 6);
                    double crossY = Math.Round(k1 * crossX + b1, 6);

                    bool flagLine1 = false;
                    if (crossY >= Math.Round(Math.Min(line1StartPoint.GetY(), line1EndPoint.GetY()), 6)
                        && crossY <= Math.Round(Math.Max(line1StartPoint.GetY(), line1EndPoint.GetY()), 6) 
                        && crossX >= Math.Round(Math.Min(line1StartPoint.GetX(), line1EndPoint.GetX()), 6)
                        && crossX <= Math.Round(Math.Max(line1StartPoint.GetX(), line1EndPoint.GetX()), 6))
                    {
                        flagLine1 = true;
                    }

                    bool flagLine2 = false;
                    if (crossY >= Math.Round(Math.Min(line2StartPoint.GetY(), line2EndPoint.GetY()), 6)
                        && crossY <= Math.Round(Math.Max(line2StartPoint.GetY(), line2EndPoint.GetY()), 6)
                        && crossX >= Math.Round(Math.Min(line2StartPoint.GetX(), line2EndPoint.GetX()), 6)
                        && crossX <= Math.Round(Math.Max(line2StartPoint.GetX(), line2EndPoint.GetX()), 6))
                    {
                        flagLine2 = true;
                    }

                    if (flagLine1 && flagLine2)
                    {
                        return new Point(crossX, crossY);
                    }
                    else
                    {
                        return null;
                    }
                }
            }

            return null;
        }

        // 可能是多部件的MultiPolygon
        public static IGeometry Union(IGeometry firstGeometry, IGeometry secondGeometry)
        {
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(firstGeometry);
            iGeospatial.Geometries.Geometry geom2 = GeometryConverter.ConvertToGEOSGeometry(secondGeometry);
            iGeospatial.Geometries.Geometry unionResult = geom1.Union(geom2);
            if (firstGeometry.GetGeometryType().StartsWith("OpenGIS"))
            {
                return GeometryConverter.ConvertToOpenGISGeometry(unionResult);
            }
            else
            {
                return GeometryConverter.ConvertToShapefileGeometry(unionResult);
            }
        }

        public static IGeometry Intersection(IGeometry firstGeometry, IGeometry secondGeometry)
        {
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(firstGeometry);
            iGeospatial.Geometries.Geometry geom2 = GeometryConverter.ConvertToGEOSGeometry(secondGeometry);
            iGeospatial.Geometries.Geometry unionResult = geom1.Intersection(geom2);
            if (firstGeometry.GetGeometryType().StartsWith("OpenGIS"))
            {
                return GeometryConverter.ConvertToOpenGISGeometry(unionResult);
            }
            else
            {
                return GeometryConverter.ConvertToShapefileGeometry(unionResult);
            }
        }

        public static IGeometry Buffer(IGeometry inputGeometry, double distance, CoordinateBase coordinateSystem)
        {
            BoundaryBox boundaryBox = inputGeometry.GetBoundaryBox();
            if (coordinateSystem.GetCoordinateType() == CoordinateType.Geographic)
            {
                distance /= (111000.0 * Math.Cos((0.5 * boundaryBox.YMax + 0.5 * boundaryBox.YMin) / 180.0 * Math.PI));
            }
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(inputGeometry);
            iGeospatial.Geometries.Geometry bufferGeom = geom1.Buffer(distance);
            if (inputGeometry.GetGeometryType().StartsWith("OpenGIS"))
            {
                return GeometryConverter.ConvertToOpenGISGeometry(bufferGeom);
            }
            else
            {
                return GeometryConverter.ConvertToShapefileGeometry(bufferGeom);
            }
        }

        public static ShpPolygon3D LineBuffer3D(ShpPolyline3D inputPolyline3D, BufferType bufferType, Vector3D normal, double length)
        {
            ShpPolygon3D newShpPolygon3D = new ShpPolygon3D();

            List<Point> leftSidePointList = new List<Point>();
            List<double> leftSideZList = new List<double>();

            List<Point> rightSidePointList = new List<Point>();
            List<double> rightSideZList = new List<double>();

            List<Point> centerPointList = new List<Point>();
            List<double> centerZList = new List<double>();

            for (int i = 0; i < inputPolyline3D.GetPointNumber(); i++)
            {
                Vector3D prevLoc;
                Vector3D nextLoc;
                Vector3D moveDirection;
                if (i == inputPolyline3D.GetPointNumber() - 1)
                {
                    prevLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i - 1), inputPolyline3D.ZList[i - 1]);
                    nextLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i), inputPolyline3D.ZList[i]);
                    Vector3D tangent = (nextLoc - prevLoc).Normalize();
                    tangent.Z = 0;
                    tangent = tangent.Normalize();
                    moveDirection = -Vector3D.Cross(tangent, normal);
                }
                else if (i == 0)
                {
                    prevLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i), inputPolyline3D.ZList[i]);
                    nextLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i + 1), inputPolyline3D.ZList[i + 1]);
                    Vector3D tangent = (nextLoc - prevLoc).Normalize();
                    tangent.Z = 0;
                    tangent = tangent.Normalize();
                    moveDirection = -Vector3D.Cross(tangent, normal);
                }
                else
                {
                    prevLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i - 1), inputPolyline3D.ZList[i - 1]);
                    Vector3D nowLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i), inputPolyline3D.ZList[i]);
                    nextLoc = new Vector3D(inputPolyline3D.GetPointByIndex(i + 1), inputPolyline3D.ZList[i + 1]);

                    Vector3D toPrevVector = (prevLoc - nowLoc).Normalize();
                    toPrevVector.Z = 0;
                    toPrevVector = toPrevVector.Normalize();
                    Vector3D toNextVector = (nextLoc - nowLoc).Normalize();
                    toNextVector.Z = 0;
                    toNextVector = toNextVector.Normalize();
                    Vector3D tangent = toNextVector.Normalize();
                    tangent.Z = 0;
                    tangent = tangent.Normalize();

                    if ((toPrevVector + toNextVector).Length() == 0)
                    {
                        moveDirection = -Vector3D.Cross(tangent, normal);
                    }
                    else
                    {
                        moveDirection = (toPrevVector + toNextVector).Normalize();
                        moveDirection.Z = 0;
                        moveDirection = moveDirection.Normalize();
                        if (Vector3D.Cross(moveDirection, tangent).Z > 0)
                        {
                            moveDirection = -moveDirection;
                        }
                    }

                    prevLoc = nowLoc;
                }

                // 生成左侧点序列
                if (bufferType == BufferType.Left || bufferType == BufferType.All)
                {
                    Vector3D newLeftPoint;
                    if (i == inputPolyline3D.GetPointNumber() - 1)
                    {
                        newLeftPoint = nextLoc + length * moveDirection;
                    }
                    else
                    {
                        newLeftPoint = prevLoc + length * moveDirection;
                    }
                    leftSidePointList.Add(new Point(newLeftPoint.X, newLeftPoint.Y));
                    leftSideZList.Add(newLeftPoint.Z);
                }

                // 生成右侧点序列
                if (bufferType == BufferType.Right || bufferType == BufferType.All)
                {
                    Vector3D newRightPoint;
                    if (i == inputPolyline3D.GetPointNumber() - 1)
                    {
                        newRightPoint = nextLoc - length * moveDirection;
                    }
                    else
                    {
                        newRightPoint = prevLoc - length * moveDirection;
                    }
                    rightSidePointList.Add(new Point(newRightPoint.X, newRightPoint.Y));
                    rightSideZList.Add(newRightPoint.Z);
                }

                centerPointList.Add(inputPolyline3D.GetPointByIndex(i) as Point);
                centerZList.Add(inputPolyline3D.ZList[i]);
            }

            List<Point> polygon3DPointList = new List<Point>();
            List<double> polygon3DZList = new List<double>();
            if (bufferType == BufferType.Left)
            {
                centerPointList.Reverse();
                centerZList.Reverse();
                polygon3DPointList.AddRange(leftSidePointList);
                polygon3DZList.AddRange(leftSideZList);
                polygon3DPointList.AddRange(centerPointList);
                polygon3DZList.AddRange(centerZList);
            }

            if (bufferType == BufferType.Right)
            {
                polygon3DPointList.AddRange(centerPointList);
                polygon3DZList.AddRange(centerZList);
                rightSidePointList.Reverse();
                rightSideZList.Reverse();
                polygon3DPointList.AddRange(rightSidePointList);
                polygon3DZList.AddRange(rightSideZList);
            }

            if (bufferType == BufferType.All)
            {
                polygon3DPointList.AddRange(leftSidePointList);
                polygon3DZList.AddRange(leftSideZList);
                rightSidePointList.Reverse();
                rightSideZList.Reverse();
                polygon3DPointList.AddRange(rightSidePointList);
                polygon3DZList.AddRange(rightSideZList);
            }

            newShpPolygon3D.PartList.Add(0);
            newShpPolygon3D.PointList.AddRange(polygon3DPointList);
            newShpPolygon3D.ZList.AddRange(polygon3DZList);
            for (int j = 0; j < polygon3DZList.Count; j++)
            {
                newShpPolygon3D.MList.Add(0);
            }

            newShpPolygon3D.PointList.Add(newShpPolygon3D.PointList.First());
            newShpPolygon3D.ZList.Add(newShpPolygon3D.ZList.First());
            newShpPolygon3D.MList.Add(newShpPolygon3D.MList.First());
            
            return newShpPolygon3D;
        }

        public static IGeometry Difference(IGeometry firstGeometry, IGeometry secondGeometry)
        {
            iGeospatial.Geometries.Geometry geom1 = GeometryConverter.ConvertToGEOSGeometry(firstGeometry);
            iGeospatial.Geometries.Geometry geom2 = GeometryConverter.ConvertToGEOSGeometry(secondGeometry);
            iGeospatial.Geometries.Geometry unionResult = geom1.Difference(geom2);
            if (firstGeometry.GetGeometryType().StartsWith("OpenGIS"))
            {
                return GeometryConverter.ConvertToOpenGISGeometry(unionResult);
            }
            else
            {
                return GeometryConverter.ConvertToShapefileGeometry(unionResult);
            }
        }

        public static bool IsLineSegmentCross(IPoint a, IPoint b, IPoint c, IPoint d)
        {
            Point tp1, tp2, tp3;
            tp1 = new Point(a.GetX() - c.GetX(), a.GetY() - c.GetY());
            tp2 = new Point(d.GetX() - c.GetX(), d.GetY() - c.GetY());
            tp3 = new Point(b.GetX() - c.GetX(), b.GetY() - c.GetY());

            if ((tp1.X * tp2.Y - tp1.Y * tp2.X) * (tp2.X * tp3.Y - tp2.Y * tp3.X) >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
