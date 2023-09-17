using System;
using System.Collections.Generic;
using ThomasGIS.Geometries;
using System.Text;

namespace ThomasGIS.SpatialIndex
{
    public interface ISpatialIndex
    {
        IEnumerable<int> SearchID(IPoint point, double range);

        IEnumerable<IGeometry> SearchItem(IPoint point, double range);

        bool RemoveItem(IGeometry geometry);

        bool RefreshIndex();

        bool AddItem(IGeometry geometry);

        IGeometry GetItemByIndex(int index);

        double GetScale();
    }
}
