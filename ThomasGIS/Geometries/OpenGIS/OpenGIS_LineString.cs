using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_LineString : ILineString
    {
        public List<IPoint> PointList { get; } = new List<IPoint>();

        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        public IPoint GetPointByIndex(int index)
        {
            if (index < -this.PointList.Count && index >= this.PointList.Count) return null;

            if (index < 0) index += this.PointList.Count;

            return this.PointList[index];
        }

        private void AddProperties(Dictionary<string, string> properties)
        {
            if (properties == null) return;

            foreach (KeyValuePair<string, string> oneProperty in properties)
            {
                this.Properties.Add(oneProperty.Key, oneProperty.Value);
            }
        }

        public double GetLength(CoordinateType coordinateType = CoordinateType.Projected)
        {
            if (coordinateType == CoordinateType.Geographic)
            {
                return DistanceCalculator.SpatialDistanceGeo(this.PointList);
            }

            return DistanceCalculator.SpatialDistance(this.PointList);
        }

        public IEnumerable<IPoint> GetPointList()
        {
            return this.PointList;
        }

        public OpenGIS_LineString(IEnumerable<IPoint> pointList, Dictionary<string, string> properties = null)
        {
            PointList.AddRange(pointList);
            AddProperties(properties);
        }

        public OpenGIS_LineString(string wkt, Dictionary<string, string> properties = null)
        {
            string wktContent = wkt.ToUpper();
            wktContent = wktContent.Substring(10, wktContent.Length - 10);
            wktContent = wktContent.Trim();
            wktContent = wktContent.Substring(1, wktContent.Length - 2);
            // 按","分割后按" "分割
            string[] points = wktContent.Split(',');
            // 只有一条折线只有一个part
            foreach (string point in points)
            {
                string[] coordinates = point.Trim(' ').Split(' ');
                double x = Convert.ToDouble(coordinates[0]);
                double y = Convert.ToDouble(coordinates[1]);
                PointList.Add(new Point(x, y));
            }

            AddProperties(properties);
        }

        public string ExportToWkt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("LINESTRING(");
            for (int i = 0; i < PointList.Count; i++)
            {
                sb.Append(PointList[i].GetX());
                sb.Append(" ");
                sb.Append(PointList[i].GetY());
                if (i <= PointList.Count - 1)
                {
                    sb.Append(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        public string GetGeometryType()
        {
            return "OpenGIS_LineString";
        }

        public string GetBaseGeometryType()
        {
            return "LineString";
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xMin = PointList.Min(item => item.GetX());
            double xMax = PointList.Max(item => item.GetX());
            double yMin = PointList.Min(item => item.GetY());
            double yMax = PointList.Max(item => item.GetY());

            return new BoundaryBox(xMin, yMin, xMax, yMax);
        }
    }
}
