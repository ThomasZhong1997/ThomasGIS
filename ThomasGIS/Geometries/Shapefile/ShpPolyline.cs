using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Vector;

namespace ThomasGIS.Geometries.Shapefile
{
    public enum PolylineType 
    { 
        // 程序自行判断
        Unknown,
        // 单部件折线
        LineString,
        // 多部件折线
        MultiLineString
    }

    public class ShpPolyline : IShpPolyline
    {
        public List<IPoint> PointList { get; } = new List<IPoint>();
        public List<int> PartList { get; } = new List<int>();
        public int PointNumber => PointList.Count;
        public int PartNumber => PartList.Count;
        public PolylineType WktType => PartNumber == 1 ? PolylineType.LineString : PolylineType.MultiLineString;
        public BoundaryBox BoundaryBox { get; set; } = null;

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

        public ShpPolyline(IEnumerable<IPoint> points, IEnumerable<int> parts, BoundaryBox boundaryBox = null) 
        {
            PointList.AddRange(points);
            PartList.AddRange(parts);
            BoundaryBox = boundaryBox != null ? new BoundaryBox(boundaryBox) : GetBoundaryBox();
        }

        public ShpPolyline(IEnumerable<IPoint> points, BoundaryBox boundaryBox = null)
        {
            PartList.Add(0);
            PointList.AddRange(points);
            BoundaryBox = boundaryBox != null ? new BoundaryBox(boundaryBox) : GetBoundaryBox();
        }

        public ShpPolyline()
        {
            GetBoundaryBox();
        }

        public ShpPolyline(string wkt, PolylineType type = PolylineType.Unknown)
        {
            if (type == PolylineType.Unknown)
            {
                wkt = wkt.ToUpper();
                string subTypeString = wkt.Substring(0, 10);
                if (subTypeString == "LINESTRING")
                {
                    type = PolylineType.LineString;
                }
                else
                {
                    subTypeString = wkt.Substring(0, 15);
                    if (subTypeString == "MULTILINESTRING")
                    {
                        type = PolylineType.MultiLineString;
                    }
                }
            }

            string wktContent = wkt;

            switch (type)
            {
                case PolylineType.Unknown:
                    throw new Exception("无法识别的线状WKT字符串");
                case PolylineType.LineString:
                    wktContent = wktContent.Substring(10, wktContent.Length - 10);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(1, wktContent.Length - 2);
                    // 按","分割后按" "分割
                    string[] points = wktContent.Split(',');
                    // 只有一条折线只有一个part
                    PartList.Add(PointNumber);
                    foreach (string point in points)
                    {
                        string[] coordinates = point.Trim(' ').Split(' ');
                        double x = Convert.ToDouble(coordinates[0]);
                        double y = Convert.ToDouble(coordinates[1]);
                        PointList.Add(new Point(x, y));
                    }
                    break;
                case PolylineType.MultiLineString:
                    wktContent = wktContent.Substring(15, wktContent.Length - 15);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(2, wktContent.Length - 4);
                    string[] lineParts = wktContent.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lineParts)
                    {
                        PartList.Add(PointNumber);
                        string[] points1 = line.Split(',');
                        foreach (var point in points1)
                        {
                            string[] coordinates = point.Trim(' ').Split(' ');
                            double x = Convert.ToDouble(coordinates[0]);
                            double y = Convert.ToDouble(coordinates[1]);
                            PointList.Add(new Point(x, y));
                        }
                    }
                    break;
            }

            BoundaryBox = GetBoundaryBox();
        }

        public ShpPolyline(ShpPolyline clone)
        {
            this.PartList.AddRange(clone.PartList);
            for (int i = 0; i < clone.PointNumber; i++)
            {
                IPoint nowPoint = clone.GetPointByIndex(i);
                IPoint newPoint = new Point(nowPoint);
                this.PointList.Add(newPoint);
            }
            GetBoundaryBox();
        }

