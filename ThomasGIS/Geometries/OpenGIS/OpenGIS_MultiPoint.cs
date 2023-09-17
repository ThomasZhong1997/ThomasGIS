using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_MultiPoint : IGeometry
    {
        public List<IPoint> PointList { get; } = new List<IPoint>();

        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        public OpenGIS_MultiPoint(IEnumerable<IPoint> pointList, Dictionary<string, string> properties = null)
        {
            this.PointList.AddRange(pointList);
            if (properties == null)
            {
                foreach (KeyValuePair<string, string> oneProperty in properties)
                {
                    Properties.Add(oneProperty.Key, oneProperty.Value);
                }
            }
        }

        public OpenGIS_MultiPoint(string wkt, Dictionary<string, string> properties = null)
        {
            wkt = wkt.ToLower();
            string typeToken = wkt.Substring(0, 10);
            if (typeToken != "multipoint")
            {
                throw new Exception("WKT Error 004: MultiPoint Token Error!");
            }
            string wktContent = wkt.Substring(11, wkt.Length - 12);
            string[] points = wktContent.Split(',');
            for (int i = 0; i < points.Length; i++)
            {
                string[] coordiantes = points[i].Trim(' ').Split(' ');
                double x = Convert.ToDouble(coordiantes[0]);
                double y = Convert.ToDouble(coordiantes[1]);
                this.PointList.Add(new Point(x, y));
            }

            AddProperties(properties);
        }

        private void AddProperties(Dictionary<string, string> properties)
        {
            if (properties == null) return;

            foreach (KeyValuePair<string, string> oneProperty in properties)
            {
                this.Properties.Add(oneProperty.Key, oneProperty.Value);
            }
        }

        public string ExportToWkt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MultiPoint(");
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
            return "OpenGIS_MultiPoint";
        }

        public string GetBaseGeometryType()
        {
            return "MultiPoint";
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
