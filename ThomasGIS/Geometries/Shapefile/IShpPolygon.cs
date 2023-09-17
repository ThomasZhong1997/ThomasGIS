using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Geometries.Shapefile
{
    public interface IShpPolygon : IShpGeometryBase, IMultiPolygon
    {
        // 计算当前面状对象的全部件面积，返回一个double类型的迭代器
        IEnumerable<double> GetArea();
        // 计算当前面对象的shapefile文件中对应的字节长度

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
