using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.TrajectoryPackage
{
    public interface ITrajectory
    {
        // 在轨迹对象的第index位置插入轨迹点
        bool AddPoint(GnssPoint newPoint, int index);

        // 在轨迹对象的末尾插入轨迹点
        bool AddPoint(GnssPoint newPoint);

        // 获取第index个轨迹点的TPoint对象
        GnssPoint GetPointByIndex(int index);

        // 移除对象中的第index个轨迹点
        bool RemovePoint(int index);

        // 返回轨迹点对象列表
        IEnumerable<GnssPoint> GetPointEnumerable();

        int GetPointNumber();

        CoordinateBase GetCoordinateSystem();

        bool SetCoordinateSystem(CoordinateBase coordinateSystem);

        double GetLength();

        string GetTaxiID();

        bool Clear();

        bool AddPoints(IEnumerable<GnssPoint> gnssPoints);
    }
}
