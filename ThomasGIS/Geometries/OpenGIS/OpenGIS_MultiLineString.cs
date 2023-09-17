using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_MultiLineString : IGeometry
    {
        public List<List<IPoint>> PointList { get; } = new List<List<IPoint>>();
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        private void AddProperties(Dictionary<string, string> properties)
        {
            if (properties == null) return;

            foreach (KeyValuePair<string, string> oneProperty in properties)
            {
                this.Properties.Add(oneProperty.Key, oneProperty.Value);
            }
        }

        public OpenGIS_MultiLineString(IEnumerable<IEnumerable<IPoint>> lineStringList, Dictionary<string, string> properties = null)
        {

            for (int i = 0; i < lineStringList.Count(); i++)
            {
                this.PointList.Add(new List<IPoint>());
                for (int j = 0; j < lineStringList.ElementAt(i).Count(); j++)
                {
                    this.PointList[i].Add(lineStringList.ElementAt(i).ElementAt(j));
                }
            }

            AddProperties(properties);
        }

        public OpenGIS_MultiLineString(string wkt, Dictionary<string, string> properties = null)
        {
            string wktContent = wkt.ToUpper();
            wktContent = wktContent.Substring(15, wktContent.Length - 15);
            wktContent = wktContent.Trim();
            wktContent = wktContent.Substring(2, wktContent.Length - 4);
            string[] lineParts = wktContent.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lineParts)
            {
                List<IPoint> partPointList = new List<IPoint>();
                string[] points1 = line.Split(',');
                foreach (var point in points1)
                {
                    string[] coordinates = point.Trim(' ').Split(' ');
                    double x = Convert.ToDouble(coordinates[0]);
                    double y = Convert.ToDouble(coordinates[1]);
                    partPointList.Add(new Point(x, y));
                }
                this.PointList.Add(partPointList);
            }

            AddProperties(properties);
        }

        public string ExportToWkt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("MULTILINESTRING(");
            for (int i = 0; i < this.PointList.Count; i++)
            {
                StringBuilder oneLineStringSb = new StringBuilder();
                oneLineStringSb.Append("(");
                for (int j = 0; j < this.PointList[i].Count; j++)
                {
                    oneLineStringSb.Append(this.PointList[i][j].GetX());
                    oneLineStringSb.Append(" ");
                    oneLineStringSb.Append(this.PointList[i][j].GetY());
                    if (j < this.PointList[i].Count - 1)
                    {
                        oneLineStringSb.Append(",");
                    }
                }
                oneLineStringSb.Append(")");
                if (i < this.PointList.Count - 1) 
                {
                    oneLineStringSb.Append(",");
                }
            }
            sb.Append(")");
            return sb.ToString();
        }

        public string GetGeometryType()
        {
            return "OpenGIS_MultiLineString";
        }

        public string GetBaseGeometryType()
        {
            return "MultiLineString";
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;

            for (int i = 0; i < PointList.Count; i++)
            {
                List<IPoint> lineStringPoint = PointList[i];
                xMin = Math.Min(lineStringPoint.Min(item => item.GetX()), xMin);
                xMax = Math.Max(lineStringPoint.Max(item => item.GetX()), xMax);
                yMin = Math.Min(lineStringPoint.Min(item => item.GetY()), yMin);
                yMax = Math.Max(lineStringPoint.Max(item => item.GetY()), yMax);
            }

            return new BoundaryBox(xMin, yMin, xMax, yMax);
        }
    }
}
