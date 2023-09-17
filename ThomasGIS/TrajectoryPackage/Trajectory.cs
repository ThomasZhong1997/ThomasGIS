using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Helpers;
using ThomasGIS.Projections;

namespace ThomasGIS.TrajectoryPackage
{
    public class Trajectory : ITrajectory
    {
        private List<GnssPoint> pointList = new List<GnssPoint>();
        private double length { get; set; }
        // 返回直角坐标系下的长度
        public double Length => GetLength();
        public string TaxiID { get; }

        public int PointNumber => pointList.Count;

        // 当前轨迹对象的坐标系统
        public CoordinateBase TrajectoryCoordinate { get; set; } = null;

        // 新建空轨迹对象需先设定该轨迹对象中轨迹点的唯一ID号，以及内部轨迹点的坐标系统
        public Trajectory(string TaxiID, CoordinateBase trajectoryCoorindate)
        {
            this.TaxiID = TaxiID;
            this.TrajectoryCoordinate = trajectoryCoorindate;
        }

        // 重载方法，通过轨迹点集合构建轨迹，轨迹点集合可以无序
        public Trajectory(string taxiID, CoordinateBase trajectoryCoordindate, List<GnssPoint> pointList)
        {
            this.TaxiID = taxiID;
            // 按Timestamp升序排序
            pointList.Sort();

            // 添加轨迹点进入List
            this.pointList.AddRange(pointList);
            // 设置坐标系统
            this.TrajectoryCoordinate = trajectoryCoordindate;
            this.length = 0;

            // 按坐标类型计算长度
            RefreshTrajectoryLength();
        }

        private bool IsPointVaild(GnssPoint point)
        {
            return point.ID == this.TaxiID ? true : false;
        }

        public bool AddPoint(GnssPoint newPoint, int index)
        {
            if (!IsPointVaild(newPoint)) throw new Exception("新增轨迹点ID与轨迹ID不一致！");

            if (index < 0 || index > PointNumber) throw new Exception("索引位置超出列表范围！");

            // 在指定位置添加轨迹点
            this.pointList.Insert(index, newPoint);
            RefreshTrajectoryLength();
            return true;
        }

        public bool AddPoint(GnssPoint newPoint)
        {
            if (!IsPointVaild(newPoint)) throw new Exception("新增轨迹点ID与轨迹ID不一致！");

            // 在末尾添加轨迹点
            this.pointList.Add(newPoint);
            GnssPoint lastPoint = this.GetPointByIndex(-1);
            if (this.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Geographic)
            {
                this.length += DistanceCalculator.SpatialDistanceGeo(lastPoint, newPoint);
            }
            else
            {
                this.length += DistanceCalculator.SpatialDistance(lastPoint, newPoint);
            }
            return true;
        }

        public GnssPoint GetPointByIndex(int index)
        {
            if (index < -PointNumber || index >= PointNumber) throw new Exception("索引位置超出列表范围！");

            if (index < 0) index += PointNumber;

            return this.pointList[index];
        }

        public bool RemovePoint(int index)
        {
            if (index < 0 || index >= PointNumber) throw new Exception("索引位置超出列表范围！");

            this.pointList.RemoveAt(index);
            return true;
        }

        private void RefreshTrajectoryLength()
        {
            this.length = 0;
            if (this.PointNumber > 1)
            {
                if (this.TrajectoryCoordinate.GetCoordinateType() == CoordinateType.Projected)
                {
                    for (int i = 0; i < pointList.Count - 1; i++)
                    {
                        this.length += DistanceCalculator.SpatialDistance(pointList[i], pointList[i + 1]);
                    }
                }
                else
                {
                    for (int i = 0; i < pointList.Count - 1; i++)
                    {
                        this.length += DistanceCalculator.SpatialDistanceGeo(pointList[i], pointList[i + 1]);
                    }
                }
            }
        }

        public IEnumerable<GnssPoint> GetPointEnumerable()
        {
            return this.pointList.AsEnumerable();
        }

        public int GetPointNumber()
        {
            return this.pointList.Count;
        }

        public CoordinateBase GetCoordinateSystem()
        {
            return this.TrajectoryCoordinate;
        }

        public bool SetCoordinateSystem(CoordinateBase coordinateSystem)
        {
            this.TrajectoryCoordinate = coordinateSystem;
            return true;
        }

        public double GetLength()
        {
            this.length = 0;
            for (int i = 0; i < this.PointNumber - 1; i++)
            {
                GnssPoint p1 = pointList[i];
                GnssPoint p2 = pointList[i + 1];
                if (this.GetCoordinateSystem().GetCoordinateType() == CoordinateType.Geographic)
                {
                    this.length += DistanceCalculator.SpatialDistanceGeo(p1, p2);
                }
                else
                {
                    this.length += DistanceCalculator.SpatialDistance(p1, p2);
                }
            }

            return this.length;
        }

        public string GetTaxiID()
        {
            return this.TaxiID;
        }

        public bool Clear()
        {
            this.pointList.Clear();
            this.length = 0;
            return true;
        }

        public bool AddPoints(IEnumerable<GnssPoint> gnssPoints)
        {
            foreach (GnssPoint gnssPoint in gnssPoints)
            {
                this.AddPoint(gnssPoint);
            }
            return true;
        }
    }
}
