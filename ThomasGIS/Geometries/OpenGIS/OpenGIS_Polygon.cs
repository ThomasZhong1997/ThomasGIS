using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;
using System.Linq;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_Polygon : IPolygon
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

        public OpenGIS_Polygon(string wkt, Dictionary<string, string> properties = null)
        {
            string wktContent = wkt.Substring(7, wkt.Length - 7);
            wktContent = wktContent.Trim();
            wktContent = wktContent.Substring(2, wktContent.Length - 4);
            string[] gonParts = wktContent.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string onePart in gonParts)
            {
                List<IPoint> partPointList = new List<IPoint>();
                string[] points = onePart.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string point in points)
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

        public OpenGIS_Polygon(IEnumerable<IEnumerable<IPoint>> pointList, Dictionary<string, string> properties = null)
        {
            for (int i = 0; i < pointList.Count(); i++)
            {
                this.PointList.Add(new List<IPoint>());
                for (int j = 0; j < pointList.ElementAt(i).Count(); j++)
                {
                    this.PointList[i].Add(pointList.ElementAt(i).ElementAt(j));
                }
            }

            AddProperties(properties);
        }

        public IEnumerable<IPoint> GetPartByIndex(int index)
        {
            if (index < -this.PointList.Count && index >= this.PointList.Count) return null;

            if (index < 0) index += this.PointList.Count;

            return this.PointList[index];
        }

        public double GetArea(CoordinateType coordinateType)
        {
            double area = 0;
            for (int i = 0; i < this.PointList.Count; i++)
            {
                if (coordinateType == CoordinateType.Geographic)
                {
                    area += DistanceCalculator.SpatialAreaGeo(this.PointList[i]);
                }
                else
                {
                    area += DistanceCalculator.SpatialArea(this.PointList[i]);
                }
            }

            return area;
        }

        public IEnumerable<IEnumerable<IPoint>> GetPointList()
        {
            return PointList;
        }

        public string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();

            wktBuilder.Append("POLYGON (");
            for (int i = 0; i < this.PointList.Count; i++)
            {
                StringBuilder innerBuilder = new StringBuilder();
                innerBuilder.Append("(");
                for (int j = 0; j < this.PointList[i].Count; j++)
                {
                    string coordinates = $"{PointList[i][j].GetX()} {PointList[i][j].GetY()}";
                    innerBuilder.Append(coordinates);
                    if (j < this.PointList[i].Count - 1)
                    {
                        innerBuilder.Append(",");
                    }
                }
                innerBuilder.Append(")");
                if (i < this.PointList.Count - 1)
                {
                    innerBuilder.Append(",");
                }
                wktBuilder.Append(innerBuilder.ToString());
            }
            wktBuilder.Append(")");

            return wktBuilder.ToString();
        }

        public string GetGeometryType()
        {
            return "OpenGIS_Polygon";
        }

        public string GetBaseGeometryType()
        {
            return "Polygon";
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
