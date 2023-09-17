using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Vector
{
    public interface IDataBaseFile
    {
        /// <summary>
        /// 根据行数和字段名称获取数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>返回二进制byte序列</returns>
        /// <exception cref="Exception">行数越界异常；字段不存在异常</exception>
        byte[] GetValue(int rowId, string field);

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为double
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        double GetDouble(int rowId, string field);

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为string
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        string GetString(int rowId, string field);

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为int
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        int GetInt(int rowId, string field);

        /// <summary>
        /// 根据行数和字段名称设置数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <param name="value">数据对象</param>
        /// <returns>是否设置成功</returns>
        bool SetValue(int rowId, string field, object value);

        /// <summary>
        /// 根据行数和字段名称设置二进制数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <param name="value">二进制序列</param>
        /// <returns>是否设置成功</returns>
        bool SetValue2(int rowId, string field, byte[] value);

        /// <summary>
        /// 插入一个空行
        /// </summary>
        /// <returns>空行的行号</returns>
        int InsertEmptyRow();

        /// <summary>
        /// 插入一行数据
        /// </summary>
        /// <param name="values">数据字典，仅会插入与DBF现有字段一致的数据</param>
        /// <returns>插入成功</returns>
        bool InsertRow(Dictionary<string, object> values);

        /// <summary>
        /// 根据行号删除一行数据
        /// </summary>
        /// <param name="rowId">行号</param>
        /// <returns>删除成功</returns>
        bool DeleteRow(int rowId);

        /// <summary>
        /// 向DBF添加一列数据
        /// </summary>
        /// <param name="fieldName">字段名称，不可重复，大于11字符会被自动截断</param>
        /// <param name="type">字段类型</param>
        /// <param name="length">字段长度</param>
        /// <param name="precision">字段精度</param>
        /// <returns>插入成功</returns>
        /// <exception cref="Exception">插入重复字段名异常</exception>
        bool InsertField(string fieldName, DBFFieldType type, int length, int precision);

        /// <summary>
        /// 根据字段名称删除一列数据
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>删除成功</returns>
        bool DeleteField(string fieldName);

        /// <summary>
        /// 另存为DBF文件，默认覆盖源文件
        /// </summary>
        /// <param name="filepath">可选路径</param>
        /// <returns>保存成功为true，失败为false</returns>
        bool Save(string filepath);


        /// <summary>
        /// 获取所有的字段名称
        /// </summary>
        /// <returns>字段名称序列</returns>
        IEnumerable<string> GetFieldNameList();

        /// <summary>
        /// 获取所有的字段详细信息
        /// </summary>
        /// <returns>字段详细信息序列</returns>
        IEnumerable<DBFFieldObject> GetFieldInfoList();
    }
}
