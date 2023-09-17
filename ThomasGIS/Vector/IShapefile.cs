using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Vector
{
    public interface IShapefile
    {
        // 写出Shapefile文件
        bool ExportShapefile(string filepath);

        // 保存文件在当前位置
        bool Save();

        // 写出为GeoJson格式
        bool ExportToGeoJson(string filepath);

        // 以WKT格式在末尾添加一个要素
        int AddFeature(string wkt, Dictionary<string, object> values);

        // 以WKT格式在末尾添加一个要素
        int AddFeature(string wkt);

        // 以GeometryBase在末尾添加一个要素
        int AddFeature(IShpGeometryBase newGeometry, Dictionary<string, object> values);

        // 以GeometryBase在末尾添加一个要素
        int AddFeature(IShpGeometryBase newGeometry);

        // 设置属性字段的数值
        bool SetValue(int index, string field, object value);

        bool SetValue2(int index, string field, byte[] value);

        // 移除指定位置的要素
        bool RemoveFeature(int index);

        // 添加空字段
        bool AddField(string name, DBFFieldType type, int length, int prediction);

        // 删除字段
        bool DeleteField(string name);

        // 获取当前文件的坐标系统
        CoordinateBase GetCoordinateRef();

        bool SetCoordinateRef(CoordinateBase newCoordinate);

        // 获取文件中的要素数量
        int GetFeatureNumber();

        IShpGeometryBase GetFeature(int index);

        bool SetFeature(int index, IShpGeometryBase newFeature);

        int GetFeatureType();

        string GetFieldValueAsString(int index, string field);

        double GetFieldValueAsDouble(int index, string field);

        int GetFieldValueAsInt(int index, string field);

        byte[] GetFieldValueAsByte(int index, string field);

        BoundaryBox GetBoundaryBox();

        IEnumerable<string> GetFieldNames();

        bool OnlySaveDBF(string outputPath);

        bool FieldExist(string fieldName);

        bool CopyFieldInformation(IShapefile otherShapefile);

        List<DBFFieldObject> GetFieldInfoList();

        IShapefile Clone();
    }

}