        // 静态方法由wkt字符串构建折线对象
        public static IShpPolyline ParseWkt(string wkt, PolylineType type = PolylineType.Unknown)
        {
            List<Point> pointList = new List<Point>();
            List<int> partList = new List<int>();
            if (type == PolylineType.Unknown)
            {
                wkt = wkt.ToUpper();
                string subTypeString = wkt.Substring(0, 10);
                if (subTypeString == "LINESTRING")
                {
                    type = PolylineType.LineString;
                }

                subTypeString = wkt.Substring(0, 15);
                if (subTypeString == "MULTILINESTRING")
                {
                    type = PolylineType.MultiLineString;
                }
            }

            string wktContent = wkt;

            switch (type)
            {
                case PolylineType.Unknown:
                    throw new Exception("无法识别的线状WKT字符串");
                case PolylineType.LineString:
                    wktContent = wktContent.Substring(10, wktContent.Length - 10);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(1, wktContent.Length - 2);
                    // 按","分割后按" "分割
                    string[] points = wktContent.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                    // 只有一条折线只有一个part
                    partList.Add(pointList.Count);
                    foreach (string point in points)
                    {
                        string[] coordinates = point.Split(' ');
                        double x = Convert.ToDouble(coordinates[0]);
                        double y = Convert.ToDouble(coordinates[1]);
                        pointList.Add(new Point(x, y));
                    }
                    break;
                case PolylineType.MultiLineString:
                    wktContent = wktContent.Substring(10, wktContent.Length - 10);
                    wktContent = wktContent.Trim();
                    wktContent = wktContent.Substring(1, wktContent.Length - 2);
                    string[] lineParts = wktContent.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string line in lineParts)
                    {
                        partList.Add(pointList.Count);
                        string[] points1 = line.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                        foreach (var point in points1)
                        {
                            string[] coordinates = point.Split(' ');
                            double x = Convert.ToDouble(coordinates[0]);
                            double y = Convert.ToDouble(coordinates[1]);
                            pointList.Add(new Point(x, y));
                        }
                    }
                    break;
            }

