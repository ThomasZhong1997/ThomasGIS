using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.BaseConfiguration;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.OpenGIS;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Grids.Basic;
using ThomasGIS.Vector;

namespace ThomasGIS.DataManagement
{
    public static class DataConvertTool
    {
        public static IShapefile RasterToPolygon_Scan(Raster inputRaster)
        {
            int bands = inputRaster.GetRasterBandNumber();
            int rows = inputRaster.Rows;
            int cols = inputRaster.Cols;

            double[,,] rasterData = new double[rows, cols, bands];

            for (int i = 0; i < bands; i++) 
            {
                RasterBand oneBand = inputRaster.GetRasterBand(i) as RasterBand;
                oneBand.ReadData(out var data);
                for (int m = 0; m < rows; m++)
                {
                    for (int n = 0; n < cols; n++)
                    {
                        rasterData[m, n, i] = data[m, n];
                    }
                }
            }

            bool[,] edgeFlag = new bool[rows, cols];

            for (int i = 0; i < rows - 1; i += 1)
            {
                for (int j = 0; j < cols - 1; j += 1)
                {
                    int colorFlag = (int)(rasterData[i, j, 0] * 255 * 255 + rasterData[i, j, 1] * 255 + rasterData[i, j, 2]);
                    int colorFlag_1 = (int)(rasterData[i + 1, j, 0] * 255 * 255 + rasterData[i + 1, j, 1] * 255 + rasterData[i + 1, j, 2]);
                    int colorFlag_2 = (int)(rasterData[i, j + 1, 0] * 255 * 255 + rasterData[i, j + 1, 1] * 255 + rasterData[i, j + 1, 2]);
                    int colorFlag_3 = (int)(rasterData[i + 1, j + 1, 0] * 255 * 255 + rasterData[i + 1, j + 1, 1] * 255 + rasterData[i + 1, j + 1, 2]);
                    // 四个都相等就不是边界了
                    if (colorFlag == colorFlag_1 && colorFlag_1 == colorFlag_2 && colorFlag_2 == colorFlag_3)
                    {
                        continue;
                    }

                    // 否则四个都是潜在的边界
                    edgeFlag[i, j] = true;
                    edgeFlag[i + 1, j] = true;
                    edgeFlag[i, j + 1] = true;
                    edgeFlag[i + 1, j + 1] = true;
                }
            }

            // 用于计算shapefile点的坐标
            double xmin = inputRaster.RasterXMin;
            double ymax = inputRaster.RasterYMax;
            double xScale = inputRaster.XScale;
            double yScale = inputRaster.YScale;

            int[,] direction = { { -1, 1 }, { 0, 1 }, { 1, 1 }, { 1, 0 }, { 1, -1 }, { 0, -1 }, { -1, -1 }, { -1, 0 } };
            
            // 顺序遍历边界，顺时针的话先向右再向下，再向左，再向上
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    // 如果不是边界，则跳过，是边界则判断一下是哪个颜色的边界
                    if (edgeFlag[i, j] == false) continue;
                    int colorFlag = (int)(rasterData[i, j, 0] * 255 * 255 + rasterData[i, j, 1] * 255 + rasterData[i, j, 2]);

                    // 向顺时针方向寻找可能存在的连续的边界
                }
            }

