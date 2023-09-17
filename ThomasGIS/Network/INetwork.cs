using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public interface INetwork
    {
        bool AddNode(INetworkNode newNode);

        bool AddNodes(IEnumerable<INetworkNode> nodeList);

        bool AddArc(INetworkArc newArc);

        bool AddArcs(IEnumerable<INetworkArc> arcList);

        int GetArcNumber();

        int GetNodeNumber();

        bool DeleteNodeByID(int nodeID);

        bool DeleteNodeByIndex(int index);

        bool DeleteArcByIndex(int index);

        bool DeleteArcByNodeID(int nodeID);

        NetworkType GetNetworkType();

        int Degree(int index);

        Route SolvePath(int startNodeIndex, int endNodeIndex, string powerField);

        void RefreshNeighborMatrix(string dataField = null);

        double ClusterCoefficient(int nodeIndex);

        IEnumerable<NodeCluster> CommunityDetection_GN(int maxClusterNumber);

        IEnumerable<NodeCluster> CommunityDetection_Louvain(int maxClusterNumber);

        double GlobleModularity(List<NodeCluster> communities);

        int DegreeCentrality(int nodeIndex);

        double ClosestCentrality(int nodeIndex);

        double KatzCentrality(int nodeIndex, double alpha);

        bool KatzCentrality(double alpha);

        bool DegreeCentrality();

        bool ClosestCentrality();

        bool BetweenCentrality();

        bool ClusterCoefficient();

        bool RandomWalk(int level);

        double RandomWalkLength(int level);

        INetworkNode GetNodeByIndex(int nodeIndex);

        INetworkArc GetArcByIndex(int arcIndex);

        INetworkNode GetNodeByID(int ID);
    }
}
