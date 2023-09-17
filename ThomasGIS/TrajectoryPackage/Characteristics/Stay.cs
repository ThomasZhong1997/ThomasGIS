using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System.Linq;
using ThomasGIS.Helpers;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.TrajectoryPackage.Characteristics
{
    public enum StayAreaType
    {
        Rectangle,
        Detail
    }

    public class Stay : ShpPolygon, IStay
    {
        private string taxiID;

        private double startTimestamp;

        private double endTimestamp;

        private CoordinateBase coordinate = null;

        private List<GnssPoint> stayPointList = new List<GnssPoint>();

        private int prevMoveID;

        private int nextMoveID;

        public int PrevMoveID => prevMoveID;

        public int NextMoveID => nextMoveID;

        public Stay(string taxiID, CoordinateBase coordinate, IEnumerable<GnssPoint> pointList, int prevMoveID, int nextMoveID, StayAreaType type = StayAreaType.Rectangle) : base()
        {
            this.taxiID = taxiID;
            this.startTimestamp = pointList.First().Timestamp;
            this.endTimestamp = pointList.Last().Timestamp;
            this.coordinate = coordinate;
            this.stayPointList.AddRange(pointList);
            this.prevMoveID = prevMoveID;
            this.nextMoveID = nextMoveID;

            // 按矩形计算或者按点集凸包计算
            if (pointList.Count() > 2)
            {
                if (type == StayAreaType.Rectangle)
                {
                    double xmin = pointList.Min(item => item.GetX());
                    double ymin = pointList.Min(item => item.GetY());
                    double xmax = pointList.Max(item => item.GetX());
                    double ymax = pointList.Max(item => item.GetY());

                    if (coordinate.GetCoordinateType() == CoordinateType.Geographic)
                    {
                        if (xmin == xmax)
                        {
                            xmax += 0.0000001;
                            xmin -= 0.0000001;
                        }
                        if (ymin == ymax)
                        {
                            ymax += 0.0000001;
                            ymin -= 0.0000001;
                        }
                    }
                    else
                    {
                        if (xmin == xmax)
                        {
                            xmax += 5;
                            xmin -= 5;
                        }
                        if (ymin == ymax)
                        {
                            ymax += 5;
                            ymin -= 5;
                        }
                    }

                    List<Point> areaPolygonPoints = new List<Point>();

                    areaPolygonPoints.Add(new Point(xmin, ymin));
                    areaPolygonPoints.Add(new Point(xmin, ymax));
                    areaPolygonPoints.Add(new Point(xmax, ymax));
                    areaPolygonPoints.Add(new Point(xmax, ymin));
                    areaPolygonPoints.Add(new Point(xmin, ymin));

                    this.AddPart(areaPolygonPoints);
                }
                else
                {
                    IEnumerable<IPoint> areaPolygonPoints = TopoCalculator.GrahamCreateBoundary(PointList);
                    this.AddPart(areaPolygonPoints);
                }
            }
            // 只有两个点只可能算出一个Rectangle
            else
            {
                double xmin = pointList.Min(item => item.GetX());
                double ymin = pointList.Min(item => item.GetY());
                double xmax = pointList.Max(item => item.GetX());
                double ymax = pointList.Max(item => item.GetY());

                if (coordinate.GetCoordinateType() == CoordinateType.Geographic)
                {
                    if (xmin == xmax)
                    {
                        xmax += 0.0000001;
                        xmin -= 0.0000001;
                    }
                    if (ymin == ymax)
                    {
                        ymax += 0.0000001;
                        ymin -= 0.0000001;
                    }
                }
                else
                {
                    if (xmin == xmax)
                    {
                        xmax += 5;
                        xmin -= 5;
                    }
                    if (ymin == ymax)
                    {
                        ymax += 5;
                        ymin -= 5;
                    }
                }
 

                List<Point> areaPolygonPoints = new List<Point>();

                areaPolygonPoints.Add(new Point(xmin, ymin));
                areaPolygonPoints.Add(new Point(xmin, ymax));
                areaPolygonPoints.Add(new Point(xmax, ymax));
                areaPolygonPoints.Add(new Point(xmax, ymin));
                areaPolygonPoints.Add(new Point(xmin, ymin));

                this.AddPart(areaPolygonPoints);
            }
        }

        public new double GetArea()
        {
            return base.GetArea().First();
        }

        public double GetEndTime()
        {
            return this.endTimestamp;
        }

        public double GetStartTime()
        {
            return this.startTimestamp;
        }

        public string GetID()
        {
            return this.taxiID;
        }

        public IEnumerable<GnssPoint> GetStayPoints()
        {
            return this.stayPointList;
        }
    }
}
