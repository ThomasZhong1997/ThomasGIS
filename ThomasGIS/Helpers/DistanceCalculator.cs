using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS._3DModel.Basic;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Mesh.Vector;
using ThomasGIS.TrajectoryPackage;

namespace ThomasGIS.Helpers
{
    public static class DistanceCalculator
    {
        private static double SpatialDistance(IPoint p1, IPoint p2)
        {
            double x1 = p1.GetX();
            double x2 = p2.GetX();
            double y1 = p1.GetY();
            double y2 = p2.GetY();
            return Math.Sqrt(Math.Pow((y2 - y1), 2) + Math.Pow((x2 - x1), 2));
        }

        public static double SpatialDistanceGeo(IPoint p1, IPoint p2)
        {
            double lon1 = p1.GetX() / 180.0 * Math.PI;
            double lat1 = p1.GetY() / 180.0 * Math.PI;
            double lon2 = p2.GetX() / 180.0 * Math.PI;
            double lat2 = p2.GetY() / 180.0 * Math.PI;

            double a = lat1 - lat2;
            double b = lon1 - lon2;
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(b / 2), 2)));

            s = s * 6378137;
            s = Math.Floor(s * 10000.0) / 10000.0;
            return s;
        }

        public static double SpatialDistance(ICollection<IPoint> pointList)
        {
            double distance = 0;
            for (int i = 0; i < pointList.Count() - 1; i++)
            {
                IPoint startPoint = pointList.ElementAt(i);
                IPoint endPoint = pointList.ElementAt(i + 1);
                distance += DistanceCalculator.SpatialDistance(startPoint, endPoint);
            }
            return distance;
        }

        public static double SpatialDistanceGeo(ICollection<IPoint> pointList)
        {
            double distance = 0;
            for (int i = 0; i < pointList.Count() - 1; i++)
            {
                IPoint startPoint = pointList.ElementAt(i);
                IPoint endPoint = pointList.ElementAt(i + 1);
                distance += DistanceCalculator.SpatialDistanceGeo(startPoint, endPoint);
            }
            return distance;
        }

        public static double SpatialDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt(Math.Pow((y2 - y1), 2) + Math.Pow((x2 - x1), 2));
        }

        public static double SpatialDistanceGeo(double lon1, double lat1, double lon2, double lat2)
        {
            lon1 = lon1 / 180.0 * Math.PI;
            lat1 = lat1 / 180.0 * Math.PI;
            lon2 = lon2 / 180.0 * Math.PI;
            lat2 = lat2 / 180.0 * Math.PI;

            double a = lat1 - lat2;
            double b = lon1 - lon2;
            double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(lat1) * Math.Cos(lat2) * Math.Pow(Math.Sin(b / 2), 2)));

            s = s * 6378137;
            s = Math.Floor(s * 10000.0) / 10000.0;
            return s;
        }

        // 线段与垂直线的夹角
        public static double DirectionAngle(IPoint p1, IPoint p2)
        {
            double p1X = p1.GetX();
            double p1Y = p1.GetY();
            double p2X = p2.GetX();
            double p2Y = p2.GetY();

            if (p1X == p2X && p1Y == p2Y) return -1;
            double angle;
            if (p1X < p2X)
            {
                if (p1Y == p2Y) return 90;
                double tanValue = (p2X - p1X) / (p2Y - p1Y);
                if (p1Y < p2Y)
                {
                    angle = Math.Atan(tanValue) * 180.0 / Math.PI;
                }
                else
                {
                    angle = 180.0 + Math.Atan(tanValue) * 180.0 / Math.PI;
                }

            }
            else
            {
                if (p1Y == p2Y) return 270;
                double tanValue = (p2X - p1X) / (p2Y - p1Y);
                if (p1Y < p2Y)
                {
                    angle = Math.Atan(tanValue) * 180.0 / Math.PI + 360.0;
                }
                else
                {
                    angle = Math.Atan(tanValue) * 180.0 / Math.PI + 180.0;
                }
            }

            return angle;
        }

        // 线段与水平线的夹角
        public static double HorizonAngle(IPoint p1, IPoint p2)
        {
            double p1X = p1.GetX();
            double p1Y = p1.GetY();
            double p2X = p2.GetX();
            double p2Y = p2.GetY();

            if (p1X == p2X && p1Y == p2Y) return -1;

            double angle = -1;

            if (p1X == p2X)
            {
                if (p2Y > p1Y) return 90;
                if (p2Y < p1Y) return 270;
            }
            else if (p2X > p1X)
            {
                double k = (p2Y - p1Y) / (p2X - p1X);
                angle = Math.Atan(k) / Math.PI * 180;
                if (angle < 0) angle = 360 + angle;
            }
            else
            {
                double k = (p2Y - p1Y) / (p2X - p1X);
                angle = Math.Atan(k) / Math.PI * 180;
                angle = 180 + angle;
            }

            return angle;
        }

        public static double DirectionAngleGeo(IPoint p1, IPoint p2)
        {
            double p1X = p1.GetX();
            double p1Y = p1.GetY();
            double p2X = p2.GetX();
            double p2Y = p2.GetY();

            if (p1X == p2X && p1Y == p2Y) return -1;

            double angle;
            if (p1X < p2X)
            {
                if (p1Y == p2Y) return 90;
                double distanceY = SpatialDistanceGeo(p1X, p1Y, p1X, p2Y);
                double distanceX = SpatialDistanceGeo(p1X, p2Y, p2X, p2Y);
                if (p1Y > p2Y) distanceY = -distanceY;
                double tanValue = distanceX / distanceY;
                angle = Math.Atan(tanValue) * 180.0 / Math.PI;

                if (angle == 0 && p1Y > p2Y)
                {
                    angle = 180.0;
                }

                if (angle < 0) angle = 180.0 + angle;
            }
            else
            {
                if (p1Y == p2Y) return 270;
                double distanceY = SpatialDistanceGeo(p1X, p1Y, p1X, p2Y);
                double distanceX = -SpatialDistanceGeo(p1X, p2Y, p2X, p2Y);
                if (p1Y > p2Y) distanceY = -distanceY;
                double tanValue = distanceX / distanceY;
                angle = Math.Atan(tanValue) * 180.0 / Math.PI;

                if (angle == 0)
                {
                    if (p1Y > p2Y)
                    {
                        angle = 180.0;
                    }
                    else
                    {
                        angle = 0.0;
                    }
                }
                else if (angle > 0)
                {
                    angle += 180.0;
                }
                else
                {
                    angle = 360.0 + angle;
                }
            }

            return angle;
        }

        public static double SpatialDistance(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                if ((lineEndY <= pointY && lineStartY >= pointY) || (lineEndY >= pointY && lineStartY <= pointY))
                {
                    return Math.Abs(pointX - lineStartX);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                if ((lineEndX <= pointX && lineStartX >= pointX) || (lineEndX >= pointX && lineStartX <= pointX))
                {
                    return Math.Abs(pointY - lineStartY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);
                double crossY = k * (crossX - lineStartX) + lineStartY;

                if ((lineEndX <= crossX && lineStartX >= crossX) || (lineEndX >= crossX && lineStartX <= crossX))
                {
                    return DistanceCalculator.SpatialDistance(crossX, crossY, pointX, pointY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
        }

        public static double SpatialDistanceGeo(IShpPolygon polygon, IPoint point)
        {
            if (TopoCalculator.IsContain(polygon, new ShpPoint(point.GetX(), point.GetY())))
            {
                return -1;
            }

            double minDistance = double.MaxValue;

            for (int i = 0; i < polygon.GetPartNumber(); i++)
            {
                List<IPoint> partPoints = polygon.GetPartByIndex(i).ToList();
                for (int j = 0; j < partPoints.Count - 1; j++)
                {
                    IPoint startPoint = partPoints[j];
                    IPoint endPoint = partPoints[j + 1];

                    double distance = SpatialDistanceGeo(startPoint.GetX(), startPoint.GetY(), endPoint.GetX(), endPoint.GetY(), point.GetX(), point.GetY());

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return minDistance;
        }

        public static double SpatialDistanceGeo(IShpPolyline polyline, IPoint point)
        {
            double minDistance = double.MaxValue;

            for (int i = 0; i < polyline.GetPartNumber(); i++)
            {
                List<IPoint> partPoints = polyline.GetPartByIndex(i).ToList();
                for (int j = 0; j < partPoints.Count - 1; j++)
                {
                    IPoint startPoint = partPoints[j];
                    IPoint endPoint = partPoints[j + 1];

                    double distance = SpatialDistanceGeo(startPoint.GetX(), startPoint.GetY(), endPoint.GetX(), endPoint.GetY(), point.GetX(), point.GetY());

                    if (distance < minDistance)
                    {
                        minDistance = distance;
                    }
                }
            }

            return minDistance;
        }

        public static double SpatialDistanceGeo(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                if ((lineEndY <= pointY && lineStartY >= pointY) || (lineEndY >= pointY && lineStartY <= pointY))
                {
                    return DistanceCalculator.SpatialDistanceGeo(lineStartX, pointY, pointX, pointY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistanceGeo(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistanceGeo(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                if ((lineEndX <= pointX && lineStartX >= pointX) || (lineEndX >= pointX && lineStartX <= pointX))
                {
                    return DistanceCalculator.SpatialDistanceGeo(pointX, lineStartY, pointX, pointY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistanceGeo(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistanceGeo(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);
                double crossY = k * (crossX - lineStartX) + lineStartY;

                if ((lineEndX <= crossX && lineStartX >= crossX) || (lineEndX >= crossX && lineStartX <= crossX))
                {
                    return DistanceCalculator.SpatialDistanceGeo(crossX, crossY, pointX, pointY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistanceGeo(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistanceGeo(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? toEndDistance : toStartDistance;
                }
            }
        }

        // 直角坐标系下的点到线段距离
        public static double SpatialDistance(ISingleLine singleLine, IPoint point)
        {
            return DistanceCalculator.SpatialDistance(singleLine.GetStartPoint().GetX(), singleLine.GetStartPoint().GetY(), singleLine.GetEndPoint().GetX(), singleLine.GetEndPoint().GetY(), point.GetX(), point.GetY());
        }

        public static double SpatialDistance(IPoint lineStartPoint, IPoint lineEndPoint, IPoint point)
        {
            return DistanceCalculator.SpatialDistance(lineStartPoint.GetX(), lineStartPoint.GetY(), lineEndPoint.GetX(), lineEndPoint.GetY(), point.GetX(), point.GetY());
        }

        // 地理坐标系下的点到线段的距离
        public static double SpatialDistanceGeo(ISingleLine singleLine, IPoint point)
        {
            return DistanceCalculator.SpatialDistanceGeo(singleLine.GetStartPoint().GetX(), singleLine.GetStartPoint().GetY(), singleLine.GetEndPoint().GetX(), singleLine.GetEndPoint().GetY(), point.GetX(), point.GetY());
        }

        /// <summary>
        /// 返回点垂线在线上的交点，若没有则返回距离点最近的线端点
        /// </summary>
        /// <param name="lineStartX"></param>
        /// <param name="lineStartY"></param>
        /// <param name="lineEndX"></param>
        /// <param name="lineEndY"></param>
        /// <param name="pointX"></param>
        /// <param name="pointY"></param>
        /// <returns></returns>
        public static Point CrossPoint(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                if ((lineEndY <= pointY && lineStartY >= pointY) || (lineEndY >= pointY && lineStartY <= pointY))
                {
                    return new Point(lineStartX, pointY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? new Point(lineEndX, lineEndY) : new Point(lineStartX, lineStartY);
                }
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                if ((lineEndX <= pointX && lineStartX >= pointX) || (lineEndX >= pointX && lineStartX <= pointX))
                {
                    return new Point(pointX, lineStartY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? new Point(lineEndX, lineEndY) : new Point(lineStartX, lineStartY);
                }
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);
                double crossY = k * (crossX - lineStartX) + lineStartY;

                if ((lineEndX <= crossX && lineStartX >= crossX) || (lineEndX >= crossX && lineStartX <= crossX))
                {
                    return new Point(crossX, crossY);
                }
                else
                {
                    double toStartDistance = DistanceCalculator.SpatialDistance(lineStartX, lineStartY, pointX, pointY);
                    double toEndDistance = DistanceCalculator.SpatialDistance(lineEndX, lineEndY, pointX, pointY);
                    return toStartDistance > toEndDistance ? new Point(lineEndX, lineEndY) : new Point(lineStartX, lineStartY);
                }
            }
        }

        public static Point CrossPoint(ISingleLine singleLine, IPoint point)
        {
            return DistanceCalculator.CrossPoint(singleLine.GetStartPoint().GetX(), singleLine.GetStartPoint().GetY(), singleLine.GetEndPoint().GetX(), singleLine.GetEndPoint().GetY(), point.GetX(), point.GetY());
        }

        public static Point CrossPoint(IPoint lineStartPoint, IPoint lineEndPoint, IPoint point)
        {
            return DistanceCalculator.CrossPoint(lineStartPoint.GetX(), lineStartPoint.GetY(), lineEndPoint.GetX(), lineEndPoint.GetY(), point.GetX(), point.GetY());
        }

        ///<summary>
        ///返回点垂线在线上的交点，若没有则返回 null
        ///</summary>
        public static Point CrossPoint2(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                if ((lineEndY <= pointY && lineStartY >= pointY) || (lineEndY >= pointY && lineStartY <= pointY))
                {
                    return new Point(lineStartX, pointY);
                }
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                if ((lineEndX <= pointX && lineStartX >= pointX) || (lineEndX >= pointX && lineStartX <= pointX))
                {
                    return new Point(pointX, lineStartY);
                }
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);
                double crossY = k * (crossX - lineStartX) + lineStartY;

                if ((lineEndX <= crossX && lineStartX >= crossX) || (lineEndX >= crossX && lineStartX <= crossX))
                {
                    return new Point(crossX, crossY);
                }
            }

            return null;
        }

        /// <summary>
        /// 点和线段的相交关系
        /// </summary>
        /// <param name="lineStartX"></param>
        /// <param name="lineStartY"></param>
        /// <param name="lineEndX"></param>
        /// <param name="lineEndY"></param>
        /// <param name="pointX"></param>
        /// <param name="pointY"></param>
        /// <returns>0表示相交，-1表示左侧，1表示右侧</returns>
        public static int CrossPoint3(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                if ((lineEndY <= pointY && lineStartY >= pointY) || (lineEndY >= pointY && lineStartY <= pointY))
                {
                    return 0;
                }
                else if (lineEndY > pointY && lineStartY > pointY)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                if ((lineEndX <= pointX && lineStartX >= pointX) || (lineEndX >= pointX && lineStartX <= pointX))
                {
                    return 0;
                }
                else if (lineEndX > pointX && lineStartX > pointX)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);

                if ((lineEndX <= crossX && lineStartX >= crossX) || (lineEndX >= crossX && lineStartX <= crossX))
                {
                    return 0;
                }
                else if (lineEndX > crossX && lineStartX > crossX)
                {
                    return -1;
                }
                else
                {
                    return 1;
                }
            }
        }

        public static Point CrossPoint2(ISingleLine singleLine, IPoint point)
        {
            return DistanceCalculator.CrossPoint2(singleLine.GetStartPoint().GetX(), singleLine.GetStartPoint().GetY(), singleLine.GetEndPoint().GetX(), singleLine.GetEndPoint().GetY(), point.GetX(), point.GetY());
        }

        public static Point CrossPoint2(IPoint lineStartPoint, IPoint lineEndPoint, IPoint point)
        {
            return DistanceCalculator.CrossPoint2(lineStartPoint.GetX(), lineStartPoint.GetY(), lineEndPoint.GetX(), lineEndPoint.GetY(), point.GetX(), point.GetY());
        }

        public static int CrossPoint3(IPoint lineStartPoint, IPoint lineEndPoint, IPoint point)
        {
            return DistanceCalculator.CrossPoint3(lineStartPoint.GetX(), lineStartPoint.GetY(), lineEndPoint.GetX(), lineEndPoint.GetY(), point.GetX(), point.GetY());
        }

        /// <summary>
        /// 点和直线的交点
        /// </summary>
        /// <param name="lineStartX"></param>
        /// <param name="lineStartY"></param>
        /// <param name="lineEndX"></param>
        /// <param name="lineEndY"></param>
        /// <param name="pointX"></param>
        /// <param name="pointY"></param>
        /// <returns></returns>
        public static Point CrossPoint4(double lineStartX, double lineStartY, double lineEndX, double lineEndY, double pointX, double pointY)
        {
            // 一条竖线k=∞
            if (lineStartX == lineEndX)
            {
                return new Point(lineStartX, pointY);
            }
            // 一条横线
            else if (lineStartY == lineEndY)
            {
                return new Point(pointX, lineStartY);
            }
            else
            {
                double k = (lineEndY - lineStartY) / (lineEndX - lineStartX);

                // 垂线交点X,Y
                double crossX = (k * pointY - k * lineStartY + pointX + lineStartX * k * k) / (k * k + 1);
                double crossY = k * (crossX - lineStartX) + lineStartY;
                return new Point(crossX, crossY);
            }
        }

        public static Point CrossPoint4(IPoint lineStartPoint, IPoint lineEndPoint, IPoint point)
        {
            return DistanceCalculator.CrossPoint4(lineStartPoint.GetX(), lineStartPoint.GetY(), lineEndPoint.GetX(), lineEndPoint.GetY(), point.GetX(), point.GetY());
        }

        public static double SpatialArea(ICollection<IPoint> pointList)
        {
            if (pointList.First() != pointList.Last())
            {
                pointList.Add(pointList.First());
            }

            if (pointList.Count() < 3) return 0;

            double area = 0;
            for (int i = 0; i < pointList.Count() - 1; i++)
            {
                IPoint startPoint = pointList.ElementAt(i);
                IPoint endPoint = pointList.ElementAt(i + 1);
                area += 0.5 * (endPoint.GetX() - startPoint.GetX()) * (endPoint.GetY() + startPoint.GetY());
            }

            return area;
        }

        public static double SpatialAreaGeo(ICollection<IPoint> pointList)
        {
            if (pointList.First() != pointList.Last())
            {
                pointList.Add(pointList.First());
            }

            if (pointList.Count() < 3) return 0;

            double area = 0;
            for (int i = 0; i < pointList.Count() - 1; i++)
            {
                IPoint startPoint = pointList.ElementAt(i);
                IPoint endPoint = pointList.ElementAt(i + 1);
                area += startPoint.GetX() * endPoint.GetY();
                area -= startPoint.GetY() * endPoint.GetX();
            }

            return Math.Abs(area * 0.5 * 9101160000.085981);
        }

        public static double SpatialDistance(IGeometry geometry1, IGeometry geometry2)
        {
            string geom1Type = geometry1.GetBaseGeometryType();
            string geom2Type = geometry2.GetBaseGeometryType();
            // 两个点
            if (geom1Type == "Point" && geom2Type == "Point")
            {
                IPoint geom1 = geometry1 as IPoint;
                IPoint geom2 = geometry2 as IPoint;
                return SpatialDistance(geom1, geom2);
            }
            // 两个 LineString
            else if (geom1Type == "LineString" && geom2Type == "LineString")
            {
                ILineString geom1 = geometry1 as ILineString;
                ILineString geom2 = geometry2 as ILineString;
                double minDistance = double.MaxValue;
                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IPoint> pointList2 = geom2.GetPointList().ToList();
                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < pointList2.Count - 1; j++)
                    {
                        IPoint line1S = pointList1[i];
                        IPoint line1E = pointList1[i + 1];
                        IPoint line2S = pointList2[j];
                        IPoint line2E = pointList2[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                    }
                }
                return minDistance;
            }
            // 一个 LineString 一个 MultiLineString
            else if ((geom1Type == "LineString" && geom2Type == "MultiLineString")
                  || (geom1Type == "MultiLineString" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IMultiLineString geom2 = geometry2 as IMultiLineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IMultiLineString;
                }

                double minDistance = double.MaxValue;
                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> pointList2 = geom2.GetPointList().ToList();

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < pointList2.Count; j++)
                    {
                        List<IPoint> partList = pointList2[j].ToList();
                        for (int k = 0; k < partList.Count - 1; k++)
                        {
                            IPoint line1S = pointList1[i];
                            IPoint line1E = pointList1[i + 1];
                            IPoint line2S = partList[k];
                            IPoint line2E = partList[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                        }
                    }
                }
                return minDistance;
            }
            // 两个 MultiLineString
            else if (geom1Type == "MultiLineString" && geom2Type == "MultiLineString")
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IMultiLineString geom2 = geometry2 as IMultiLineString;

                if (TopoCalculator.IsIntersection(geom1, geom2)) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();
                return InnerMinDistance(partList1, partList2);
            }
            // 一个 Point 一个 LineString
            else if ((geom1Type == "Point" && geom2Type == "LineString")
                  || (geom1Type == "LineString" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                ILineString geom2 = geometry2 as ILineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as ILineString;
                }

                List<IPoint> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count - 1; i++)
                {
                    IPoint lineS = pointList[i];
                    IPoint lineE = pointList[i + 1];
                    minDistance = Math.Min(minDistance, SpatialDistance(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                }
                return minDistance;
            }
            // 一个 Point 一个 MultiLineString
            else if ((geom1Type == "Point" && geom2Type == "MultiLineString")
                  || (geom1Type == "MultiLineString" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IMultiLineString geom2 = geometry2 as IMultiLineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as IMultiLineString;
                }

                List<IEnumerable<IPoint>> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count; i++)
                {
                    List<IPoint> partList = pointList[i].ToList();
                    for (int j = 0; j < partList.Count - 1; j++)
                    {
                        IPoint lineS = partList[j];
                        IPoint lineE = partList[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistance(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                    }
                }
                return minDistance;
            }
            // 一个 Point 一个 Polygon
            else if ((geom1Type == "Point" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as IPolygon;
                }

                // geom2是否包含geom1
                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                if (isContain) return 0;

                List<IEnumerable<IPoint>> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count; i++)
                {
                    List<IPoint> partList = pointList[i].ToList();
                    for (int j = 0; j < partList.Count - 1; j++)
                    {
                        IPoint lineS = partList[j];
                        IPoint lineE = partList[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistance(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                    }
                }
                return minDistance;
            }
            // 一个Point一个MultiPolygon
            else if ((geom1Type == "Point" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as IMultiPolygon;
                }

                // geom2是否包含geom1
                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                if (isContain) return 0;

                List<IEnumerable<IEnumerable<IPoint>>> polygonList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < polygonList.Count; i++)
                {
                    List<IEnumerable<IPoint>> partList = polygonList[i].ToList();
                    for (int j = 0; j < partList.Count; j++)
                    {
                        List<IPoint> pointList = partList[j].ToList();
                        for (int k = 0; k < pointList.Count - 1; k++)
                        {
                            IPoint lineS = pointList[k];
                            IPoint lineE = pointList[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistance(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                        }
                    }
                }
                return minDistance;
            }
            // 一个LineString一个Polygon
            else if ((geom1Type == "LineString" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < partList2.Count; j++)
                    {
                        List<IPoint> pointList2 = partList2[j].ToList();
                        for (int k = 0; k < pointList2.Count - 1; k++)
                        {
                            IPoint line1S = pointList1[i];
                            IPoint line1E = pointList1[i + 1];
                            IPoint line2S = pointList2[k];
                            IPoint line2E = pointList2[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                        }
                    }
                }
                return minDistance;
            }
            // 一个LineString一个MultiPolygon
            else if ((geom1Type == "LineString" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < polygonList2.Count; j++)
                    {
                        List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                        for (int k = 0; k < partList2.Count; k++)
                        {
                            List<IPoint> pointList2 = partList2[k].ToList();
                            for (int m = 0; m < pointList2.Count - 1; m++)
                            {
                                IPoint line1S = pointList1[i];
                                IPoint line1E = pointList1[i + 1];
                                IPoint line2S = pointList2[m];
                                IPoint line2E = pointList2[m + 1];
                                minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                            }
                        }
                    }
                }
                return minDistance;
            }
            // 一个MultiLineString一个Polygon
            else if ((geom1Type == "MultiLineString" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "MultiLineString"))
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IMultiLineString;
                    geom2 = geometry1 as IPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();

                return InnerMinDistance(partList1, partList2);
            }
            // 一个MultiLineString一个MultiPolygon
            else if ((geom1Type == "MultiLineString" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "MultiLineString"))
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IMultiLineString;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistance(partList1, polygonList2);
            }
            // 两个Polygon
            else if (geom1Type == "Polygon" && geom2Type == "Polygon")
            {
                IPolygon geom1 = geometry1 as IPolygon;
                IPolygon geom2 = geometry2 as IPolygon;

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();

                return InnerMinDistance(partList1, partList2);
            }
            // 一个Polygon一个MultiPolygon
            else if ((geom1Type == "Polygon" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "Polygon"))
            {
                IPolygon geom1 = geometry1 as IPolygon;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IPolygon;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistance(partList1, polygonList2);
            }
            // 两个MultiPolygon
            else if (geom1Type == "MultiPolygon" && geom2Type == "MultiPolygon")
            {
                IMultiPolygon geom1 = geometry1 as IMultiPolygon;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IEnumerable<IPoint>>> polygonList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistance(polygonList1, polygonList2);
            }
            throw new NotImplementedException();
        }

        public static double SpatialDistanceGeo(IGeometry geometry1, IGeometry geometry2)
        {
            string geom1Type = geometry1.GetBaseGeometryType();
            string geom2Type = geometry2.GetBaseGeometryType();
            // 两个点
            if (geom1Type == "Point" && geom2Type == "Point")
            {
                IPoint geom1 = geometry1 as IPoint;
                IPoint geom2 = geometry2 as IPoint;
                return SpatialDistanceGeo(geom1, geom2);
            }
            // 两个 LineString
            else if (geom1Type == "LineString" && geom2Type == "LineString")
            {
                ILineString geom1 = geometry1 as ILineString;
                ILineString geom2 = geometry2 as ILineString;
                double minDistance = double.MaxValue;
                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IPoint> pointList2 = geom2.GetPointList().ToList();
                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < pointList2.Count - 1; j++)
                    {
                        IPoint line1S = pointList1[i];
                        IPoint line1E = pointList1[i + 1];
                        IPoint line2S = pointList2[j];
                        IPoint line2E = pointList2[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                    }
                }
                return minDistance;
            }
            // 一个 LineString 一个 MultiLineString
            else if ((geom1Type == "LineString" && geom2Type == "MultiLineString")
                  || (geom1Type == "MultiLineString" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IMultiLineString geom2 = geometry2 as IMultiLineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IMultiLineString;
                }

                double minDistance = double.MaxValue;
                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> pointList2 = geom2.GetPointList().ToList();

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < pointList2.Count; j++)
                    {
                        List<IPoint> partList = pointList2[j].ToList();
                        for (int k = 0; k < partList.Count - 1; k++)
                        {
                            IPoint line1S = pointList1[i];
                            IPoint line1E = pointList1[i + 1];
                            IPoint line2S = partList[k];
                            IPoint line2E = partList[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                        }
                    }
                }
                return minDistance;
            }
            // 两个 MultiLineString
            else if (geom1Type == "MultiLineString" && geom2Type == "MultiLineString")
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IMultiLineString geom2 = geometry2 as IMultiLineString;

                if (TopoCalculator.IsIntersection(geom1, geom2)) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();
                return InnerMinDistanceGeo(partList1, partList2);
            }
            // 一个 Point 一个 LineString
            else if ((geom1Type == "Point" && geom2Type == "LineString")
                  || (geom1Type == "LineString" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                ILineString geom2 = geometry2 as ILineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as ILineString;
                }

                List<IPoint> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count - 1; i++)
                {
                    IPoint lineS = pointList[i];
                    IPoint lineE = pointList[i + 1];
                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                }
                return minDistance;
            }
            // 一个 Point 一个 MultiLineString
            else if ((geom1Type == "Point" && geom2Type == "MultiLineString")
                  || (geom1Type == "MultiLineString" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IMultiLineString geom2 = geometry2 as IMultiLineString;
                if (geom1 == null || geom2 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as IMultiLineString;
                }

                List<IEnumerable<IPoint>> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count; i++)
                {
                    List<IPoint> partList = pointList[i].ToList();
                    for (int j = 0; j < partList.Count - 1; j++)
                    {
                        IPoint lineS = partList[j];
                        IPoint lineE = partList[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistanceGeo(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                    }
                }
                return minDistance;
            }
            // 一个 Point 一个 Polygon
            else if ((geom1Type == "Point" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IPoint;
                    geom2 = geometry1 as IPolygon;
                }

                // geom2是否包含geom1
                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                if (isContain) return 0;

                List<IEnumerable<IPoint>> pointList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList.Count; i++)
                {
                    List<IPoint> partList = pointList[i].ToList();
                    for (int j = 0; j < partList.Count - 1; j++)
                    {
                        IPoint lineS = partList[j];
                        IPoint lineE = partList[j + 1];
                        minDistance = Math.Min(minDistance, SpatialDistanceGeo(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                    }
                }
                return minDistance;
            }
            // 一个Point一个MultiPolygon
            else if ((geom1Type == "Point" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "Point"))
            {
                IPoint geom1 = geometry1 as IPoint;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geom2 as IPoint;
                    geom2 = geom1 as IMultiPolygon;
                }

                // geom2是否包含geom1
                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                if (isContain) return 0;

                List<IEnumerable<IEnumerable<IPoint>>> polygonList = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < polygonList.Count; i++)
                {
                    List<IEnumerable<IPoint>> partList = polygonList[i].ToList();
                    for (int j = 0; j < partList.Count; j++)
                    {
                        List<IPoint> pointList = partList[j].ToList();
                        for (int k = 0; k < pointList.Count - 1; k++)
                        {
                            IPoint lineS = pointList[k];
                            IPoint lineE = pointList[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistanceGeo(lineS.GetX(), lineS.GetY(), lineE.GetX(), lineE.GetY(), geom1.GetX(), geom1.GetY()));
                        }
                    }
                }
                return minDistance;
            }
            // 一个LineString一个Polygon
            else if ((geom1Type == "LineString" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < partList2.Count; j++)
                    {
                        List<IPoint> pointList2 = partList2[j].ToList();
                        for (int k = 0; k < pointList2.Count - 1; k++)
                        {
                            IPoint line1S = pointList1[i];
                            IPoint line1E = pointList1[i + 1];
                            IPoint line2S = pointList2[k];
                            IPoint line2E = pointList2[k + 1];
                            minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                        }
                    }
                }
                return minDistance;
            }
            // 一个LineString一个MultiPolygon
            else if ((geom1Type == "LineString" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "LineString"))
            {
                ILineString geom1 = geometry1 as ILineString;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as ILineString;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IPoint> pointList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();
                double minDistance = double.MaxValue;

                for (int i = 0; i < pointList1.Count - 1; i++)
                {
                    for (int j = 0; j < polygonList2.Count; j++)
                    {
                        List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                        for (int k = 0; k < partList2.Count; k++)
                        {
                            List<IPoint> pointList2 = partList2[k].ToList();
                            for (int m = 0; m < pointList2.Count - 1; m++)
                            {
                                IPoint line1S = pointList1[i];
                                IPoint line1E = pointList1[i + 1];
                                IPoint line2S = pointList2[m];
                                IPoint line2E = pointList2[m + 1];
                                minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                            }
                        }
                    }
                }
                return minDistance;
            }
            // 一个MultiLineString一个Polygon
            else if ((geom1Type == "MultiLineString" && geom2Type == "Polygon")
                  || (geom1Type == "Polygon" && geom2Type == "MultiLineString"))
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IPolygon geom2 = geometry2 as IPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IMultiLineString;
                    geom2 = geometry1 as IPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();

                return InnerMinDistanceGeo(partList1, partList2);
            }
            // 一个MultiLineString一个MultiPolygon
            else if ((geom1Type == "MultiLineString" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "MultiLineString"))
            {
                IMultiLineString geom1 = geometry1 as IMultiLineString;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IMultiLineString;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistanceGeo(partList1, polygonList2);
            }
            // 两个Polygon
            else if (geom1Type == "Polygon" && geom2Type == "Polygon")
            {
                IPolygon geom1 = geometry1 as IPolygon;
                IPolygon geom2 = geometry2 as IPolygon;

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IPoint>> partList2 = geom2.GetPointList().ToList();

                return InnerMinDistanceGeo(partList1, partList2);
            }
            // 一个Polygon一个MultiPolygon
            else if ((geom1Type == "Polygon" && geom2Type == "MultiPolygon")
                  || (geom1Type == "MultiPolygon" && geom2Type == "Polygon"))
            {
                IPolygon geom1 = geometry1 as IPolygon;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;
                if (geom1 == null)
                {
                    geom1 = geometry2 as IPolygon;
                    geom2 = geometry1 as IMultiPolygon;
                }

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IPoint>> partList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistanceGeo(partList1, polygonList2);
            }
            // 两个MultiPolygon
            else if (geom1Type == "MultiPolygon" && geom2Type == "MultiPolygon")
            {
                IMultiPolygon geom1 = geometry1 as IMultiPolygon;
                IMultiPolygon geom2 = geometry2 as IMultiPolygon;

                bool isContain = TopoCalculator.IsContain(geom1, geom2);
                bool IsIntersection = TopoCalculator.IsIntersection(geom1, geom2);
                if (isContain || IsIntersection) return 0;

                List<IEnumerable<IEnumerable<IPoint>>> polygonList1 = geom1.GetPointList().ToList();
                List<IEnumerable<IEnumerable<IPoint>>> polygonList2 = geom2.GetPointList().ToList();

                return InnerMinDistanceGeo(polygonList1, polygonList2);
            }
            throw new NotImplementedException();
        }

        public static double SpatialDistance3D(Vector3D v1, Vector3D v2)
        {
            return Vector3D.Length(v1 - v2);
        }

        public static double SpatialDistance3DGeo(Vector3D v1, Vector3D v2)
        {
            double planeDistance = DistanceCalculator.SpatialDistanceGeo(v1.X, v1.Y, v2.X, v2.Y);
            return Math.Sqrt(Math.Pow(planeDistance, 2) + Math.Pow(v1.Z - v2.Z, 2));
        }

        // 两条线段间的距离
        public static double SpatialDistance(IPoint line1S, IPoint line1E, IPoint line2S, IPoint line2E)
        {
            // 如果两线段相交，则距离为 0
            bool isCross = TopoCalculator.IsCross(line1S, line1E, line2S, line2E);
            if (isCross) return 0;

            IPoint line1SInLine2 = CrossPoint2(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1S.GetX(), line1S.GetY());
            IPoint line1EInLine2 = CrossPoint2(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1E.GetX(), line1E.GetY());
            IPoint line2SInLine1 = CrossPoint2(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2S.GetX(), line2S.GetY());
            IPoint line2EInLine1 = CrossPoint2(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2E.GetX(), line2E.GetY());

            // 端点都不相交
            if (line1SInLine2 == null && line1EInLine2 == null && line2SInLine1 == null && line2EInLine1 == null)
            {
                double minDistance = SpatialDistance(line1S, line2S);
                minDistance = Math.Min(minDistance, SpatialDistance(line1S, line2E));
                minDistance = Math.Min(minDistance, SpatialDistance(line1E, line2S));
                minDistance = Math.Min(minDistance, SpatialDistance(line1E, line2E));
                return minDistance;
            }
            else
            {
                double minDistance = double.MaxValue;
                if (line1SInLine2 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistance(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1S.GetX(), line1S.GetY()));
                }

                if (line1EInLine2 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistance(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1E.GetX(), line1E.GetY()));
                }

                if (line2SInLine1 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistance(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2S.GetX(), line2S.GetY()));
                }

                if (line2EInLine1 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistance(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2E.GetX(), line2E.GetY()));
                }

                return minDistance;
            }
        }

        public static double SpatialDistanceGeo(IPoint line1S, IPoint line1E, IPoint line2S, IPoint line2E)
        {
            // 如果两线段相交，则距离为 0
            bool isCross = TopoCalculator.IsCross(line1S, line1E, line2S, line2E);
            if (isCross) return 0;

            IPoint line1SInLine2 = CrossPoint2(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1S.GetX(), line1S.GetY());
            IPoint line1EInLine2 = CrossPoint2(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1E.GetX(), line1E.GetY());
            IPoint line2SInLine1 = CrossPoint2(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2S.GetX(), line2S.GetY());
            IPoint line2EInLine1 = CrossPoint2(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2E.GetX(), line2E.GetY());

            // 端点都不相交
            if (line1SInLine2 == null && line1EInLine2 == null && line2SInLine1 == null && line2EInLine1 == null)
            {
                double minDistance = SpatialDistanceGeo(line1S, line2S);
                minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line2E));
                minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1E, line2S));
                minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1E, line2E));
                return minDistance;
            }
            else
            {
                double minDistance = double.MaxValue;
                if (line1SInLine2 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1S.GetX(), line1S.GetY()));
                }

                if (line1EInLine2 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(line2S.GetX(), line2S.GetY(), line2E.GetX(), line2E.GetY(), line1E.GetX(), line1E.GetY()));
                }

                if (line2SInLine1 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2S.GetX(), line2S.GetY()));
                }

                if (line2EInLine1 != null)
                {
                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S.GetX(), line1S.GetY(), line1E.GetX(), line1E.GetY(), line2E.GetX(), line2E.GetY()));
                }

                return minDistance;
            }
        }

        public static double Distance(Color4 color1, Color4 color2)
        {
            double distance = Math.Sqrt(Math.Pow(color1.Red - color2.Red, 2)
                + Math.Pow(color1.Green - color2.Green, 2)
                + Math.Pow(color1.Blue - color2.Blue, 2)
                + Math.Pow(color1.Alpha - color2.Alpha, 2));
            return distance;
        }

        private static double InnerMinDistance(List<IEnumerable<IPoint>> partList1, List<IEnumerable<IPoint>> partList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < partList1.Count; i++)
            {
                for (int j = 0; j < partList2.Count; j++)
                {
                    List<IPoint> pointList1 = partList1[i].ToList();
                    List<IPoint> pointList2 = partList2[j].ToList();
                    for (int m = 0; m < pointList1.Count - 1; m++)
                    {
                        for (int n = 0; n < pointList2.Count - 1; n++)
                        {
                            IPoint line1S = pointList1[m];
                            IPoint line1E = pointList1[m + 1];
                            IPoint line2S = pointList2[n];
                            IPoint line2E = pointList2[n + 1];
                            minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                        }
                    }
                }
            }
            return minDistance;
        }

        private static double InnerMinDistance(List<IEnumerable<IPoint>> partList1, List<IEnumerable<IEnumerable<IPoint>>> polygonList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < partList1.Count; i++)
            {
                for (int j = 0; j < polygonList2.Count; j++)
                {
                    List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                    for (int k = 0; k < partList2.Count; k++)
                    {
                        List<IPoint> pointList1 = partList1[i].ToList();
                        List<IPoint> pointList2 = partList2[k].ToList();
                        for (int m = 0; m < pointList1.Count - 1; m++)
                        {
                            for (int n = 0; n < pointList2.Count - 1; n++)
                            {
                                IPoint line1S = pointList1[m];
                                IPoint line1E = pointList1[m + 1];
                                IPoint line2S = pointList2[n];
                                IPoint line2E = pointList2[n + 1];
                                minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                            }
                        }
                    }

                }
            }
            return minDistance;
        }

        private static double InnerMinDistance(List<IEnumerable<IEnumerable<IPoint>>> polygonList1, List<IEnumerable<IEnumerable<IPoint>>> polygonList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < polygonList1.Count; i++)
            {
                for (int j = 0; j < polygonList2.Count; j++)
                {
                    List<IEnumerable<IPoint>> partList1 = polygonList1[i].ToList();
                    List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                    for (int k = 0; k < partList1.Count; k++)
                    {
                        for (int l = 0; l < partList2.Count; l++)
                        {
                            List<IPoint> pointList1 = partList1[k].ToList();
                            List<IPoint> pointList2 = partList2[l].ToList();
                            for (int m = 0; m < pointList1.Count - 1; m++)
                            {
                                for (int n = 0; n < pointList2.Count - 1; n++)
                                {
                                    IPoint line1S = pointList1[m];
                                    IPoint line1E = pointList1[m + 1];
                                    IPoint line2S = pointList2[n];
                                    IPoint line2E = pointList2[n + 1];
                                    minDistance = Math.Min(minDistance, SpatialDistance(line1S, line1E, line2S, line2E));
                                }
                            }
                        }
                    }

                }
            }
            return minDistance;
        }

        private static double InnerMinDistanceGeo(List<IEnumerable<IPoint>> partList1, List<IEnumerable<IPoint>> partList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < partList1.Count; i++)
            {
                for (int j = 0; j < partList2.Count; j++)
                {
                    List<IPoint> pointList1 = partList1[i].ToList();
                    List<IPoint> pointList2 = partList2[j].ToList();
                    for (int m = 0; m < pointList1.Count - 1; m++)
                    {
                        for (int n = 0; n < pointList2.Count - 1; n++)
                        {
                            IPoint line1S = pointList1[m];
                            IPoint line1E = pointList1[m + 1];
                            IPoint line2S = pointList2[n];
                            IPoint line2E = pointList2[n + 1];
                            minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                        }
                    }
                }
            }
            return minDistance;
        }

        private static double InnerMinDistanceGeo(List<IEnumerable<IPoint>> partList1, List<IEnumerable<IEnumerable<IPoint>>> polygonList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < partList1.Count; i++)
            {
                for (int j = 0; j < polygonList2.Count; j++)
                {
                    List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                    for (int k = 0; k < partList2.Count; k++)
                    {
                        List<IPoint> pointList1 = partList1[i].ToList();
                        List<IPoint> pointList2 = partList2[k].ToList();
                        for (int m = 0; m < pointList1.Count - 1; m++)
                        {
                            for (int n = 0; n < pointList2.Count - 1; n++)
                            {
                                IPoint line1S = pointList1[m];
                                IPoint line1E = pointList1[m + 1];
                                IPoint line2S = pointList2[n];
                                IPoint line2E = pointList2[n + 1];
                                minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                            }
                        }
                    }

                }
            }
            return minDistance;
        }

        private static double InnerMinDistanceGeo(List<IEnumerable<IEnumerable<IPoint>>> polygonList1, List<IEnumerable<IEnumerable<IPoint>>> polygonList2)
        {
            double minDistance = double.MaxValue;
            for (int i = 0; i < polygonList1.Count; i++)
            {
                for (int j = 0; j < polygonList2.Count; j++)
                {
                    List<IEnumerable<IPoint>> partList1 = polygonList1[i].ToList();
                    List<IEnumerable<IPoint>> partList2 = polygonList2[j].ToList();
                    for (int k = 0; k < partList1.Count; k++)
                    {
                        for (int l = 0; l < partList2.Count; l++)
                        {
                            List<IPoint> pointList1 = partList1[k].ToList();
                            List<IPoint> pointList2 = partList2[l].ToList();
                            for (int m = 0; m < pointList1.Count - 1; m++)
                            {
                                for (int n = 0; n < pointList2.Count - 1; n++)
                                {
                                    IPoint line1S = pointList1[m];
                                    IPoint line1E = pointList1[m + 1];
                                    IPoint line2S = pointList2[n];
                                    IPoint line2E = pointList2[n + 1];
                                    minDistance = Math.Min(minDistance, SpatialDistanceGeo(line1S, line1E, line2S, line2E));
                                }
                            }
                        }
                    }

                }
            }
            return minDistance;
        }

        public static Vector3D CrossPoint3D(Vector3D segmentStartLoc, Vector3D segmentEndLoc, Vector3D pointLoc)
        {
            Vector3D x1x2 = (segmentEndLoc - segmentStartLoc).Normalize();
            Vector3D x1x0 = (pointLoc - segmentStartLoc).Normalize();

            double a = (x1x0.X + x1x0.Y + x1x0.Z) / (x1x2.X * x1x2.X + x1x2.Y * x1x2.Y + x1x2.Z * x1x2.Z);

            return segmentStartLoc + a * x1x2;
        }
    }
}
