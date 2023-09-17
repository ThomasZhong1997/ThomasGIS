using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.TrajectoryPackage;
using ThomasGIS.Vector;

namespace ThomasGIS.TrajectoryPackage
{
    public interface ITrajectorySet
    {
        IShapefile ExportToShapefile(CoordinateBase targetCoordinate);

        bool AddTrajectory(ITrajectory trajectory);

        bool RemoveTrajectory(int index);

        bool SortByLength();

        IEnumerable<ITrajectory> LengthLongerThan(double length);

        IEnumerable<ITrajectory> LengthShorterThan(double length);

        IEnumerable<GnssPoint> SnapShotByTime(int timeStamp);

        BoundaryBox CalculateBoundaryBox();

        // 筛选出Destination在目标区域内的轨迹集合
        IEnumerable<ITrajectory> DestinationRegionFilter(ShpPolygon polygon);

        IEnumerable<ITrajectory> OriginRegionFilter(ShpPolygon polygon);

        IEnumerable<ITrajectory> ODRegionFilter(ShpPolygon polygon);

        IEnumerable<ITrajectory> IntersectFilter(ShpPolygon polygon);

        // 输出为文本格式，按separator分隔
        bool ExportToText(string filepath, char separator);

        ITrajectory GetTrajectoryByIndex(int index);

        // 将两个轨迹数据集中的轨迹合并
        bool AllUnion(ITrajectorySet addTrajectorySet);

        IEnumerable<ITrajectory> GetTrajectoryEnumerable();

        IEnumerable<ITrajectory> TimeLongerThan(double timeInterval);

        IEnumerable<ITrajectory> TimeShorterThan(double timeInterval);

        // 仅基于位置信息判断并切割In和Out
        bool ExtractInOutTrajectory(ShpPolygon polygon, out List<ITrajectory> inTrajectories, out List<ITrajectory> outTrajectories);

        bool ExportTrajectoryStatistics(string outputFilePath);

        int GetTrajectoryNumber();

        TrajectorySetStatistics GetStatistics();
    }
}
