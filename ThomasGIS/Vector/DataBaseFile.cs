using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using ThomasGIS.BaseConfiguration;
using System.CodeDom;


namespace ThomasGIS.Vector
{
    #region 必要数据结构

    /// <summary>
    /// DBF文件格式中定义的标准数据格式枚举
    /// </summary>
    public enum DBFFieldType
    {
        Binary,
        Char,
        Date,
        General,
        Number,
        Logical,
        Memo
    }

    /// <summary>
    /// DBF文件中的数据字段描述
    /// </summary>
    public class DBFFieldObject
    {
        public string FieldName;
        public DBFFieldType FieldType;
        public int FieldLength;
        public int FieldPrecision;

        public DBFFieldObject(string fieldName, DBFFieldType dBFFieldType, int fieldLength, int fieldPrecision)
        {
            this.FieldName = fieldName;
            this.FieldLength = fieldLength;
            this.FieldPrecision = fieldPrecision;
            this.FieldType = dBFFieldType;
        }
    }

    /// <summary>
    /// 用于读取DBF文件的数据结构
    /// </summary>
    public class DBFField
    {
        public byte[] Name { get; set; } = new byte[11] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public byte Type { get; set; } = Convert.ToByte('C');
        public byte[] Stay1 { get; set; } = new byte[4] { 0, 0, 0, 0 };
        public byte FieldLength { get; set; } = 20;
        public byte FieldPrecision { get; set; } = 0;
        public byte[] Stay2 { get; set; } = { 0, 0 };
        public byte AreaID { get; set; } = 0;
        public byte[] Stay3 { get; set; } = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
        public byte MXDMarker { get; set; } = 0;
    }

    #endregion

    public class DataBaseFile : IDataBaseFile
    {
        #region 私有成员
        private byte Version { get; }
        private byte[] ModifyTime { get; } = new byte[3];
        private int RecordNumber { get; set; }
        private short HeaderLength { get; set; }
        private short RecordLength { get; set; }
        private byte[] Unuse { get; } = new byte[2];
        private byte Undo { get; }
        private byte IVCodeMarker { get; }
        private byte[] MultiUser { get; } = new byte[12];
        private byte MXDMarker { get; }
        private byte LanguageID { get; }
        private byte[] NewInfo { get; } = new byte[2];
        private byte End1 { get; }
        private byte End2 { get; }
        private byte End3 { get; }
        private Dictionary<string, int> FieldLength { get; } = new Dictionary<string, int>();

        #endregion

        public List<DBFField> Fields { get; set; } = new List<DBFField>();
        public List<Dictionary<string, byte[]>> RawData = new List<Dictionary<string, byte[]>>();

        public int FieldCount => FieldLength.Count;

        private string Filepath = null;

        // 从配置信息中获取shp属性文件的编码类型，防止文件乱码
        private string shpEncodingType = Configuration.GetConfiguration("shapefile.encoding.type");

        #region 构造函数

        /// <summary>
        /// 构造函数，从文件中读取DBF
        /// </summary>
        /// <param name="filepath">DBF文件位置</param>
        public DataBaseFile(string filepath)
        {
            Filepath = filepath;
            using (BinaryReader dbfReader = new BinaryReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                Version = dbfReader.ReadByte();
                ModifyTime = dbfReader.ReadBytes(3);
                RecordNumber = dbfReader.ReadInt32();
                HeaderLength = dbfReader.ReadInt16();
                RecordLength = dbfReader.ReadInt16();
                Unuse = dbfReader.ReadBytes(2);
                Undo = dbfReader.ReadByte();
                IVCodeMarker = dbfReader.ReadByte();
                MultiUser = dbfReader.ReadBytes(12);
                MXDMarker = dbfReader.ReadByte();
                LanguageID = dbfReader.ReadByte();
                NewInfo = dbfReader.ReadBytes(2);

                int fieldNumber = (HeaderLength - 1) / 32 - 1;
                for (int i = 0; i < fieldNumber; i++)
                {
                    DBFField newField = new DBFField();
                    newField.Name = dbfReader.ReadBytes(11);
                    newField.Type = dbfReader.ReadByte();
                    newField.Stay1 = dbfReader.ReadBytes(4);
                    newField.FieldLength = dbfReader.ReadByte();
                    newField.FieldPrecision = dbfReader.ReadByte();
                    newField.Stay2 = dbfReader.ReadBytes(2);
                    newField.AreaID = dbfReader.ReadByte();
                    newField.Stay3 = dbfReader.ReadBytes(10);
                    newField.MXDMarker = dbfReader.ReadByte();
                    Fields.Add(newField);
                    FieldLength.Add(Encoding.GetEncoding(shpEncodingType).GetString(newField.Name).TrimEnd('\0'), newField.FieldLength);
                }

                End1 = dbfReader.ReadByte();
                // End2 = dbfReader.ReadByte();

                for (int i = 0; i < RecordNumber; i++)
                {
                    //读取控制位
                    Dictionary<string, byte[]> oneRecord = new Dictionary<string, byte[]>();
                    byte deleteFlag = dbfReader.ReadByte();
                    byte[] flagValue = new byte[1];
                    flagValue[0] = deleteFlag;
                    oneRecord.Add("deleteFlag", flagValue);
                    for (int j = 0; j < Fields.Count; j++)
                    {
                        DBFField tempField = Fields[j];
                        int length = Convert.ToInt32(tempField.FieldLength);
                        string name = Encoding.GetEncoding(shpEncodingType).GetString(tempField.Name).TrimEnd('\0');
                        byte[] innerData = dbfReader.ReadBytes(length);
                        oneRecord.Add(name, innerData);
                    }

                    RawData.Add(oneRecord);
                }
                if (dbfReader.PeekChar() != -1) 
                {
                    End3 = dbfReader.ReadByte();
                }
            }
        }

