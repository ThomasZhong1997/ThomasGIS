using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.DataManagement;
using ThomasGIS.Geometries.OpenGIS;
using System.IO;
using ThomasGIS.TextParser;
using ThomasGIS.BaseConfiguration;

namespace ThomasGIS.Vector
{
    public class GeoJson : IGeoJson
    {
        private List<IGeometry> geometryList;

        public int GeometryCount => this.geometryList.Count;

        public GeoJson(string filePath, int openMode)
        {
            this.geometryList = new List<IGeometry>();

            using (StreamReader sr = new StreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                StringBuilder sb = new StringBuilder();
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine().Trim('\n').Trim('\r');
                    sb.Append(line);
                }
                sr.Close();

                string geoJsonString = sb.ToString();

                Init(geoJsonString);
            }
        }

        public GeoJson() 
        {
            this.geometryList = new List<IGeometry>();
        }

        private OpenGIS_Polygon ParsePolygon(JsonArray polygonArray, Dictionary<string, string> properties=null)
        {
            List<List<Point>> pointList = new List<List<Point>>();

            int partLength = polygonArray.GetLength();

            for (int i = 0; i < partLength; i++)
            {
                JsonArray partArray = polygonArray[i] as JsonArray;
                List<Point> partPointList = new List<Point>();
                for (int j = 0; j < partArray.GetLength(); j++)
                {
                    JsonArray onePoint = partArray[j] as JsonArray;
                    double x = Convert.ToDouble(onePoint[0].ToString());
                    double y = Convert.ToDouble(onePoint[1].ToString());
                    partPointList.Add(new Point(x, y));
                }
                pointList.Add(partPointList);
            }

            OpenGIS_Polygon polygon = new OpenGIS_Polygon(pointList, properties);
            return polygon;
        }

        private OpenGIS_Point ParsePoint(JsonArray pointArray, Dictionary<string, string> properties)
        {
            double x = Convert.ToDouble(pointArray[0].ToString());
            double y = Convert.ToDouble(pointArray[1].ToString());
            return new OpenGIS_Point(x, y, properties);
        }

        private OpenGIS_LineString ParseLineString(JsonArray lineStringArray, Dictionary<string, string> properties)
        {
            List<Point> pointList = new List<Point>();

            for (int i = 0; i < lineStringArray.GetLength(); i++)
            {
                JsonArray pointArray = lineStringArray[i] as JsonArray;
                double x = Convert.ToDouble(pointArray[0].ToString());
                double y = Convert.ToDouble(pointArray[1].ToString());
                pointList.Add(new Point(x, y));
            }

            OpenGIS_LineString lineString = new OpenGIS_LineString(pointList, properties);
            return lineString;
        }

        private OpenGIS_MultiPoint ParseMultiPoint(JsonArray multiPointArray, Dictionary<string, string> properties)
        {
            List<Point> pointList = new List<Point>();
            for (int i = 0; i < multiPointArray.GetLength(); i++)
            {
                JsonArray pointArray = multiPointArray[i] as JsonArray;
                double x = Convert.ToDouble(pointArray[0].ToString());
                double y = Convert.ToDouble(pointArray[1].ToString());
                pointList.Add(new Point(x, y));
            }

            OpenGIS_MultiPoint multiPoint = new OpenGIS_MultiPoint(pointList, properties);
            return multiPoint;
        }

        private OpenGIS_MultiLineString ParseMultiLineString(JsonArray multiLineStringArray, Dictionary<string, string> properties)
        {
            List<List<Point>> pointList = new List<List<Point>>();

            int partLength = multiLineStringArray.GetLength();

            for (int i = 0; i < partLength; i++)
            {
                JsonArray partArray = multiLineStringArray[i] as JsonArray;
                List<Point> partPointList = new List<Point>();
                for (int j = 0; j < partArray.GetLength(); j++)
                {
                    JsonArray onePoint = partArray[j] as JsonArray;
                    double x = Convert.ToDouble(onePoint[0].ToString());
                    double y = Convert.ToDouble(onePoint[1].ToString());
                    partPointList.Add(new Point(x, y));
                }
                pointList.Add(partPointList);
            }

            OpenGIS_MultiLineString multiLineString = new OpenGIS_MultiLineString(pointList, properties);
            return multiLineString;
        }

        private OpenGIS_MultiPolygon ParseMultiPolygon(JsonArray multiPolygonArray, Dictionary<string, string> properties)
        {
            List<List<List<Point>>> pointList = new List<List<List<Point>>>();

            int polygonNumber = multiPolygonArray.GetLength();
            for (int m = 0; m < polygonNumber; m++)
            {
                JsonArray polygon = multiPolygonArray[m] as JsonArray;
                int partLength =  polygon.GetLength();
                List<List<Point>> polygonPointList = new List<List<Point>>();
                for (int i = 0; i < partLength; i++)
                {
                    JsonArray partArray = polygon[i] as JsonArray;
                    List<Point> partPointList = new List<Point>();
                    for (int j = 0; j < partArray.GetLength(); j++)
                    {
                        JsonArray onePoint = partArray[j] as JsonArray;
                        double x = Convert.ToDouble(onePoint[0].ToString());
                        double y = Convert.ToDouble(onePoint[1].ToString());
                        partPointList.Add(new Point(x, y));
                    }
                    polygonPointList.Add(partPointList);
                }
                pointList.Add(polygonPointList);
            }

            OpenGIS_MultiPolygon multiPolygon = new OpenGIS_MultiPolygon(pointList, properties);
            return multiPolygon;
        }

