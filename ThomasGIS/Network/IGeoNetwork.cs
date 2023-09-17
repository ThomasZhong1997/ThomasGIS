using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Vector;

namespace ThomasGIS.Network
{
    public interface IGeoNetwork : INetwork
    {
        // 将节点输出为点状shapefile
        IShapefile ExportNodesToShapefile();
        // 将边输出为线状shapefile
        IShapefile ExportArcsToShapefile();

        bool LinkNodes_KLink_MaxDistance(double maxTolerateDistance, int KLinks, Func<Dictionary<string, object>, Dictionary<string, object>, int> judgeFunction);

        bool LinkNodes_Delaunary_Triangle(Func<Dictionary<string, object>, Dictionary<string, object>, int> judgeFunction, double tolerateDistance);

        IEnumerable<NodeCluster> CommunityDetection_SpatialDBScan(double maxDistance, double maxNodeCount);

        GeoNetworkArc GetGeoNetworkArc(int index);

        GeoNetworkNode GetGeoNetworkNode(int index);

        GeoNetworkNode GetGeoNetworkNodeByID(int ID);

        CoordinateBase GetGeoNetworkCoordinate();

        int GetGeoNetworkNodeIndex(int ID);

        Route SolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance);

        Route GreedySolvePath(int startNodeIndex, int endNodeIndex, string powerField);

        Route GreedySolvePath(IPoint startPoint, IPoint endPoint, string powerField, double tolerateDistance);

        BoundaryBox GetBoundaryBox();

        bool DelaunaryTriangle();
    }
}