        /// <summary>
        /// 构造函数，创建一个新的空DBF文件对象
        /// </summary>
        public DataBaseFile()
        {
            Version = 3;
            DateTimeOffset nowTime = DateTimeOffset.Now;
            byte year = Convert.ToByte(nowTime.Year - 1970);
            byte month = Convert.ToByte(nowTime.Month);
            byte day = Convert.ToByte(nowTime.Day);
            ModifyTime[0] = year;
            ModifyTime[1] = month;
            ModifyTime[2] = day;
            RecordNumber = 0;
            HeaderLength = 33;
            RecordLength = 1;
            Unuse[0] = 0;
            Unuse[1] = 0;
            Undo = 0;
            IVCodeMarker = 0;
            for (int i = 0; i < 12; i++)
            {
                MultiUser[i] = 0;
            }
            MXDMarker = 0;
            LanguageID = 0;
            NewInfo[0] = 0;
            NewInfo[1] = 0;
            End1 = 13;
            End3 = 26;
        }

        #endregion

        #region 获取数据

        /// <summary>
        /// 根据行数和字段名称获取数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>返回二进制byte序列</returns>
        /// <exception cref="Exception">行数越界异常；字段不存在异常</exception>
        public byte[] GetValue(int rowId, string field)
        {
            if (rowId >= RawData.Count) throw new Exception("行索引超出范围");
            if (!FieldLength.Keys.Contains<string>(field)) throw new Exception("不存在目标列");

            int length = 0;
            FieldLength.TryGetValue(field, out length);
            byte[] result = new byte[length];
            // 用户自己添加的列长度大于11则切断为11
            if (field.Length > 11) 
            {
                field = field.Substring(0, 11);
            }
            RawData[rowId].TryGetValue(field, out result);
            return result;
        }

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为double
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        public double GetDouble(int rowId, string field)
        {
            string info = Encoding.GetEncoding(shpEncodingType).GetString(GetValue(rowId, field)).TrimEnd('\0');
            return Convert.ToDouble(info);
        }

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为string
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        public string GetString(int rowId, string field)
        {
            return Encoding.GetEncoding(shpEncodingType).GetString(GetValue(rowId, field)).TrimEnd('\0');
        }

        /// <summary>
        /// 根据行数和字段名称获取数据，并转为int
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <returns>目标数据</returns>
        public int GetInt(int rowId, string field)
        {
            string info = Encoding.GetEncoding(shpEncodingType).GetString(GetValue(rowId, field)).TrimEnd('\0');
            if (info == "") return 0;
            return Convert.ToInt32(info);
        }

        #endregion

