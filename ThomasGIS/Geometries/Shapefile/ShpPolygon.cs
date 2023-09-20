using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.DataManagement;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Helpers;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public enum PolygonType
    {
        // 自动识别
        Unknown,
        // 单部件面
        Polygon,
        // 多部件面
        MultiPolygon
    }

    public class ShpPolygon : IShpPolygon
    {
        public List<IPoint> PointList { get; } = new List<IPoint>();
        public List<int> PartList { get; } = new List<int>();
        public BoundaryBox BoundaryBox { get; set; } = null;
        public PolygonType WktType => PointNumber <= 0 ? PolygonType.Unknown : GetPartsArea().Select(item => item > 0).ToList().Count > 1 ? PolygonType.MultiPolygon : PolygonType.Polygon;

        public int PointNumber => PointList.Count;
        public int PartNumber => PartList.Count;

        public BoundaryBox GetBoundaryBox()
        {
            if (PointNumber > 0)
            {
                double xMin = PointList.Min(item => item.GetX());
                double xMax = PointList.Max(item => item.GetX());
                double yMin = PointList.Min(item => item.GetY());
                double yMax = PointList.Max(item => item.GetY());
                this.BoundaryBox = new BoundaryBox(xMin, yMin, xMax, yMax);
            }
            else
            {
                this.BoundaryBox = new BoundaryBox(0, 0, 0, 0);
            }

            return this.BoundaryBox;
        }

        public ShpPolygon(IEnumerable<IPoint> pointList, IEnumerable<int> partList, BoundaryBox boundaryBox = null)
        {
            PointList.AddRange(pointList);
            PartList.AddRange(partList);
            BoundaryBox = boundaryBox != null ? new BoundaryBox(boundaryBox) : GetBoundaryBox();
        }

        public ShpPolygon(IEnumerable<IPoint> pointList, BoundaryBox boundaryBox = null)
        {
            PartList.Add(0);
            PointList.AddRange(pointList);
            BoundaryBox = boundaryBox != null ? new BoundaryBox(boundaryBox) : GetBoundaryBox();
        }

        public ShpPolygon()
        {
            GetBoundaryBox();
        }

        public ShpPolygon(string wkt, PolygonType type = PolygonType.Unknown)
        {
            if (type == PolygonType.Unknown)
            {
                wkt = wkt.ToUpper();
                string subTypeString = wkt.Substring(0, 7);
                if (subTypeString == "POLYGON")
                {
                    type = PolygonType.Polygon;
                }
                else 
                {
                    subTypeString = wkt.Substring(0, 12);
                    if (subTypeString == "MULTIPOLYGON")
                    {
                        type = PolygonType.MultiPolygon;
                    }
                }
            }

            string wktContent;

            switch (type)
            {
                case PolygonType.Polygon:
                    wktContent = wkt.Substring(7, wkt.Length - 7);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(2, wktContent.Length - 4);
                    string[] gonParts = wktContent.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string onePart in gonParts)
                    {
                        string[] points = onePart.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                        PartList.Add(PointNumber);
                        foreach (string point in points)
                        {
                            string[] coordinates = point.Trim(' ').Split(' ');
                            double x = Convert.ToDouble(coordinates[0]);
                            double y = Convert.ToDouble(coordinates[1]);
                            PointList.Add(new ShpPoint(x, y));
                        }
                    }
                    break;
                case PolygonType.MultiPolygon:
                    wktContent = wkt.Substring(12, wkt.Length - 12);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(3, wktContent.Length - 6);
                    string[] polygons = wktContent.Split(new string[] { ")),((", ")), ((" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string polygon in polygons)
                    {
                        string[] gonParts1 = polygon.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (string onePart in gonParts1)
                        {
                            string[] points = onePart.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                            PartList.Add(PointNumber);
                            foreach (string point in points)
                            {
                                string[] coordinates = point.Trim(' ').Split(' ');
                                double x = Convert.ToDouble(coordinates[0]);
                                double y = Convert.ToDouble(coordinates[1]);
                                PointList.Add(new ShpPoint(x, y));
                            }
                        }
                    }
                    break;
                default:
                    throw new Exception("无法识别的WKT面状字符串");
            }

            BoundaryBox = GetBoundaryBox();
        }

        public ShpPolygon(byte[] wkb)
        {
            byte byteOrder = wkb[0];

            ByteArrayReader wkbReader;
            if ((!BitConverter.IsLittleEndian && byteOrder == 0) || (BitConverter.IsLittleEndian && byteOrder == 1))
            {
                wkbReader = new ByteArrayReader(wkb, false);
            }
            else
            {
                wkbReader = new ByteArrayReader(wkb, true);
            }

            byteOrder = wkbReader.ReadByte();
            uint geometryType = wkbReader.ReadUInt();
            uint geometryNumber = wkbReader.ReadUInt();

            if (geometryType == 0x00000003)
            {
                for (int i = 0; i < geometryNumber; ++i)
                {
                    PartList.Add(PointNumber);
                    uint pointNumber = wkbReader.ReadUInt();
                    for (int j = 0; j < pointNumber; ++j)
                    {
                        double X = wkbReader.ReadDouble();
                        double Y = wkbReader.ReadDouble();
                        PointList.Add(new Point(X, Y));
                    }
                }
            }

            if (geometryType == 0x00000006)
            {
                for (int i = 0; i < geometryNumber; ++i)
                {
                    byteOrder = wkbReader.ReadByte();
                    geometryType = wkbReader.ReadUInt();
                    uint ringNumber = wkbReader.ReadUInt();

                    if (geometryType != 0x00000003) throw new Exception("Error Occurred When Construct ShpPolygon: WKB Error!");

                    for (int j = 0; j < ringNumber; ++j)
                    {
                        PartList.Add(PointNumber);
                        uint pointNumber = wkbReader.ReadUInt();
                        for (int k = 0; k < pointNumber; ++k)
                        {
                            double X = wkbReader.ReadDouble();
                            double Y = wkbReader.ReadDouble();
                            PointList.Add(new Point(X, Y));
                        }
                    }
                }
            }
        }

        public ShpPolygon(ShpPolygon clone)
        {
            for (int i = 0; i < clone.PartNumber; i++)
            {
                this.PartList.AddRange(clone.PartList);
            }

            for (int i = 0; i < clone.PointNumber; i++)
            {
                IPoint nowPoint = clone.GetPointByIndex(i);
                IPoint newPoint = new Point(nowPoint);
                this.PointList.Add(newPoint);
            }

            GetBoundaryBox();
        }

        public double[] GetPartsArea()
        {
            double[] partsArea = new double[PartNumber];
            for (int i = 0; i < PartNumber; i++)
            {
                if (i == PartNumber - 1)
                {
                    double area = 0;
                    for (int j = PartList[i]; j < PointNumber - 1; j++)
                    {
                        IPoint sp = PointList[j];
                        IPoint ep = PointList[j + 1];
                        area += (sp.GetY() + ep.GetY()) * (ep.GetX() - sp.GetX()) * 0.5;
                    }
                    partsArea[i] = area;
                }
                else
                {
                    double area = 0;
                    for (int j = PartList[i]; j < PartList[i + 1] - 1; j++)
                    {
                        IPoint sp = PointList[j];
                        IPoint ep = PointList[j + 1];
                        area += (sp.GetY() + ep.GetY()) * (ep.GetX() - sp.GetX()) * 0.5;
                    }
                    partsArea[i] = area;
                }
            }

            return partsArea;
        }

        // 有BUG禁用！！！
        public IEnumerable<double> GetArea()
        {
            throw new NotImplementedException();
        }

        public virtual string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();
            double[] partAreas = GetPartsArea();
            switch (WktType)
            {
                case PolygonType.Polygon:
                    wktBuilder.Append("POLYGON (");
                    for (int i = 0; i < PartNumber; i++)
                    {
                        StringBuilder innerBuilder = new StringBuilder();
                        innerBuilder.Append("(");
                        if (i < PartNumber - 1)
                        {
                            for (int j = PartList[i]; j < PartList[i + 1]; j++)
                            {
                                string coordinates = $"{PointList[j].GetX()} {PointList[j].GetY()}";
                                innerBuilder.Append(coordinates);
                                if (j < PartList[i + 1] - 1)
                                {
                                    innerBuilder.Append(",");
                                }
                            }
                        }
                        else
                        {
                            for (int j = PartList[i]; j < PointNumber; j++)
                            {
                                string coordinates = $"{PointList[j].GetX()} {PointList[j].GetY()}";
                                innerBuilder.Append(coordinates);
                                if (j < PointNumber - 1)
                                {
                                    innerBuilder.Append(",");
                                }
                            }
                        }
                        innerBuilder.Append(")");
                        if (i < PartNumber - 1)
                        {
                            innerBuilder.Append(",");
                        }
                        wktBuilder.Append(innerBuilder.ToString());
                        innerBuilder.Clear();
                    }
                    wktBuilder.Append(")");
                    break;
                case PolygonType.MultiPolygon:
                    wktBuilder.Append("MULTIPOLYGON (");
                    // 有几个面积是正的就有几个polygon，然后依据正负划分polygon
                    int polygonNumber = GetPartsArea().Select(item => item > 0).ToList().Count;
                    // 每个polygon都要生成一个对象，记录当前处理到的part位置
                    int nowPart = 0;
                    for (int i = 0; i < polygonNumber; i++)
                    {
                        StringBuilder polygonBuilder = new StringBuilder();
                        polygonBuilder.Append("(");
                        // 首先结束部分等于开始部分+1，如果是面积是负数则向后移动
                        int endPart = nowPart + 1;
                        while (endPart < PartNumber && partAreas[endPart] < 0)
                        {
                            endPart += 1;
                        }
                        for (int k = nowPart; k < endPart; k++)
                        {
                            StringBuilder partBuilder = new StringBuilder();
                            partBuilder.Append("(");
                            if (k < PartNumber - 1)
                            {
                                for (int m = PartList[k]; m < PartList[k + 1]; m++)
                                {
                                    string coordinates = $"{PointList[m].GetX()} {PointList[m].GetY()}";
                                    partBuilder.Append(coordinates);
                                    if (m < PartList[k + 1] - 1)
                                    {
                                        partBuilder.Append(",");
                                    }
                                }
                            }
                            else
                            {
                                for (int m = PartList[k]; m < PointNumber; m++)
                                {
                                    string coordinates = $"{PointList[m].GetX()} {PointList[m].GetY()}";
                                    partBuilder.Append(coordinates);
                                    if (m < PointNumber - 1)
                                    {
                                        partBuilder.Append(",");
                                    }
                                }

                            }
                            partBuilder.Append(")");
                            polygonBuilder.Append(partBuilder.ToString());
                            partBuilder.Clear();
                            if (k < endPart - 1)
                            {
                                polygonBuilder.Append(",");
                            }
                        }
                        nowPart = endPart;
                        polygonBuilder.Append(")");
                        wktBuilder.Append(polygonBuilder.ToString());
                        if (i < polygonNumber - 1)
                        {
                            wktBuilder.Append(",");
                        }
                    }
                    wktBuilder.Append(")");
                    break;
                default:
                    throw new Exception("无法识别的类型，输出失败");
            }
            return wktBuilder.ToString();
        }

        public virtual string ExportToWktMysql()
        {
            StringBuilder wktBuilder = new StringBuilder();
            double[] partAreas = GetPartsArea();

            wktBuilder.Append("MULTIPOLYGON (");
            // 有几个面积是正的就有几个polygon，然后依据正负划分polygon
            int polygonNumber = GetPartsArea().Select(item => item > 0).ToList().Count;
            // 每个polygon都要生成一个对象，记录当前处理到的part位置
            int nowPart = 0;
            for (int i = 0; i < polygonNumber; i++)
            {
                StringBuilder polygonBuilder = new StringBuilder();
                polygonBuilder.Append("(");
                // 首先结束部分等于开始部分+1，如果是面积是负数则向后移动
                int endPart = nowPart + 1;
                while (endPart < PartNumber && partAreas[endPart] < 0)
                {
                    endPart += 1;
                }
                for (int k = nowPart; k < endPart; k++)
                {
                    StringBuilder partBuilder = new StringBuilder();
                    partBuilder.Append("(");
                    if (k < PartNumber - 1)
                    {
                        for (int m = PartList[k]; m < PartList[k + 1]; m++)
                        {
                            string coordinates = $"{PointList[m].GetY()} {PointList[m].GetX()}";
                            partBuilder.Append(coordinates);
                            if (m < PartList[k + 1] - 1)
                            {
                                partBuilder.Append(",");
                            }
                        }
                    }
                    else
                    {
                        for (int m = PartList[k]; m < PointNumber; m++)
                        {
                            string coordinates = $"{PointList[m].GetY()} {PointList[m].GetX()}";
                            partBuilder.Append(coordinates);
                            if (m < PointNumber - 1)
                            {
                                partBuilder.Append(",");
                            }
                        }

                    }
                    partBuilder.Append(")");
                    polygonBuilder.Append(partBuilder.ToString());
                    partBuilder.Clear();
                    if (k < endPart - 1)
                    {
                        polygonBuilder.Append(",");
                    }
                }
                nowPart = endPart;
                polygonBuilder.Append(")");
                wktBuilder.Append(polygonBuilder.ToString());
                if (i < polygonNumber - 1)
                {
                    wktBuilder.Append(",");
                }
            }
            wktBuilder.Append(")");

            return wktBuilder.ToString();
        }

        public virtual int ContentLength()
        {
            return 2 + 16 + 2 + 2 + 2 * PartNumber + 8 * PointNumber;
        }

        public virtual ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.Polygon;
        }

        public virtual string GetGeometryType()
        {
            return "ShapePolygon";
        }

        public IEnumerable<IPoint> GetPointEnumerable()
        {
            return PointList.AsEnumerable();
        }

        // 添加一个完整的部件
        public bool AddPart(IEnumerable<IPoint> pointList)
        {
            this.PartList.Add(PointNumber);
            this.PointList.AddRange(pointList);
            return true;
        }

        public IEnumerable<int> GetPartEnumerable()
        {
            return PartList.AsEnumerable();
        }

        public int GetPartNumber()
        {
            return this.PartNumber;
        }

        public int GetPointNumber()
        {
            return this.PointNumber;
        }

        public IPoint GetPointByIndex(int index)
        {
            if (index < -this.PointNumber || index >= this.PointNumber) throw new IndexOutOfRangeException();

            if (index < 0) index += this.PointNumber;

            return this.PointList[index];
        }

        public IEnumerable<IPoint> GetPartByIndex(int index)
        {
            if (index < -this.PartNumber || index >= this.PartNumber) throw new IndexOutOfRangeException();

            if (index < 0) index += this.PartNumber;

            int startLoc = this.PartList[index];
            int endLoc;

            if (index == this.PartNumber - 1)
            {
                endLoc = this.PointNumber;
            }
            else
            {
                endLoc = this.PartList[index + 1];
            }

            return this.PointList.GetRange(startLoc, endLoc - startLoc);
        }

        public bool RemovePart(int index)
        {
            if (index < -this.PartNumber || index >= this.PartNumber) throw new IndexOutOfRangeException();

            if (index < 0) index += this.PartNumber;

            int startLoc = this.PartList[index];
            int endLoc;

            if (index == this.PartNumber - 1)
            {
                endLoc = this.PointNumber;
            }
            else
            {
                endLoc = this.PartList[index + 1];
            }

            for (int i = startLoc; i < endLoc; i++)
            {
                this.PointList.RemoveAt(startLoc);
            }

            this.PartList.RemoveAt(index);

            for (int i = index; i < this.PartNumber; i++)
            {
                this.PartList[i] -= (endLoc - startLoc);
            }

            return true;
        }

        public IEnumerable<IEnumerable<IPoint>> GetPolygon(int index)
        {
            List<List<IPoint>> result = new List<List<IPoint>>();
            double[] areas = this.GetPartsArea();
            int searchIndex = 0;
            for (int i = 0; i < this.PartNumber; i++)
            {
                if (areas[i] > 0)
                {
                    searchIndex++;
                }

                if (searchIndex == index)
                {
                    List<IPoint> partPolygon = new List<IPoint>();
                    int startLoc = this.PartList[i];
                    int endLoc;
                    if (i < this.PartNumber - 1)
                    {
                        endLoc = this.PartList[i + 1];
                    }
                    else
                    {
                        endLoc = this.PointNumber;
                    }

                    for (int k = startLoc; k < endLoc; k++)
                    {
                        partPolygon.Add(this.PointList[k]);
                    }

                    result.Add(partPolygon);
                    i++;

                    while (i < this.PartNumber && areas[i] < 0)
                    {
                        List<IPoint> minusPolygon = new List<IPoint>();
                        startLoc = this.PartList[i];
                        if (i < this.PartNumber - 1)
                        {
                            endLoc = this.PartList[i + 1];
                        }
                        else
                        {
                            endLoc = this.PointNumber;
                        }

                        for (int k = startLoc; k < endLoc; k++)
                        {
                            minusPolygon.Add(this.PointList[k]);
                        }

                        result.Add(partPolygon);
                    }

                    break;
                }
            }

            return result;
        }

        double[] IMultiPolygon.GetArea(CoordinateType coordinateType)
        {
            double[] areas = new double[this.PartNumber];
            for (int i = 0; i < this.PartNumber; i++)
            {
                List<IPoint> partPolygon = new List<IPoint>();
                int startLoc = this.PartList[i];
                int endLoc;
                if (i < this.PartNumber - 1)
                {
                    endLoc = this.PartList[i + 1];
                }
                else
                {
                    endLoc = this.PointNumber;
                }

                for (int j = startLoc; j < endLoc; j++)
                {
                    partPolygon.Add(this.PointList[j]);
                }

                if (coordinateType == CoordinateType.Geographic)
                {
                    areas[i] = DistanceCalculator.SpatialAreaGeo(partPolygon);
                }
                else
                {
                    areas[i] = DistanceCalculator.SpatialArea(partPolygon);
                }
            }

            return areas;
        }

        public IEnumerable<IEnumerable<IEnumerable<IPoint>>> GetPointList()
        {
            List<List<List<IPoint>>> result = new List<List<List<IPoint>>>();

            double[] partAreaList = this.GetPartsArea();
            int biggerThanZeroCount = 0;
            for (int i = 0; i < this.PartNumber; i++)
            {
                if (partAreaList[i] > 0)
                {
                    biggerThanZeroCount += 1;
                }
            }

            int prevPositiveIndex = 0;
            int nowIndex = 1;

            while (nowIndex < this.PartNumber)
            {
                if (partAreaList[nowIndex] > 0)
                {
                    List<List<IPoint>> polygon = new List<List<IPoint>>();
                    for (int i = prevPositiveIndex; i < nowIndex; i++)
                    {
                        polygon.Add(this.GetPartByIndex(i).ToList());
                    }
                    result.Add(polygon);
                    prevPositiveIndex = nowIndex;
                }

                nowIndex += 1;
            }

            List<List<IPoint>> lastPolygon = new List<List<IPoint>>();
            for (int i = prevPositiveIndex; i < nowIndex; i++)
            {
                lastPolygon.Add(this.GetPartByIndex(i).ToList());
            }
            result.Add(lastPolygon);

            return result;
        }

        public string GetBaseGeometryType()
        {
            return "MultiPolygon";
        }

        public virtual IShpGeometryBase Clone()
        {
            return new ShpPolygon(this);
        }
    }
}
