using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Network;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Vector
{
    /// <summary>
    /// 目前支持的ESRI Shapefile类型
    /// </summary>
    public enum ESRIShapeType 
    {
        None = 0,
        Point = 1,
        Polyline = 3,
        Polygon = 5,
        SingleLine = 2,
        PointZ = 11,
        PolylineZ = 13,
        PolygonZ = 15,
    }

    /// <summary>
    /// Shapefile类
    /// </summary>
    public class Shapefile : IShapefile
    {
        #region 私有成员
        private string filePath = "";
        private VectorOpenMode openMode { get; set; } = VectorOpenMode.Common;
        private int FileCode { get; } = 9994;
        private int FileLength { get; set; } = 0;
        private int Version { get; } = 1000;
        private int ShapeType { get; } = 1;
        private double XMin { get; set; } = 0;
        private double YMin { get; set; } = 0;
        private double XMax { get; set; } = 0;
        private double YMax { get; set; } = 0;
        private double ZMin { get; set; } = 0;
        private double ZMax { get; set; } = 0;
        private double MMin { get; set; } = 0;
        private double MMax { get; set; } = 0;

        // 属性表
        private DataBaseFile PropertySheet { get; set; } = null;

        // 几何对象列表
        private List<IShpGeometryBase> innerGeometryList = new List<IShpGeometryBase>();

        // 坐标系统
        private CoordinateBase CoordinateSystem { get; set; } = null;

        #endregion

        /// <summary>
        /// 公开的几何对象序列
        /// </summary>
        public List<IShpGeometryBase> GeometryList => innerGeometryList ?? new List<IShpGeometryBase>();

        #region 构造函数

        public Shapefile(string filepath, int unUsed)
        {
            this.filePath = filepath;

            string shpPath = filepath;
            string shxPath = filepath.Substring(0, filepath.Length - 4) + ".shx";
            string dbfPath = filepath.Substring(0, filepath.Length - 4) + ".dbf";
            string prjPath = filepath.Substring(0, filepath.Length - 4) + ".prj";

            bool shpExist = File.Exists(shpPath);
            bool shxExist = File.Exists(shxPath);
            bool dbfExist = File.Exists(dbfPath);
            bool prjExist = File.Exists(prjPath);

            if (!(shpExist && shxExist && dbfExist)) throw new Exception("ESRI Shapefile基本三要素不完整！");

            if (!prjExist)
            {
                Console.WriteLine("无投影文件，若需要请自行定义投影！");
            }
            else
            {
                this.CoordinateSystem = CoordinateGenerator.ParseFromFile(prjPath);
            }

            BinaryReader shpReader = new BinaryReader(new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read));
            FileCode = SwapInt(shpReader.ReadInt32());
            for (int i = 0; i < 5; i++)
            {
                int unuse = SwapInt(shpReader.ReadInt32());
            }
            FileLength = SwapInt(shpReader.ReadInt32());
            Version = shpReader.ReadInt32();
            ShapeType = shpReader.ReadInt32();
            XMin = shpReader.ReadDouble();
            YMin = shpReader.ReadDouble();
            XMax = shpReader.ReadDouble();
            YMax = shpReader.ReadDouble();
            ZMin = shpReader.ReadDouble();
            ZMax = shpReader.ReadDouble();
            MMin = shpReader.ReadDouble();
            MMax = shpReader.ReadDouble();

            List<int> offsetList = new List<int>();
            List<int> contentLengthList = new List<int>();
            // 读入.shx文件
            using (BinaryReader shxReader = new BinaryReader(new FileStream(shxPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                FileCode = SwapInt(shxReader.ReadInt32());
                for (int i = 0; i < 5; i++)
                {
                    int unuse = SwapInt(shxReader.ReadInt32());
                }
                int shxFileLength = SwapInt(shxReader.ReadInt32());
                Version = shxReader.ReadInt32();
                ShapeType = shxReader.ReadInt32();
                XMin = shxReader.ReadDouble();
                YMin = shxReader.ReadDouble();
                XMax = shxReader.ReadDouble();
                YMax = shxReader.ReadDouble();
                ZMin = shxReader.ReadDouble();
                ZMax = shxReader.ReadDouble();
                MMin = shxReader.ReadDouble();
                MMax = shxReader.ReadDouble();

                while (shxReader.PeekChar() > -1)
                {
                    int offset = SwapInt(shxReader.ReadInt32());
                    int contentLength = SwapInt(shxReader.ReadInt32());
                    offsetList.Add(offset * 2);
                    contentLengthList.Add(contentLength * 2);
                }
            }

            int trueFileLength = FileLength * 2;

            for (int i = 0; i < offsetList.Count; i++)
            {
                int startByteOffset = offsetList[i];
                int endByteOffset;
                if (i < offsetList.Count - 1)
                {
                    endByteOffset = offsetList[i + 1];
                }
                else
                {
                    endByteOffset = trueFileLength;
                }

                if (startByteOffset + contentLengthList[i] + 8 != endByteOffset)
                {
                    continue;
                }

                shpReader.BaseStream.Position = startByteOffset;
                SwapInt(shpReader.ReadInt32());
                SwapInt(shpReader.ReadInt32());

                int recordType = shpReader.ReadInt32();

                if (recordType == 0)
                {
                    innerGeometryList.Add(new ShpNone());
                }

                if (recordType == 1)
                {
                    double x = shpReader.ReadDouble();
                    double y = shpReader.ReadDouble();

                    if (4 + 8 + 8 != contentLengthList[i])
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    ShpPoint newPoint = new ShpPoint(x, y);
                    innerGeometryList.Add(newPoint);
                }

                if (recordType == 3)
                {
                    double recordXMin = shpReader.ReadDouble();
                    double recordYMin = shpReader.ReadDouble();
                    double recordXMax = shpReader.ReadDouble();
                    double recordYMax = shpReader.ReadDouble();
                    BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                    int partNumber = shpReader.ReadInt32();
                    int pointNumber = shpReader.ReadInt32();

                    int featureTotalLength = partNumber * 4 + pointNumber * 16 + 32 + 4 + 8;

                    if (featureTotalLength != contentLengthList[i])
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    List<int> partList = new List<int>();
                    for (int j = 0; j < partNumber; j++)
                    {
                        partList.Add(shpReader.ReadInt32());
                    }
                    List<Point> pointList = new List<Point>();
                    for (int j = 0; j < pointNumber; j++)
                    {
                        pointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                    }
                    innerGeometryList.Add(new ShpPolyline(pointList, partList, recordMBR));
                }

                if (recordType == 5)
                {
                    double recordXMin = shpReader.ReadDouble();
                    double recordYMin = shpReader.ReadDouble();
                    double recordXMax = shpReader.ReadDouble();
                    double recordYMax = shpReader.ReadDouble();
                    BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                    int partNumber = shpReader.ReadInt32();
                    int pointNumber = shpReader.ReadInt32();

                    int featureTotalLength = partNumber * 4 + pointNumber * 16 + 32 + 4 + 8;

                    if (featureTotalLength != contentLengthList[i])
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    List<int> partList = new List<int>();
                    for (int j = 0; j < partNumber; j++)
                    {
                        partList.Add(shpReader.ReadInt32());
                    }
                    List<Point> pointList = new List<Point>();
                    for (int j = 0; j < pointNumber; j++)
                    {
                        pointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                    }
                    innerGeometryList.Add(new ShpPolygon(pointList, partList, recordMBR));
                }

                if (recordType == 11)
                {
                    int featureTotalLength = 4 + 32;
                    int featureTotalLengthWithoutM = 4 + 24;

                    int readType = 0;
                    if (featureTotalLength == contentLengthList[i])
                    {
                        readType = 1;
                    }
                    else if (featureTotalLengthWithoutM == contentLengthList[i])
                    {
                        readType = 2;
                    }
                    else
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    double x = shpReader.ReadDouble();
                    double y = shpReader.ReadDouble();
                    double z = shpReader.ReadDouble();
                    double m;
                    if (readType == 1)
                    {
                        m = shpReader.ReadDouble();
                    }
                    else
                    {
                        m = 0;
                    }
                    ShpPoint3D newPoint = new ShpPoint3D(x, y, z, m);
                    innerGeometryList.Add(newPoint);
                }

                if (recordType == 13)
                {
                    double recordXMin = shpReader.ReadDouble();
                    double recordYMin = shpReader.ReadDouble();
                    double recordXMax = shpReader.ReadDouble();
                    double recordYMax = shpReader.ReadDouble();
                    BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                    ShpPolyline3D shpPolyline3D = new ShpPolyline3D();
                    shpPolyline3D.BoundaryBox = recordMBR;
                    int partNumber = shpReader.ReadInt32();
                    int pointNumber = shpReader.ReadInt32();

                    int featureTotalLength = partNumber * 4 + pointNumber * 32 + 32 + 4 + 32 + 8;
                    int featureTotalLengthWithoutM = partNumber * 4 + pointNumber * 24 + 32 + 4 + 16 + 8;
                    int readType = 0;
                    if (featureTotalLength == contentLengthList[i])
                    {
                        readType = 1;
                    }
                    else if (featureTotalLengthWithoutM == contentLengthList[i])
                    {
                        readType = 2;
                    }
                    else
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    for (int j = 0; j < partNumber; j++)
                    {
                        shpPolyline3D.PartList.Add(shpReader.ReadInt32());
                    }
                    for (int j = 0; j < pointNumber; j++)
                    {
                        shpPolyline3D.PointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                    }

                    shpPolyline3D.ZMin = shpReader.ReadDouble();
                    shpPolyline3D.ZMax = shpReader.ReadDouble();
                    for (int j = 0; j < pointNumber; j++)
                    {
                        shpPolyline3D.ZList.Add(shpReader.ReadDouble());
                    }

                    if (readType == 1)
                    {
                        shpPolyline3D.MMin = shpReader.ReadDouble();
                        shpPolyline3D.MMax = shpReader.ReadDouble();
                        for (int j = 0; j < pointNumber; j++)
                        {
                            shpPolyline3D.MList.Add(shpReader.ReadDouble());
                        }
                    }
                    else
                    {
                        shpPolyline3D.MMin = 0;
                        shpPolyline3D.MMax = 0;
                        for (int j = 0; j < pointNumber; j++)
                        {
                            shpPolyline3D.MList.Add(0);
                        }
                    }

                    innerGeometryList.Add(shpPolyline3D);
                }

                if (recordType == 15)
                {
                    double recordXMin = shpReader.ReadDouble();
                    double recordYMin = shpReader.ReadDouble();
                    double recordXMax = shpReader.ReadDouble();
                    double recordYMax = shpReader.ReadDouble();
                    BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                    ShpPolygon3D shpPolygon3D = new ShpPolygon3D();
                    shpPolygon3D.BoundaryBox = recordMBR;
                    int partNumber = shpReader.ReadInt32();
                    int pointNumber = shpReader.ReadInt32();

                    int featureTotalLength = partNumber * 4 + pointNumber * 32 + 32 + 4 + 32 + 8;
                    int featureTotalLengthWithoutM = partNumber * 4 + pointNumber * 24 + 32 + 4 + 16 + 8;
                    int readType = 0;
                    if (featureTotalLength == contentLengthList[i])
                    {
                        readType = 1;
                    }
                    else if (featureTotalLengthWithoutM == contentLengthList[i])
                    {
                        readType = 2;
                    }
                    else
                    {
                        throw new Exception("Shapefile Error!");
                    }

                    for (int j = 0; j < partNumber; j++)
                    {
                        shpPolygon3D.PartList.Add(shpReader.ReadInt32());
                    }
                    for (int j = 0; j < pointNumber; j++)
                    {
                        shpPolygon3D.PointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                    }

                    shpPolygon3D.ZMin = shpReader.ReadDouble();
                    shpPolygon3D.ZMax = shpReader.ReadDouble();
                    for (int j = 0; j < pointNumber; j++)
                    {
                        shpPolygon3D.ZList.Add(shpReader.ReadDouble());
                    }

                    if (readType == 1)
                    {
                        shpPolygon3D.MMin = shpReader.ReadDouble();
                        shpPolygon3D.MMax = shpReader.ReadDouble();
                        for (int j = 0; j < pointNumber; j++)
                        {
                            shpPolygon3D.MList.Add(shpReader.ReadDouble());
                        }
                    }
                    else
                    {
                        shpPolygon3D.MMin = 0;
                        shpPolygon3D.MMax = 0;
                        for (int j = 0; j < pointNumber; j++)
                        {
                            shpPolygon3D.MList.Add(0);
                        }
                    }
  

                    innerGeometryList.Add(shpPolygon3D);
                }
            }

            PropertySheet = new DataBaseFile(dbfPath);
        }

        /// <summary>
        /// 读取并解析Shapefile文件
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <param name="mode">读取模式，一般无需更改，OnlyShp表示仅读取.shp文件用于校验文件正确性</param>
        /// <exception cref="Exception">Shapefile三要素不完整异常</exception>
        public Shapefile(string filepath, VectorOpenMode mode = VectorOpenMode.Common)
        {
            this.filePath = filepath;

            string shpPath = filepath;
            string shxPath = filepath.Substring(0, filepath.Length - 4) + ".shx";
            string dbfPath = filepath.Substring(0, filepath.Length - 4) + ".dbf";
            string prjPath = filepath.Substring(0, filepath.Length - 4) + ".prj";

            bool shpExist = File.Exists(shpPath);
            bool shxExist = File.Exists(shxPath);
            bool dbfExist = File.Exists(dbfPath);
            bool prjExist = File.Exists(prjPath);

            if (mode != VectorOpenMode.ShpOnly)
            {
                if (!(shpExist && shxExist && dbfExist)) throw new Exception("ESRI Shapefile基本三要素不完整！");
            }
            else
            {
                Console.WriteLine("该模式仅读取shp文件，无法执行属性表操作!");
            }

            if (!prjExist)
            {
                Console.WriteLine("无投影文件，若需要请自行定义投影！");
            }
            else
            {
                this.CoordinateSystem = CoordinateGenerator.ParseFromFile(prjPath);
            }

            using (BinaryReader shpReader = new BinaryReader(new FileStream(shpPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                FileCode = SwapInt(shpReader.ReadInt32());
                for (int i = 0; i < 5; i++) 
                {
                    int unuse = SwapInt(shpReader.ReadInt32());
                }
                FileLength = SwapInt(shpReader.ReadInt32());
                Version = shpReader.ReadInt32();
                ShapeType = shpReader.ReadInt32();
                XMin = shpReader.ReadDouble();
                YMin = shpReader.ReadDouble();
                XMax = shpReader.ReadDouble();
                YMax = shpReader.ReadDouble();
                ZMin = shpReader.ReadDouble();
                ZMax = shpReader.ReadDouble();
                MMin = shpReader.ReadDouble();
                MMax = shpReader.ReadDouble();


                while (shpReader.PeekChar() > -1)
                {
                    int recordNumber = SwapInt(shpReader.ReadInt32());
                    int recordLength = SwapInt(shpReader.ReadInt32());
                    int recordType = shpReader.ReadInt32();

                    // 点
                    if (ShapeType == 1)
                    {
                        double x = shpReader.ReadDouble();
                        double y = shpReader.ReadDouble();
                        ShpPoint newPoint = new ShpPoint(x, y);
                        innerGeometryList.Add(newPoint);
                    }

                    // 线
                    if (ShapeType == 3)
                    {
                        double recordXMin = shpReader.ReadDouble();
                        double recordYMin = shpReader.ReadDouble();
                        double recordXMax = shpReader.ReadDouble();
                        double recordYMax = shpReader.ReadDouble();
                        BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                        int partNumber = shpReader.ReadInt32();
                        int pointNumber = shpReader.ReadInt32();
                        List<int> partList = new List<int>();
                        for (int i = 0; i < partNumber; i++)
                        {
                            partList.Add(shpReader.ReadInt32());
                        }
                        List<Point> pointList = new List<Point>();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            pointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                        }
                        innerGeometryList.Add(new ShpPolyline(pointList, partList, recordMBR));
                    }

                    // 面
                    if (ShapeType == 5)
                    {
                        double recordXMin = shpReader.ReadDouble();
                        double recordYMin = shpReader.ReadDouble();
                        double recordXMax = shpReader.ReadDouble();
                        double recordYMax = shpReader.ReadDouble();
                        BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                        int partNumber = shpReader.ReadInt32();
                        int pointNumber = shpReader.ReadInt32();
                        List<int> partList = new List<int>();
                        for (int i = 0; i < partNumber; i++)
                        {
                            partList.Add(shpReader.ReadInt32());
                        }
                        List<IPoint> pointList = new List<IPoint>();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            pointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                        }
                        innerGeometryList.Add(new ShpPolygon(pointList, partList, recordMBR));
                    }

                    if (ShapeType == 11)
                    {
                        double x = shpReader.ReadDouble();
                        double y = shpReader.ReadDouble();
                        double z = shpReader.ReadDouble();
                        double m = shpReader.ReadDouble();
                        ShpPoint3D newPoint = new ShpPoint3D(x, y, z, m);
                        innerGeometryList.Add(newPoint);
                    }

                    if (ShapeType == 13)
                    {
                        double recordXMin = shpReader.ReadDouble();
                        double recordYMin = shpReader.ReadDouble();
                        double recordXMax = shpReader.ReadDouble();
                        double recordYMax = shpReader.ReadDouble();
                        BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                        ShpPolyline3D shpPolyline3D = new ShpPolyline3D();
                        shpPolyline3D.BoundaryBox = recordMBR;
                        int partNumber = shpReader.ReadInt32();
                        int pointNumber = shpReader.ReadInt32();
                        for (int i = 0; i < partNumber; i++)
                        {
                            shpPolyline3D.PartList.Add(shpReader.ReadInt32());
                        }
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolyline3D.PointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                        }

                        shpPolyline3D.ZMin = shpReader.ReadDouble();
                        shpPolyline3D.ZMax = shpReader.ReadDouble();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolyline3D.ZList.Add(shpReader.ReadDouble());
                        }

                        shpPolyline3D.MMin = shpReader.ReadDouble();
                        shpPolyline3D.MMax = shpReader.ReadDouble();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolyline3D.MList.Add(shpReader.ReadDouble());
                        }

                        innerGeometryList.Add(shpPolyline3D);
                    }

                    if (ShapeType == 15)
                    {
                        double recordXMin = shpReader.ReadDouble();
                        double recordYMin = shpReader.ReadDouble();
                        double recordXMax = shpReader.ReadDouble();
                        double recordYMax = shpReader.ReadDouble();
                        BoundaryBox recordMBR = new BoundaryBox(recordXMin, recordYMin, recordXMax, recordYMax);
                        
                        ShpPolygon3D shpPolygon3D = new ShpPolygon3D();
                        shpPolygon3D.BoundaryBox = recordMBR;
                        int partNumber = shpReader.ReadInt32();
                        int pointNumber = shpReader.ReadInt32();
                        List<int> partList = new List<int>();
                        for (int i = 0; i < partNumber; i++)
                        {
                            shpPolygon3D.PartList.Add(shpReader.ReadInt32());
                        }
                        List<Point> pointList = new List<Point>();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolygon3D.PointList.Add(new Point(shpReader.ReadDouble(), shpReader.ReadDouble()));
                        }
                        shpPolygon3D.ZMin = shpReader.ReadDouble();
                        shpPolygon3D.ZMax = shpReader.ReadDouble();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolygon3D.ZList.Add(shpReader.ReadDouble());
                        }

                        shpPolygon3D.MMin = shpReader.ReadDouble();
                        shpPolygon3D.MMax = shpReader.ReadDouble();
                        for (int i = 0; i < pointNumber; i++)
                        {
                            shpPolygon3D.MList.Add(shpReader.ReadDouble());
                        }

                        innerGeometryList.Add(shpPolygon3D);
                    }
                }
            }

            // 读入.shx文件
            using (BinaryReader shxReader = new BinaryReader(new FileStream(shxPath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                FileCode = SwapInt(shxReader.ReadInt32());
                for (int i = 0; i < 5; i++)
                {
                    int unuse = SwapInt(shxReader.ReadInt32());
                }
                FileLength = SwapInt(shxReader.ReadInt32());
                Version = shxReader.ReadInt32();
                ShapeType = shxReader.ReadInt32();
                XMin = shxReader.ReadDouble();
                YMin = shxReader.ReadDouble();
                XMax = shxReader.ReadDouble();
                YMax = shxReader.ReadDouble();
                ZMin = shxReader.ReadDouble();
                ZMax = shxReader.ReadDouble();
                MMin = shxReader.ReadDouble();
                MMax = shxReader.ReadDouble();

                while (shxReader.PeekChar() > -1)
                {
                    int offset = SwapInt(shxReader.ReadInt32());
                    int contentLength = SwapInt(shxReader.ReadInt32());
                }
            }

            // 读取属性表
            if (mode == VectorOpenMode.ShpOnly)
            {
                PropertySheet = null;
            }
            else
            {
                PropertySheet = new DataBaseFile(dbfPath);
            }
        }

        /// <summary>
        /// 构建一个新的Shapefile对象
        /// </summary>
        /// <param name="shapeType">Shapefile类型</param>
        public Shapefile(ESRIShapeType shapeType)
        {
            PropertySheet = new DataBaseFile();
            this.ShapeType = (int)shapeType;
        }

        #endregion

        #region 重计算

        // 重新计算当前shapefile文件的字节长度
        private void RefreshFileLength() 
        {
            int baseLength = 50;
            for (int i = 0; i < this.GeometryList.Count; i++) 
            {
                baseLength += 4;
                baseLength += GeometryList[i].ContentLength();
            }
            FileLength = baseLength;
        }

        // 重新计算当前shapefile文件的BoundaryBox
        private void RefreshBoundaryBox() 
        {
            if (this.GetFeatureNumber() == 0)
            {
                XMin = 0;
                YMin = 0;
                XMax = 0;
                YMax = 0;
                return;
            }

            // 二维点只更新x，y
            if (this.ShapeType == 1 || this.ShapeType == 11) 
            {
                XMin = double.MaxValue;
                YMin = double.MaxValue;
                XMax = double.MinValue;
                YMax = double.MinValue;
                for (int i = 0; i < this.GeometryList.Count; i++)
                {
                    if (GeometryList[i].GetFeatureType() == ESRIShapeType.None)
                    {
                        continue;
                    }
                    else
                    {
                        Point nowPoint = (Point)GeometryList[i];
                        XMin = XMin < nowPoint.X ? XMin : nowPoint.X;
                        YMin = YMin < nowPoint.Y ? YMin : nowPoint.Y;
                        XMax = XMax > nowPoint.X ? XMax : nowPoint.X;
                        YMax = YMax > nowPoint.Y ? YMax : nowPoint.Y;
                    }
                }
            }

            if (this.ShapeType == 3 || this.ShapeType == 13)
            {
                XMin = double.MaxValue;
                YMin = double.MaxValue;
                XMax = double.MinValue;
                YMax = double.MinValue;

                for (int i = 0; i < this.GeometryList.Count; i++)
                {
                    if (GeometryList[i].GetFeatureType() == ESRIShapeType.None)
                    {
                        continue;
                    }
                    else
                    {
                        ShpPolyline nowLine = (ShpPolyline)GeometryList[i];
                        if (nowLine.GetPointNumber() == 0) continue;
                        nowLine.GetBoundaryBox();
                        XMin = XMin < nowLine.BoundaryBox.XMin ? XMin : nowLine.BoundaryBox.XMin;
                        YMin = YMin < nowLine.BoundaryBox.YMin ? YMin : nowLine.BoundaryBox.YMin;
                        XMax = XMax > nowLine.BoundaryBox.XMax ? XMax : nowLine.BoundaryBox.XMax;
                        YMax = YMax > nowLine.BoundaryBox.YMax ? YMax : nowLine.BoundaryBox.YMax;
                    }
                }
            }

            if (this.ShapeType == 5 || this.ShapeType == 15)
            {
                XMin = double.MaxValue;
                YMin = double.MaxValue;
                XMax = double.MinValue;
                YMax = double.MinValue;

                for (int i = 0; i < this.GeometryList.Count; i++)
                {
                    if (GeometryList[i].GetFeatureType() == ESRIShapeType.None)
                    {
                        continue;
                    }
                    else
                    {
                        ShpPolygon nowGon = (ShpPolygon)GeometryList[i];
                        if (nowGon.GetPointNumber() == 0) continue;
                        nowGon.GetBoundaryBox();
                        XMin = XMin < nowGon.BoundaryBox.XMin ? XMin : nowGon.BoundaryBox.XMin;
                        YMin = YMin < nowGon.BoundaryBox.YMin ? YMin : nowGon.BoundaryBox.YMin;
                        XMax = XMax > nowGon.BoundaryBox.XMax ? XMax : nowGon.BoundaryBox.XMax;
                        YMax = YMax > nowGon.BoundaryBox.YMax ? YMax : nowGon.BoundaryBox.YMax;
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// 输出为Shapefile文件，文件后缀名为.shp
        /// </summary>
        /// <param name="filePath">输出文件路径</param>
        /// <returns>输出成功或失败</returns>
        public bool ExportShapefile(string filePath)
        {
            string shpPath = filePath;
            string shxPath = filePath.Substring(0, filePath.Length - 4) + ".shx";
            string dbfPath = filePath.Substring(0, filePath.Length - 4) + ".dbf";
            string prjPath = filePath.Substring(0, filePath.Length - 4) + ".prj";

            // 输出前更新文件大小属性以及BoundaryBox属性
            RefreshFileLength();
            RefreshBoundaryBox();

            // 写出.shp文件
            using (BinaryWriter shpWriter = new BinaryWriter(new FileStream(shpPath, FileMode.Create)))
            {
                // 头文件一共100位，在文件长度计数里为 100 / 2 = 50
                shpWriter.Write(SwapInt(this.FileCode));
                int unuse = 0;
                for (int i = 0; i < 5; i++)
                {
                    shpWriter.Write(SwapInt(unuse));
                }
                shpWriter.Write(SwapInt(this.FileLength));
                shpWriter.Write(this.Version);
                shpWriter.Write(this.ShapeType);
                shpWriter.Write(this.XMin);
                shpWriter.Write(this.YMin);
                shpWriter.Write(this.XMax);
                shpWriter.Write(this.YMax);
                shpWriter.Write(this.ZMin);
                shpWriter.Write(this.ZMax);
                shpWriter.Write(this.MMin);
                shpWriter.Write(this.MMax);

                // 写数据记录
                for (int i = 0; i < this.GeometryList.Count; i++)
                {
                    // 记录编号由1开始，依次递增
                    shpWriter.Write(SwapInt(i + 1));

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.None)
                    {
                        ShpNone none = this.GeometryList[i] as ShpNone;
                        int recordLength = none.ContentLength(); // （头+两个Double）/2
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.None);
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Point)
                    {
                        // 具象化为Point类型，获取记录长度
                        ShpPoint nowPoint = this.GeometryList[i] as ShpPoint;
                        int recordLength = nowPoint.ContentLength(); // （头+两个Double）/2
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.Point);
                        // 写出坐标信息
                        shpWriter.Write(nowPoint.X);
                        shpWriter.Write(nowPoint.Y);
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Polyline)
                    {
                        // 具象化为Polyline类型，获取记录长度
                        ShpPolyline nowLine = this.GeometryList[i] as ShpPolyline;
                        int recordLength = nowLine.ContentLength();
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.Polyline);

                        // 重新计算BoundaryBox并写出
                        nowLine.GetBoundaryBox();
                        shpWriter.Write(nowLine.BoundaryBox.XMin);
                        shpWriter.Write(nowLine.BoundaryBox.YMin);
                        shpWriter.Write(nowLine.BoundaryBox.XMax);
                        shpWriter.Write(nowLine.BoundaryBox.YMax);

                        // 写出部件数量与点的数量
                        shpWriter.Write(nowLine.PartNumber);
                        shpWriter.Write(nowLine.PointNumber);
                        // 写出部件位置
                        for (int j = 0; j < nowLine.PartNumber; j++)
                        {
                            shpWriter.Write(nowLine.PartList[j]);
                        }
                        // 写出点
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            shpWriter.Write(nowLine.PointList[j].GetX());
                            shpWriter.Write(nowLine.PointList[j].GetY());
                        }
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Polygon)
                    {
                        // 具象化为Polygon类型，获取记录长度
                        ShpPolygon nowGon = this.GeometryList[i] as ShpPolygon;
                        int recordLength = nowGon.ContentLength();
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.Polygon);

                        nowGon.GetBoundaryBox();
                        shpWriter.Write(nowGon.BoundaryBox.XMin);
                        shpWriter.Write(nowGon.BoundaryBox.YMin);
                        shpWriter.Write(nowGon.BoundaryBox.XMax);
                        shpWriter.Write(nowGon.BoundaryBox.YMax);

                        shpWriter.Write(nowGon.PartNumber);
                        shpWriter.Write(nowGon.PointNumber);
                        for (int j = 0; j < nowGon.PartNumber; j++)
                        {
                            shpWriter.Write(nowGon.PartList[j]);
                        }
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            shpWriter.Write(nowGon.PointList[j].GetX());
                            shpWriter.Write(nowGon.PointList[j].GetY());
                        }
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PointZ)
                    {
                        // 具象化为Point类型，获取记录长度
                        ShpPoint3D nowPoint = this.GeometryList[i] as ShpPoint3D;
                        int recordLength = nowPoint.ContentLength(); // （头+两个Double）/2
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.PointZ);
                        // 写出坐标信息
                        shpWriter.Write(nowPoint.X);
                        shpWriter.Write(nowPoint.Y);
                        shpWriter.Write(nowPoint.Z);
                        shpWriter.Write(nowPoint.M);
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PolylineZ)
                    {
                        // 具象化为Polyline类型，获取记录长度
                        ShpPolyline3D nowLine = this.GeometryList[i] as ShpPolyline3D;
                        int recordLength = nowLine.ContentLength();
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.PolylineZ);

                        // 重新计算BoundaryBox并写出
                        nowLine.GetBoundaryBox();
                        shpWriter.Write(nowLine.BoundaryBox.XMin);
                        shpWriter.Write(nowLine.BoundaryBox.YMin);
                        shpWriter.Write(nowLine.BoundaryBox.XMax);
                        shpWriter.Write(nowLine.BoundaryBox.YMax);

                        // 写出部件数量与点的数量
                        shpWriter.Write(nowLine.PartNumber);
                        shpWriter.Write(nowLine.PointNumber);
                        // 写出部件位置
                        for (int j = 0; j < nowLine.PartNumber; j++)
                        {
                            shpWriter.Write(nowLine.PartList[j]);
                        }
                        // 写出点
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            shpWriter.Write(nowLine.PointList[j].GetX());
                            shpWriter.Write(nowLine.PointList[j].GetY());
                        }

                        shpWriter.Write(nowLine.ZMin);
                        shpWriter.Write(nowLine.ZMax);
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            shpWriter.Write(nowLine.ZList[j]);
                        }

                        shpWriter.Write(nowLine.MMin);
                        shpWriter.Write(nowLine.MMax);
                        for (int j = 0; j < nowLine.PointNumber; j++)
                        {
                            shpWriter.Write(nowLine.MList[j]);
                        }
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PolygonZ)
                    {
                        // 具象化为PolyGON类型，获取记录长度
                        ShpPolygon3D nowGon = this.GeometryList[i] as ShpPolygon3D;
                        int recordLength = nowGon.ContentLength();
                        shpWriter.Write(SwapInt(recordLength));
                        shpWriter.Write((int)ESRIShapeType.PolygonZ);

                        // 重新计算BoundaryBox并写出
                        nowGon.GetBoundaryBox();
                        shpWriter.Write(nowGon.BoundaryBox.XMin);
                        shpWriter.Write(nowGon.BoundaryBox.YMin);
                        shpWriter.Write(nowGon.BoundaryBox.XMax);
                        shpWriter.Write(nowGon.BoundaryBox.YMax);

                        // 写出部件数量与点的数量
                        shpWriter.Write(nowGon.PartNumber);
                        shpWriter.Write(nowGon.PointNumber);
                        // 写出部件位置
                        for (int j = 0; j < nowGon.PartNumber; j++)
                        {
                            shpWriter.Write(nowGon.PartList[j]);
                        }
                        // 写出点
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            shpWriter.Write(nowGon.PointList[j].GetX());
                            shpWriter.Write(nowGon.PointList[j].GetY());
                        }

                        shpWriter.Write(nowGon.ZMin);
                        shpWriter.Write(nowGon.ZMax);
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            shpWriter.Write(nowGon.ZList[j]);
                        }

                        shpWriter.Write(nowGon.MMin);
                        shpWriter.Write(nowGon.MMax);
                        for (int j = 0; j < nowGon.PointNumber; j++)
                        {
                            shpWriter.Write(nowGon.MList[j]);
                        }
                    }
                }
            }

            int shxFileLength = 50 + 4 * this.GeometryList.Count;

            // 写出.shx文件
            using (BinaryWriter shxWriter = new BinaryWriter(new FileStream(shxPath, FileMode.Create)))
            {
                shxWriter.Write(SwapInt(this.FileCode));
                int unuse = 0;
                for (int i = 0; i < 5; i++)
                {
                    shxWriter.Write(SwapInt(unuse));
                }
                shxWriter.Write(SwapInt(shxFileLength));
                shxWriter.Write(this.Version);
                shxWriter.Write(this.ShapeType);
                shxWriter.Write(this.XMin);
                shxWriter.Write(this.YMin);
                shxWriter.Write(this.XMax);
                shxWriter.Write(this.YMax);
                shxWriter.Write(this.ZMin);
                shxWriter.Write(this.ZMax);
                shxWriter.Write(this.MMin);
                shxWriter.Write(this.MMax);

                int baseOffset = 50;
                for (int i = 0; i < this.GeometryList.Count; i++)
                {
                    shxWriter.Write(SwapInt(baseOffset));
                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.None)
                    {
                        ShpNone nowNone = this.GeometryList[i] as ShpNone;
                        shxWriter.Write(SwapInt(nowNone.ContentLength()));
                        baseOffset += nowNone.ContentLength();
                    }
                    
                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Point)
                    {
                        ShpPoint nowPoint = this.GeometryList[i] as ShpPoint;
                        shxWriter.Write(SwapInt(nowPoint.ContentLength()));
                        baseOffset += nowPoint.ContentLength();
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Polyline)
                    {
                        ShpPolyline nowLine = this.GeometryList[i] as ShpPolyline;
                        shxWriter.Write(SwapInt(nowLine.ContentLength()));
                        baseOffset += nowLine.ContentLength();
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.Polygon)
                    {
                        ShpPolygon nowGon = this.GeometryList[i] as ShpPolygon;
                        shxWriter.Write(SwapInt(nowGon.ContentLength()));
                        baseOffset += nowGon.ContentLength();
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PointZ)
                    {
                        ShpPoint3D nowPoint = this.GeometryList[i] as ShpPoint3D;
                        shxWriter.Write(SwapInt(nowPoint.ContentLength()));
                        baseOffset += nowPoint.ContentLength();
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PolylineZ)
                    {
                        ShpPolyline3D nowLine = this.GeometryList[i] as ShpPolyline3D;
                        shxWriter.Write(SwapInt(nowLine.ContentLength()));
                        baseOffset += nowLine.ContentLength();
                    }

                    if (this.GeometryList[i].GetFeatureType() == ESRIShapeType.PolygonZ)
                    {
                        ShpPolygon3D nowGon = this.GeometryList[i] as ShpPolygon3D;
                        shxWriter.Write(SwapInt(nowGon.ContentLength()));
                        baseOffset += nowGon.ContentLength();
                    }
                    baseOffset += 4;
                }
            }

            // 写出新的.dbf文件
            this.PropertySheet.Save(dbfPath);

            if (CoordinateSystem != null) 
            {
                using (StreamWriter prjWriter = new StreamWriter(new FileStream(prjPath, FileMode.Create)))
                {
                    prjWriter.WriteLine(CoordinateSystem.ExportToWkt());
                    prjWriter.Close();
                }
            }

            return true;
        }

        public bool ExportToGeoJson(string filepath)
        {
            throw new NotImplementedException();
        }

        // 大字节和小字节的互转
        private int SwapInt(int source)
        {
            return (source & 0xFF) << 24 | (source >> 8 & 0xFF) << 16 | (source >> 16 & 0xFF) << 8 | (source >> 24 & 0xFF);
        }

        // 将WKT转为目标对象
        private IShpGeometryBase WktToGeometryBase(string wkt) 
        {
            switch (ShapeType) 
            {
                case 1:
                    return new ShpPoint(wkt);
                case 3:
                    return new ShpPolyline(wkt);
                case 5:
                    return new ShpPolygon(wkt);
                default:
                    throw new Exception("当前的数据格式暂未支持，敬请等待版本更新！");
            }
        }

        public int AddFeature(string wkt, Dictionary<string, object> values)
        {
            // 转换完成的加入GeometryList
            this.GeometryList.Add(WktToGeometryBase(wkt));
            // 操作属性表，插入数据
            this.PropertySheet.InsertRow(values);
            return this.innerGeometryList.Count - 1;
        }

        public int AddFeature(string wkt)
        {
            // 转换完成的加入GeometryList
            this.GeometryList.Add(WktToGeometryBase(wkt));
            // 操作属性表插入空行
            this.PropertySheet.InsertEmptyRow();
            return this.innerGeometryList.Count - 1;
        }

        public int AddFeature(IShpGeometryBase newGeometry, Dictionary<string, object> values)
        {
            this.GeometryList.Add(newGeometry);
            this.PropertySheet.InsertRow(values);
            return this.innerGeometryList.Count - 1;
        }

        public int AddFeature(IShpGeometryBase newGeometry)
        {
            // 转换完成的加入GeometryList
            this.GeometryList.Add(newGeometry);
            // 操作属性表插入空行
            this.PropertySheet.InsertEmptyRow();
            return this.innerGeometryList.Count - 1;
        }

        public bool SetValue(int index, string field, object value)
        {
            this.PropertySheet.SetValue(index, field, value);
            return true;
        }

        public bool SetValue2(int index, string field, byte[] value)
        {
            this.PropertySheet.SetValue2(index, field, value);
            return true;
        }

        public bool RemoveFeature(int index)
        {
            if (index < 0 || index >= this.GeometryList.Count)
            {
                throw new Exception("要素索引超出界限，删除要素失败！");
            }
            // 删除要素的空间位置与属性信息
            this.GeometryList.RemoveAt(index);
            this.PropertySheet.DeleteRow(index);
            return true;
        }

        public bool AddField(string name, DBFFieldType type, int length, int precision)
        {
            this.PropertySheet.InsertField(name, type, length, precision);
            return true;
        }

        public bool DeleteField(string name)
        {
            this.PropertySheet.DeleteField(name);
            return true;
        }

        public bool Save()
        {
            if (this.filePath == "") 
            {
                return false;
            }
            return ExportShapefile(this.filePath);
        }

        public CoordinateBase GetCoordinateRef()
        {
            return CoordinateSystem;
        }

        public bool SetCoordinateRef(CoordinateBase newCoordinate)
        {
            this.CoordinateSystem = newCoordinate;
            return true;
        }

        public int GetFeatureNumber()
        {
            return innerGeometryList.Count;
        }

        public IShpGeometryBase GetFeature(int index)
        {
            if (index < 0 || index >= GetFeatureNumber())
            {
                throw new Exception("要素索引越界！");
            }

            return GeometryList[index];
        }

        public bool SetFeature(int index, IShpGeometryBase newFeature)
        {
            if (index < 0 || index >= GetFeatureNumber())
            {
                throw new Exception("要素索引越界！");
            }

            GeometryList[index] = newFeature;
            return true;
        }

        public int GetFeatureType()
        {
            return ShapeType;
        }

        public string GetFieldValueAsString(int index, string field)
        {
            return this.PropertySheet.GetString(index, field).Trim('\0').Trim(' ');
        }

        public double GetFieldValueAsDouble(int index, string field)
        {
            return this.PropertySheet.GetDouble(index, field);
        }

        public int GetFieldValueAsInt(int index, string field)
        {
            return this.PropertySheet.GetInt(index, field);
        }

        public byte[] GetFieldValueAsByte(int index, string field)
        {
            return this.PropertySheet.GetValue(index, field);
        }

        public BoundaryBox GetBoundaryBox()
        {
            this.RefreshBoundaryBox();
            return new BoundaryBox(this.XMin, this.YMin, this.XMax, this.YMax);
        }

        public bool OnlySaveDBF(string outputPath)
        {
            string dbfPath = outputPath.Substring(0, outputPath.Length - 4) + ".dbf";
            this.PropertySheet.Save(dbfPath);
            return true;
        }

        public bool FieldExist(string fieldName)
        {
            if (this.PropertySheet == null) return false;

            List<string> fieldNameList = this.PropertySheet.GetFieldNameList().ToList();
            if (fieldNameList.Contains(fieldName))
            {
                return true;
            }

            return false;
        }

        public IEnumerable<string> GetFieldNames()
        {
            if (this.PropertySheet != null)
            {
                return this.PropertySheet.GetFieldNameList();
            }

            return null;
        }

        public List<DBFFieldObject> GetFieldInfoList()
        {
            return this.PropertySheet.GetFieldInfoList().ToList();
        }

        public bool CopyFieldInformation(IShapefile otherShapefile)
        {
            List<DBFFieldObject> moreFieldNames = otherShapefile.GetFieldInfoList().ToList();
            List<string> existFieldNames = this.GetFieldNames().ToList();
            foreach (DBFFieldObject addField in moreFieldNames)
            {
                if (!existFieldNames.Contains(addField.FieldName))
                {
                    this.AddField(addField.FieldName, addField.FieldType, addField.FieldLength, addField.FieldPrecision);
                }
            }
            return true;
        }

        public IShapefile Clone()
        {
            Shapefile newShapefile = new Shapefile((ESRIShapeType)this.ShapeType);
            newShapefile.CopyFieldInformation(this);
            List<string> fieldNames = this.GetFieldNames().ToList();
            for (int i = 0; i < this.GetFeatureNumber(); i++)
            {
                IShpGeometryBase newDeepClone = this.GeometryList[i].Clone();
                int newIndex = newShapefile.AddFeature(newDeepClone);
                foreach (string field in fieldNames)
                {
                    byte[] oriFieldInfo = this.GetFieldValueAsByte(newIndex, field);
                    newShapefile.SetValue2(newIndex, field, oriFieldInfo);
                }
            }
            newShapefile.SetCoordinateRef(this.GetCoordinateRef());
            return newShapefile;
        }
    }
}