        #region 设置数据
        /// <summary>
        /// 根据行数和字段名称设置数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <param name="value">数据对象</param>
        /// <returns>是否设置成功</returns>
        /// <exception cref="Exception">数据类型不一致异常</exception>
        public bool SetValue(int rowId, string field, object value)
        {
            try
            {
                // 若列不存在在该处已抛出异常
                byte[] originValue = GetValue(rowId, field);
                for (int i = 0; i < originValue.Length; i++)
                {
                    originValue[i] = 0;
                }
                // 此处一定存在目标列，获取目标列的数据类型，而后根据数据类型进行转换，进而得到该列的类型
                byte fieldTypeByte = Fields.Where(item => Encoding.GetEncoding(shpEncodingType).GetString(item.Name).TrimEnd('\0') == field).ToList()[0].Type;
                char type = Convert.ToChar(fieldTypeByte);

                // 依据数据类型序列化用户设置的字段值
                if (type == 'C' || type == 'D')
                {
                    // 字符串和日期类型直接按编码转码
                    byte[] insertDataStream = Encoding.GetEncoding(shpEncodingType).GetBytes(value as string);
                    for (int i = 0; i < Math.Min(originValue.Length, insertDataStream.Length); i++)
                    {
                        originValue[i] = insertDataStream[i];
                    }
                }
                else if (type == 'B')
                {
                    // 二进制提供的应当就是字节数组的形式，直接写入即可
                    byte[] insertDataStream = (byte[])value;
                    for (int i = 0; i < Math.Min(originValue.Length, insertDataStream.Length); i++)
                    {
                        originValue[i] = insertDataStream[i];
                    }
                }
                else if (type == 'N')
                {
                    // 数值类型判断数值长度与精度
                    byte fieldLengthByte = Fields.Where(item => Encoding.GetEncoding(shpEncodingType).GetString(item.Name).TrimEnd('\0') == field).ToList()[0].FieldLength;
                    byte fieldPrecisionByte = Fields.Where(item => Encoding.GetEncoding(shpEncodingType).GetString(item.Name).TrimEnd('\0') == field).ToList()[0].FieldPrecision;
                    int fieldLength = Convert.ToInt32(fieldLengthByte);
                    int fieldPrecision = Convert.ToInt32(fieldPrecisionByte);

                    // 如果fieldPrecision是0，表示是整形
                    if (fieldPrecision == 0)
                    {
                        long insertData = Convert.ToInt64(value);
                        string insertDataStr = insertData.ToString();
                        byte[] insertDataStream = Encoding.GetEncoding(shpEncodingType).GetBytes(insertDataStr);
                        for (int i = 0; i < Math.Min(originValue.Length, insertDataStream.Length); i++)
                        {
                            originValue[i] = insertDataStream[i];
                        }
                    }
                    else
                    {
                        double insertData = Convert.ToDouble(value);
                        string insertDataStr = insertData.ToString(string.Format("f{0}", fieldPrecision));
                        byte[] insertDataStream = Encoding.GetEncoding(shpEncodingType).GetBytes(insertDataStr);
                        for (int i = 0; i < Math.Min(originValue.Length, insertDataStream.Length); i++)
                        {
                            originValue[i] = insertDataStream[i];
                        }
                    }
                }
                else
                {
                    // 其余类型转为二进制
                    BinaryFormatter formatter = new BinaryFormatter();
                    MemoryStream stream = new MemoryStream();
                    formatter.Serialize(stream, value);
                    byte[] insertDataStream = stream.GetBuffer();
                    for (int i = 0; i < Math.Min(originValue.Length, insertDataStream.Length); i++)
                    {
                        originValue[i] = insertDataStream[i];
                    }
                }
                RawData[rowId][field] = originValue;
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw new Exception("属性表数据格式与输入数据格式不一致");
            }
        }


        /// <summary>
        /// 根据行数和字段名称设置二进制数据
        /// </summary>
        /// <param name="rowId">行数</param>
        /// <param name="field">字段名称</param>
        /// <param name="value">二进制序列</param>
        /// <returns>是否设置成功</returns>
        public bool SetValue2(int rowId, string field, byte[] value)
        {
            // 若列不存在在该处已抛出异常
            byte[] originValue = GetValue(rowId, field);
            for (int i = 0; i < originValue.Length; i++)
            {
                originValue[i] = 0;
            }
            for (int i = 0; i < Math.Min(originValue.Length, value.Length); i++)
            {
                originValue[i] = value[i];
            }
            return true;
        }

        #endregion

        #region DBF操作

        /// <summary>
        /// 插入一个空行
        /// </summary>
        /// <returns>空行的行号</returns>
        public int InsertEmptyRow()
        {
            Dictionary<string, byte[]> newRow = new Dictionary<string, byte[]>();
            for (int i = 0; i < Fields.Count; i++)
            {
                string fieldName = Encoding.GetEncoding(shpEncodingType).GetString(Fields[i].Name).TrimEnd('\0');
                int fieldLength = Convert.ToInt32(Fields[i].FieldLength);
                newRow.Add(fieldName, new byte[fieldLength]);
            }
            newRow.Add("deleteFlag", new byte[] { 0x20 });
            RawData.Add(newRow);
            RecordNumber += 1;
            return RawData.Count - 1;
        }