            return new ShpPolyline(pointList, partList);
        }

        // 返回polyline的直角坐标系下的长度数组，数组长度为Polyline中的部件数量
        public double[] GetLength()
        {
            double[] result = new double[PartNumber];
            for (int i = 0; i < PartNumber; i++)
            {
                if (i == PartNumber - 1)
                {
                    double distance = 0;
                    for (int j = PartList[i]; j < PointNumber - 1; j++)
                    {
                        IPoint sp = PointList[j];
                        IPoint ep = PointList[j + 1];
                        distance += Math.Sqrt(Math.Pow((ep.GetX() - sp.GetX()), 2) + Math.Pow((ep.GetY() - sp.GetY()), 2));
                    }
                    result[i] = distance;
                }
                else
                {
                    double distance = 0;
                    for (int j = PartList[i]; j < PartList[i + 1] - 1; j++)
                    {
                        IPoint sp = PointList[j];
                        IPoint ep = PointList[j + 1];
                        distance += Math.Sqrt(Math.Pow((ep.GetX() - sp.GetX()), 2) + Math.Pow((ep.GetY() - sp.GetY()), 2));
                    }
                    result[i] = distance;
                }
            }

            return result;
        }

        // 按节点分割为单线
        public IEnumerable<ISingleLine> SplitByNode()
        {
            List<ISingleLine> singleLines = new List<ISingleLine>();
            for (int i = 0; i < PointNumber - 1; i++)
            {
                singleLines.Add(new SingleLine(PointList[i], PointList[i + 1]));
            }
            return singleLines;
        }

        // 按部件分割
        public IEnumerable<IShpPolyline> SplitByPart()
        {
            List<IShpPolyline> polylines = new List<IShpPolyline>();
            for (int i = 0; i < PartNumber; i++)
            {
                if (i == PartNumber - 1)
                {
                    List<IPoint> subPointList = PointList.Skip(PartList[i]).Take(PointNumber - PartList[i]).ToList();
                    polylines.Add(new ShpPolyline(subPointList));
                }
                else
                {
                    List<IPoint> subPointList = PointList.Skip(PartList[i]).Take(PartList[i + 1] - PartList[i]).ToList();
                    polylines.Add(new ShpPolyline(subPointList));
                }
            }

            return polylines;
        }

        // 输出为wkt格式
        public  string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();
            if (PartNumber == 1)
            {
                wktBuilder.Append("LINESTRING (");
                for (int i = 0; i < PointNumber; i++)
                {
                    string coordinates = $"{PointList[i].GetX()} {PointList[i].GetY()}";
                    wktBuilder.Append(coordinates);
                    if (i < PointNumber - 1)
                    {
                        wktBuilder.Append(",");
                    }
                }
                wktBuilder.Append(")");
            }

            if (PartNumber > 1)
            {
                wktBuilder.Append("MULTILINESTRING (");
                for (int i = 0; i < PartNumber; i++)
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    lineBuilder.Append("(");
                    if (i < PartNumber - 1)
                    {
                        for (int j = PartList[i]; j < PartList[i + 1]; j++)
                        {
                            string coordinates = $"{PointList[j].GetX()} {PointList[j].GetY()}";
                            lineBuilder.Append(coordinates);
                            if (j < PartList[i + 1] - 1)
                            {
                                lineBuilder.Append(",");
                            }
                        }
                        lineBuilder.Append(")");
                        wktBuilder.Append(lineBuilder.ToString());
                        lineBuilder.Clear();
                        wktBuilder.Append(",");
                    }
                    else
                    {
                        for (int j = PartList[i]; j < PointNumber; j++)
                        {
                            string coordinates = $"{PointList[j].GetX()} {PointList[j].GetY()}";
                            lineBuilder.Append(coordinates);
                            if (j < PointNumber - 1)
                            {
                                lineBuilder.Append(",");
                            }
                        }
                        lineBuilder.Append(")");
                        wktBuilder.Append(lineBuilder.ToString());
                        lineBuilder.Clear();
                    }
                }
                wktBuilder.Append(")");
            }

            return wktBuilder.ToString();
        }

        public string ExportToWktMysql()
        {
            StringBuilder wktBuilder = new StringBuilder();

            if (PartNumber > 0)
            {
                wktBuilder.Append("MULTILINESTRING (");
                for (int i = 0; i < PartNumber; i++)
                {
                    StringBuilder lineBuilder = new StringBuilder();
                    lineBuilder.Append("(");
                    if (i < PartNumber - 1)
                    {
                        for (int j = PartList[i]; j < PartList[i + 1]; j++)
                        {
                            string coordinates = $"{PointList[j].GetY()} {PointList[j].GetX()}";
                            lineBuilder.Append(coordinates);
                            if (j < PartList[i + 1] - 1)
                            {
                                lineBuilder.Append(",");
                            }
                        }
                        lineBuilder.Append(")");
                        wktBuilder.Append(lineBuilder.ToString());
                        lineBuilder.Clear();
                        wktBuilder.Append(",");
                    }
                    else
                    {
                        for (int j = PartList[i]; j < PointNumber; j++)
                        {
                            string coordinates = $"{PointList[j].GetY()} {PointList[j].GetX()}";
                            lineBuilder.Append(coordinates);
                            if (j < PointNumber - 1)
                            {
                                lineBuilder.Append(",");
                            }
                        }
                        lineBuilder.Append(")");
                        wktBuilder.Append(lineBuilder.ToString());
                        lineBuilder.Clear();
                    }
                }
                wktBuilder.Append(")");
            }

            return wktBuilder.ToString();
        }

        public virtual int ContentLength() 
        {
            return 2 + 16 + 2 + 2 + 2 * PartNumber + 8 * PointNumber;
        }

        public virtual ESRIShapeType GetFeatureType()
        {
            return ESRIShapeType.Polyline;
        }

        public virtual string GetGeometryType()
        {
            return "ShpPolyline";
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
            return PartNumber;
        }

        public int GetPointNumber()
        {
            return PointNumber;
        }

        public IPoint GetPointByIndex(int index)
        {
            if (index < -this.PointNumber || index >= this.PointNumber) throw new IndexOutOfRangeException();

            if (index < 0) index += this.PointNumber;

            return this.PointList[index];
        }

        public IEnumerable<IPoint> GetPart(int index)
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

        public IEnumerable<IEnumerable<IPoint>> GetPointList()
        {
            List<List<IPoint>> result = new List<List<IPoint>>();

            for (int i = 0; i < this.PartNumber; i++)
            {
                List<IPoint> singleLinePart = new List<IPoint>();
                int startLoc = this.PartList[i];
                int endLoc = -1;
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
                    singleLinePart.Add(this.PointList[j]);
                }

                result.Add(singleLinePart);
            }

            return result;
        }

        public IEnumerable<IPoint> GetPartByIndex(int index)
        {
            if (index < -this.PartNumber || index >= this.PartNumber)
            {
                throw new IndexOutOfRangeException();
            }

            if (index < 0) index += this.PartNumber;

            List<IPoint> result = new List<IPoint>();

            int startLoc = this.PartList[index];
            int endLoc;
            if (index < this.PartNumber - 1)
            {
                endLoc = this.PartList[index + 1];
            }
            else
            {
                endLoc = this.PointNumber;
            }

            for (int i = startLoc; i < endLoc; i++)
            {
                result.Add(this.PointList[i]);
            }

            return result;
        }

        public virtual string GetBaseGeometryType()
        {
            return "MultiLineString";
        }

        public virtual IShpGeometryBase Clone()
        {
            return new ShpPolyline(this);
        }
    }
}
