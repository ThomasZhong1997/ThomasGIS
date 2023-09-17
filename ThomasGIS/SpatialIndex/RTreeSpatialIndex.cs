using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.SpatialIndex
{
    // 基于 B 树实现R树
    // 实现 R 树前需要先实现几个拓扑判断
    public class RTreeNode
    {
        bool[] flagList = null;
        BoundaryBox nodeBoundaryBox;
        public RTreeNode(int nodeSize) 
        { 

        }
    }

    public class RTreeSpatialIndex : ISpatialIndex
    {
        private List<IGeometry> innerData;
        private int nodeSize;

        public RTreeSpatialIndex()
        {
            innerData = new List<IGeometry>();
            nodeSize = Convert.ToInt32(Configuration.GetConfiguration("spatialindex.rtree.node.size"));

        }

        bool ISpatialIndex.AddItem(IGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public IGeometry GetItemByIndex(int index)
        {
            throw new NotImplementedException();
        }

        public double GetScale()
        {
            throw new NotImplementedException();
        }

        public bool RefreshIndex()
        {
            throw new NotImplementedException();
        }

        public bool RemoveItem(IGeometry geometry)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<int> SearchID(IPoint point, double range)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IGeometry> SearchItem(IPoint point, double range)
        {
            throw new NotImplementedException();
        }
    }
}