        /// <summary>
        /// 插入一行数据
        /// </summary>
        /// <param name="values">数据字典，仅会插入与DBF现有字段一致的数据</param>
        /// <returns>插入成功</returns>
        public bool InsertRow(Dictionary<string, object> values)
        {
            int rowId = InsertEmptyRow();
            foreach (string key in values.Keys)
            {
                SetValue(rowId, key, values[key]);
            }

            return true;
        }

        /// <summary>
        /// 向DBF添加一列数据
        /// </summary>
        /// <param name="fieldName">字段名称，不可重复，大于11字符会被自动截断</param>
        /// <param name="type">字段类型</param>
        /// <param name="length">字段长度</param>
        /// <param name="precision">字段精度</param>
        /// <returns>插入成功</returns>
        /// <exception cref="Exception">插入重复字段名异常</exception>
        public bool InsertField(string fieldName, DBFFieldType type, int length, int precision)
        {
            // 检验是否有重名字段，如果有则不可插入
            if (FieldLength.ContainsKey(fieldName))
            {
                throw new Exception("新建字段时请勿使用重复的字段名！");
            }

            // 构建新的DBField对象
            DBFField field = new DBFField();
            // 将字段名称转为二进制字节流
            byte[] provideName = Encoding.GetEncoding(shpEncodingType).GetBytes(fieldName);
            // 名称最长为11位字节
            for (int i = 0; i < Math.Min(11, provideName.Length); i++)
            {
                field.Name[i] = provideName[i];
            }
            // 依据需求的字段类型写入field属性
            switch (type)
            {
                case DBFFieldType.Binary:
                    field.Type = Convert.ToByte('B');
                    break;
                case DBFFieldType.Char:
                    field.Type = Convert.ToByte('C');
                    break;
                case DBFFieldType.Date:
                    field.Type = Convert.ToByte('D');
                    break;
                case DBFFieldType.General:
                    field.Type = Convert.ToByte('G');
                    break;
                case DBFFieldType.Number:
                    field.Type = Convert.ToByte('N');
                    break;
                case DBFFieldType.Logical:
                    field.Type = Convert.ToByte('L');
                    break;
                case DBFFieldType.Memo:
                    field.Type = Convert.ToByte('M');
                    break;
                default:
                    return false;
            }
            // 写入字段的长度与精度
            field.FieldLength = Convert.ToByte(length);
            field.FieldPrecision = Convert.ToByte(precision);

            // 加入字段列表
            FieldLength.Add(fieldName, length);
            Fields.Add(field);

            // 如果是Number类型则自动占用8个字节（双精度和长整型都是64位，8byte），其余按照用户设定即可
            for (int i = 0; i < RecordNumber; i++)
            {
                RawData[i].Add(fieldName, new byte[length]);
            }

            // 增加一列后头文件长度+32，每条记录的长度+length
            HeaderLength += 32;
            RecordLength += (short)length;

            return true;
        }

        /// <summary>
        /// 根据字段名称删除一列数据
        /// </summary>
        /// <param name="fieldName">字段名称</param>
        /// <returns>删除成功</returns>
        public bool DeleteField(string fieldName)
        {
            if (!FieldLength.ContainsKey(fieldName)) return false;
            var items = Fields.Where(item => Encoding.GetEncoding(shpEncodingType).GetString(item.Name).TrimEnd('\0') == fieldName).ToList();
            Fields.Remove(items[0]);
            RecordLength -= (short)FieldLength[fieldName];
            FieldLength.Remove(fieldName);
            HeaderLength -= 32;
            for (int i = 0; i < RecordNumber; i++)
            {
                RawData[i].Remove(fieldName);
            }
            return true;
        }

        /// <summary>
        /// 根据行号删除一行数据
        /// </summary>
        /// <param name="rowId">行号</param>
        /// <returns>删除成功</returns>
        public bool DeleteRow(int rowId)
        {
            if (rowId > RecordNumber) return false;
            RawData.RemoveAt(rowId);
            RecordNumber -= 1;
            return true;
        }

        #endregion

        #region 文件操作