            return null;
        }

        public static IEnumerable<IShapefile> GeoJson2Shapefile(IGeoJson geoJson, CoordinateBase coordinateSystem=null)
        {
            IShapefile pointShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Point);
            IShapefile polylineShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);
            IShapefile polygonShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);

            pointShapefile.SetCoordinateRef(coordinateSystem);
            polylineShapefile.SetCoordinateRef(coordinateSystem);
            polygonShapefile.SetCoordinateRef(coordinateSystem);

            HashSet<string> fieldNames = new HashSet<string>();
            for (int i = 0; i < geoJson.GetFeatureNumber(); i++)
            {
                IGeometry oneGeometry = geoJson.GetFeature(i);
                switch (oneGeometry.GetGeometryType())
                {
                    case "OpenGIS_Point":
                        OpenGIS_Point point = oneGeometry as OpenGIS_Point;
                        foreach (string key in point.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    case "OpenGIS_LineString":
                        OpenGIS_LineString linestring = oneGeometry as OpenGIS_LineString;
                        foreach (string key in linestring.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    case "OpenGIS_MultiLineString":
                        OpenGIS_MultiLineString multiLineString = oneGeometry as OpenGIS_MultiLineString;
                        foreach (string key in multiLineString.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    case "OpenGIS_Polygon":
                        OpenGIS_Polygon polygon = oneGeometry as OpenGIS_Polygon;
                        foreach (string key in polygon.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    case "OpenGIS_MultiPolygon":
                        OpenGIS_MultiPolygon multiPolygon = oneGeometry as OpenGIS_MultiPolygon;
                        foreach (string key in multiPolygon.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    case "OpenGIS_MultiPoint":
                        OpenGIS_MultiPoint multiPoint = oneGeometry as OpenGIS_MultiPoint;
                        foreach (string key in multiPoint.Properties.Keys)
                        {
                            fieldNames.Add(key);
                        }
                        break;
                    default:
                        break;
                }
            }

            int fieldLength = Convert.ToInt32(Configuration.GetConfiguration("geojson.convert.string.length"));

            foreach (string key in fieldNames)
            {
                pointShapefile.AddField(key, DBFFieldType.Char, fieldLength, 0);
                polylineShapefile.AddField(key, DBFFieldType.Char, fieldLength, 0);
                polygonShapefile.AddField(key, DBFFieldType.Char, fieldLength, 0);
            }

            for (int i = 0; i < geoJson.GetFeatureNumber(); i++)
            {
                IGeometry oneGeometry = geoJson.GetFeature(i);
                Dictionary<string, object> properties = new Dictionary<string, object>();
                switch (oneGeometry.GetGeometryType())
                {
                    case "OpenGIS_Point":
                        OpenGIS_Point point = oneGeometry as OpenGIS_Point;
                        foreach (string key in point.Properties.Keys)
                        {
                            properties.Add(key, point.Properties[key]);
                        }
                        pointShapefile.AddFeature(new ShpPoint(point.ExportToWkt()), properties);
                        break;
                    case "OpenGIS_LineString":
                        OpenGIS_LineString linestring = oneGeometry as OpenGIS_LineString;
                        foreach (string key in linestring.Properties.Keys)
                        {
                            properties.Add(key, linestring.Properties[key]);
                        }
                        polylineShapefile.AddFeature(linestring.ExportToWkt(), properties);
                        break;
                    case "OpenGIS_MultiLineString":
                        OpenGIS_MultiLineString multiLineString = oneGeometry as OpenGIS_MultiLineString;
                        foreach (string key in multiLineString.Properties.Keys)
                        {
                            properties.Add(key, multiLineString.Properties[key]);
                        }
                        polylineShapefile.AddFeature(multiLineString.ExportToWkt(), properties);
                        break;
                    case "OpenGIS_Polygon":
                        OpenGIS_Polygon polygon = oneGeometry as OpenGIS_Polygon;
                        foreach (string key in polygon.Properties.Keys)
                        {
                            properties.Add(key, polygon.Properties[key]);
                        }
                        polygonShapefile.AddFeature(polygon.ExportToWkt(), properties);
                        break;
                    case "OpenGIS_MultiPolygon":
                        OpenGIS_MultiPolygon multiPolygon = oneGeometry as OpenGIS_MultiPolygon;
                        foreach (string key in multiPolygon.Properties.Keys)
                        {
                            properties.Add(key, multiPolygon.Properties[key]);
                        }
                        polygonShapefile.AddFeature(multiPolygon.ExportToWkt(), properties);
                        break;
                    case "OpenGIS_MultiPoint":
                        OpenGIS_MultiPoint multiPoint = oneGeometry as OpenGIS_MultiPoint;
                        foreach (string key in multiPoint.Properties.Keys)
                        {
                            properties.Add(key, multiPoint.Properties[key]);
                        }
                        for (int j = 0; j < multiPoint.PointList.Count; j++)
                        {
                            pointShapefile.AddFeature(multiPoint.PointList[j].ExportToWkt(), properties);
                        }
                        break;
                    default:
                        break;
                }
            }
             
            List<IShapefile> result = new List<IShapefile>();

            if (pointShapefile.GetFeatureNumber() != 0)
            {
                result.Add(pointShapefile);
            }

            if (polylineShapefile.GetFeatureNumber() != 0)
            {
                result.Add(polylineShapefile);
            }

            if (polygonShapefile.GetFeatureNumber() != 0)
            {
                result.Add(polygonShapefile);
            }

            return result;
        }

        public static IGeoJson Shapefile2GeoJson(IShapefile shapefile)
        {
            GeoJson newGeoJson = new GeoJson();

            List<string> fieldName = shapefile.GetFieldNames().ToList();

            for (int i = 0; i < shapefile.GetFeatureNumber(); i++)
            {
                Dictionary<string, string> properties = new Dictionary<string, string>();
                foreach (string key in fieldName)
                {
                    properties.Add(key, shapefile.GetFieldValueAsString(i, key));
                }
                newGeoJson.AddFeature(shapefile.GetFeature(i).ExportToWkt(), properties);
            }

            return newGeoJson;
        }
    }
}
