using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Geometries.Shapefile
{
    public interface IShpPolyline : IShpGeometryBase, IMultiLineString
    {
        // 将折线按节点切分为单线，返回一个SingleLine类型的迭代器
        IEnumerable<ISingleLine> SplitByNode();

        // 将折线按部件切分为但部件，返回一个Polyline类型的迭代器
        IEnumerable<IShpPolyline> SplitByPart();

        // 计算当前折线对象的shapefile文件中对应的字节长度
        IEnumerable<IPoint> GetPointEnumerable();

        IEnumerable<int> GetPartEnumerable();

        bool AddPart(IEnumerable<IPoint> pointList);

        int GetPartNumber();

        int GetPointNumber();

        IPoint GetPointByIndex(int index);

        // 获取部件
        IEnumerable<IPoint> GetPartByIndex(int index);

        // 删除部件
        bool RemovePart(int index);
    }
}