        /// <summary>
        /// 输出为DBF文件至文件系统
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <returns>输出成功</returns>
        private bool ExportFile(string filepath)
        {
            using (BinaryWriter dbfWriter = new BinaryWriter(new FileStream(filepath, FileMode.Create)))
            {
                dbfWriter.Write(Version);
                dbfWriter.Write(ModifyTime);
                dbfWriter.Write(RecordNumber);
                dbfWriter.Write(HeaderLength);
                dbfWriter.Write(RecordLength);
                dbfWriter.Write(Unuse);
                dbfWriter.Write(Undo);
                dbfWriter.Write(IVCodeMarker);
                dbfWriter.Write(MultiUser);
                dbfWriter.Write(MXDMarker);
                dbfWriter.Write(LanguageID);
                dbfWriter.Write(NewInfo);

                for (int i = 0; i < FieldCount; i++)
                {
                    DBFField field = Fields[i];
                    dbfWriter.Write(field.Name);
                    dbfWriter.Write(field.Type);
                    dbfWriter.Write(field.Stay1);
                    dbfWriter.Write(field.FieldLength);
                    dbfWriter.Write(field.FieldPrecision);
                    dbfWriter.Write(field.Stay2);
                    dbfWriter.Write(field.AreaID);
                    dbfWriter.Write(field.Stay3);
                    dbfWriter.Write(field.MXDMarker);
                }

                dbfWriter.Write(End1);
                // dbfWriter.Write(End2);

                for (int i = 0; i < RecordNumber; i++)
                {
                    dbfWriter.Write(RawData[i]["deleteFlag"], 0, 1);
                    for (int j = 0; j < Fields.Count; j++)
                    {
                        string key = Encoding.GetEncoding(shpEncodingType).GetString(Fields[i].Name).TrimEnd('\0');
                        byte[] data = RawData[i][key];
                        int length = FieldLength[key];
                        dbfWriter.Write(data, 0, length);
                    }
                }
                dbfWriter.Write(End3);
                dbfWriter.Flush();
                dbfWriter.Close();
            }
            return true;
        }

        /// <summary>
        /// 另存为DBF文件，默认覆盖源文件
        /// </summary>
        /// <param name="filepath">可选路径</param>
        /// <returns>保存成功为true，失败为false</returns>
        public bool Save(string filepath = null)
        {
            if (filepath == null && Filepath == null) return false;

            if (filepath != null) Filepath = filepath;

            return ExportFile(Filepath);
        }

        #endregion

        #region 字段操作
        /// <summary>
        /// 获取所有的字段名称
        /// </summary>
        /// <returns>字段名称序列</returns>
        public IEnumerable<string> GetFieldNameList()
        {
            List<string> result = new List<string>();
            foreach (string key in this.FieldLength.Keys)
            {
                result.Add(key);
            }
            return result;
        }

        /// <summary>
        /// 获取所有的字段详细信息
        /// </summary>
        /// <returns>字段详细信息序列</returns>
        public IEnumerable<DBFFieldObject> GetFieldInfoList()
        {
            List<DBFFieldObject> result = new List<DBFFieldObject>();
            for (int i = 0; i < Fields.Count; i++)
            {
                DBFField oneField = Fields[i];
                string fieldName = Encoding.GetEncoding(shpEncodingType).GetString(oneField.Name).TrimEnd('\0');
                byte fieldType = oneField.Type;
                DBFFieldType dBFFieldType = DBFFieldType.Binary;
                switch (fieldType)
                {
                    case 'C' + 0:
                        dBFFieldType = DBFFieldType.Char;
                        break;
                    case 'N' + 0:
                        dBFFieldType = DBFFieldType.Number;
                        break;
                    case 'B' + 0:
                        dBFFieldType = DBFFieldType.Binary;
                        break;
                    case 'D' + 0:
                        dBFFieldType = DBFFieldType.Date;
                        break;
                    case 'M' + 0:
                        dBFFieldType = DBFFieldType.Memo;
                        break;
                    case 'L' + 0:
                        dBFFieldType = DBFFieldType.Logical;
                        break;
                    case 'G' + 0:
                        dBFFieldType = DBFFieldType.General;
                        break;

                }
                int fieldLength = Convert.ToInt32(oneField.FieldLength);
                int fieldPrecision = Convert.ToInt32(oneField.FieldPrecision);
                DBFFieldObject oneFieldObject = new DBFFieldObject(fieldName, dBFFieldType, fieldLength, fieldPrecision);
                result.Add(oneFieldObject);
            }
            return result;
        }

        #endregion
    }
}
