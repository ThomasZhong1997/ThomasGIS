using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Helpers;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_MultiPolygon : IMultiPolygon
    {
        public List<List<List<IPoint>>> PointList = new List<List<List<IPoint>>>();
        public Dictionary<string, string> Properties = new Dictionary<string, string>();

        private void AddProperties(Dictionary<string, string> properties)
        {
            if (properties == null) return;

            foreach (KeyValuePair<string, string> oneProperty in properties)
            {
                this.Properties.Add(oneProperty.Key, oneProperty.Value);
            }
        }

        public OpenGIS_MultiPolygon(IEnumerable<IEnumerable<IEnumerable<IPoint>>> pointList, Dictionary<string, string> properties)
        {
            for (int i = 0; i < pointList.Count(); i++)
            {
                List<List<IPoint>> onePolygonList = new List<List<IPoint>>();
                for (int j = 0; j < pointList.ElementAt(i).Count(); j++)
                {
                    List<IPoint> onePartList = new List<IPoint>();
                    for (int k = 0; k < pointList.ElementAt(i).ElementAt(j).Count(); k++)
                    {
                        onePartList.Add(pointList.ElementAt(i).ElementAt(j).ElementAt(k));
                    }
                    onePolygonList.Add(onePartList);
                }
                this.PointList.Add(onePolygonList);
            }

            AddProperties(properties);
        }
 
        public OpenGIS_MultiPolygon(string wkt, Dictionary<string, string> properties = null)
        {
            string wktContent = wkt.ToUpper();
            wktContent = wkt.Substring(12, wkt.Length - 12);
            wktContent = wktContent.Trim();
            wktContent = wktContent.Substring(3, wktContent.Length - 6);
            string[] polygons = wktContent.Split(new string[] { ")),((", ")), ((" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string polygon in polygons)
            {
                List<List<IPoint>> polygonList = new List<List<IPoint>>();
                string[] gonParts1 = polygon.Split(new string[] { "),(", "), (" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string onePart in gonParts1)
                {
                    List<IPoint> partList = new List<IPoint>();
                    string[] points = onePart.Split(new string[] { ",", ", " }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (string point in points)
                    {
                        string[] coordinates = point.Trim(' ').Split(' ');
                        double x = Convert.ToDouble(coordinates[0]);
                        double y = Convert.ToDouble(coordinates[1]);
                        partList.Add(new Point(x, y));
                    }
                    polygonList.Add(partList);
                }
                this.PointList.Add(polygonList);
            }

            if (properties != null)
            {
                AddProperties(properties);
            }
        }

        public IEnumerable<IEnumerable<IPoint>> GetPolygon(int index)
        {
            if (index < -this.PointList.Count && index >= this.PointList.Count) return null;

            if (index < 0) index += this.PointList.Count;

            return this.PointList[index];
        }

        public double[] GetArea(CoordinateType coordinateType = CoordinateType.Projected)
        {
            double[] areas = new double[this.PointList.Count];

            for (int i = 0; i < this.PointList.Count; i++)
            {
                double area = 0;
                for (int j = 0; j < this.PointList[i].Count; j++)
                {
                    if (coordinateType == CoordinateType.Geographic)
                    {
                        area += DistanceCalculator.SpatialAreaGeo(this.PointList[i][j]);
                    }
                    else
                    {
                        area += DistanceCalculator.SpatialArea(this.PointList[i][j]);
                    }
                }
                areas[i] = area;
            }

            return areas;
        }

        public IEnumerable<IEnumerable<IEnumerable<IPoint>>> GetPointList()
        {
            return this.PointList;
        }

        public string GetGeometryType()
        {
            return "OpenGIS_MultiPolygon";
        }

        public string ExportToWkt()
        {
            StringBuilder wktBuilder = new StringBuilder();
            wktBuilder.Append("MULTIPOLYGON (");
            for (int i = 0; i < this.PointList.Count; i++)
            {
                StringBuilder polygonBuilder = new StringBuilder();
                polygonBuilder.Append("(");
                for (int j = 0; j < this.PointList[i].Count; j++)
                {
                    StringBuilder partBuilder = new StringBuilder();
                    partBuilder.Append("(");
                    for (int k = 0; k < this.PointList[i][j].Count; k++)
                    {
                        string coordinates = $"{PointList[i][j][k].GetX()} {PointList[i][j][k].GetY()}";
                        partBuilder.Append(coordinates);
                        if (k < this.PointList[i][j].Count - 1)
                        {
                            partBuilder.Append(",");
                        }
                    }
                    partBuilder.Append(")");
                    polygonBuilder.Append(partBuilder.ToString());
                    if (j < this.PointList[i].Count - 1)
                    {
                        polygonBuilder.Append(",");
                    }
                }
                polygonBuilder.Append(")");
                wktBuilder.Append(polygonBuilder.ToString());
                if (i < this.PointList.Count - 1)
                {
                    wktBuilder.Append(",");
                }
            }

            wktBuilder.Append(")");
            return wktBuilder.ToString();
        }

        public string GetBaseGeometryType()
        {
            return "MultiPolygon";
        }

        public BoundaryBox GetBoundaryBox()
        {
            double xMin = double.MaxValue;
            double xMax = double.MinValue;
            double yMin = double.MaxValue;
            double yMax = double.MinValue;

            for (int i = 0; i < PointList.Count; i++)
            {
                for (int j = 0; j < PointList[i].Count; j++)
                {
                    List<IPoint> lineStringPoint = PointList[i][j];
                    xMin = Math.Min(lineStringPoint.Min(item => item.GetX()), xMin);
                    xMax = Math.Max(lineStringPoint.Max(item => item.GetX()), xMax);
                    yMin = Math.Min(lineStringPoint.Min(item => item.GetY()), yMin);
                    yMax = Math.Max(lineStringPoint.Max(item => item.GetY()), yMax);
                }
            }

            return new BoundaryBox(xMin, yMin, xMax, yMax);
        }
    }
}