        private IGeometry ParseGeometry(JsonObject geometryObject, Dictionary<string, string> properties)
        {
            List<string> objectKeys = geometryObject.GetKeys();
            if (!objectKeys.Contains("type") || !objectKeys.Contains("coordinates"))
            {
                throw new Exception("GeoJson Error 001: Geometry type/coordiantes not exist!");
            }

            string geometryType = geometryObject["type"].ToString();
            JsonArray coordinatesData = geometryObject["coordinates"] as JsonArray;
            switch (geometryType)
            {
                case "Point":
                    return ParsePoint(coordinatesData, properties);
                case "LineString":
                    return ParseLineString(coordinatesData, properties);
                case "Polygon":
                    return ParsePolygon(coordinatesData, properties);
                case "MultiPoint":
                    return ParseMultiPoint(coordinatesData, properties);
                case "MultiLineString":
                    return ParseMultiLineString(coordinatesData, properties);
                case "MultiPolygon":
                    return ParseMultiPolygon(coordinatesData, properties);
                default:
                    throw new Exception("GeoJson Error 002: Undefined Geometry Type");
            }
        }


        private IGeometry ParseFeature(JsonObject featureObject)
        {
            List<string> objectKeys = featureObject.GetKeys();
            if (!objectKeys.Contains("type") || !objectKeys.Contains("properties") || !objectKeys.Contains("geometry"))
            {
                throw new Exception("GeoJson Error 003: Feature type/properties/geometry not exist!");
            }

            string featureType = featureObject["type"].ToString();
            JsonObject propertiesObject = featureObject["properties"] as JsonObject;
            JsonObject geometryObject = featureObject["geometry"] as JsonObject;

            Dictionary<string, string> properties = new Dictionary<string, string>();

            if (objectKeys.Contains("id"))
            {
                properties.Add("id", featureObject["id"].ToString());
            }

            List<string> propertyKeys = propertiesObject.GetKeys();
            foreach (string key in propertyKeys)
            {
                properties.Add(key, propertiesObject[key].ToString());
            }

            return ParseGeometry(geometryObject, properties);
        }

        private void Init(string inputJson)
        {
            JsonDataBase result = JsonParser.Parse(inputJson);
            if (result.GetType() == typeof(JsonArray))
            {
                throw new Exception("GeoJson Error 004: GeoJson is an object, not an array!");
            }
            else
            {
                JsonObject resultObject = result as JsonObject;
                string geoJsonType = resultObject["type"].ToString().ToLower();
                IGeometry newGeometry;
                switch (geoJsonType)
                {
                    case "featurecollection":
                        JsonArray dataArray = resultObject["features"] as JsonArray;
                        for (int i = 0; i < dataArray.GetLength(); i++)
                        {
                            JsonObject oneFeature = dataArray[i] as JsonObject;
                            newGeometry = ParseFeature(oneFeature);
                            this.geometryList.Add(newGeometry);
                        }
                        break;
                    case "point":
                    case "linestring":
                    case "polygon":
                    case "multipoint":
                    case "multilinestring":
                    case "multipolygon":
                        newGeometry = ParseGeometry(resultObject, null);
                        this.geometryList.Add(newGeometry);
                        break;
                    case "geometrycollection":
                        if (!resultObject.GetKeys().Contains("geometries"))
                        {
                            throw new Exception("GeoJson Error 005: GeometryCollection Geometries not exist!");
                        }
                        JsonArray geometryArray = resultObject["geometries"] as JsonArray;
                        for (int i = 0; i < geometryArray.GetLength(); i++)
                        {
                            JsonObject geometryObject = geometryArray[i] as JsonObject;
                            newGeometry = ParseGeometry(geometryObject, null);
                            this.geometryList.Add(newGeometry);
                        }
                        break;
                    default:
                        throw new Exception("GeoJson Error 006: Unknown Geometry Type!");
                }
            }
        }

        public GeoJson(string inputJson)
        {
            this.geometryList = new List<IGeometry>();
            Init(inputJson);
        }

        public bool AddFeature(string wkt, Dictionary<string, string> properties=null)
        {
            IGeometry newGeometry = WKTParser.ParseWKT2OpenGIS(wkt, properties);
            this.geometryList.Add(newGeometry);
            return true;
        }

        public int GetFeatureNumber()
        {
            return this.GeometryCount;
        }

        public IEnumerable<IGeometry> GetFeatures()
        {
            return this.geometryList;
        }

        public bool RemoveFeature(int index)
        {
            if (index < -this.GeometryCount || index >= this.GeometryCount)
            {
                throw new IndexOutOfRangeException();
            }

            if (index < 0) index += this.GeometryCount;

            this.geometryList.RemoveAt(index);

            return true;
        }

        public IGeometry GetFeature(int index)
        {
            if (index < -this.GeometryCount || index >= this.GeometryCount)
            {
                throw new IndexOutOfRangeException();
            }

            if (index < 0) index += this.GeometryCount;

            return this.geometryList[index];
        }
    }
}
