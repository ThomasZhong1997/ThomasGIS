using iGeospatial.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Geometries.OpenGIS;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Geometries
{
    public static class GeometryConverter
    {
        // ThomasGIS 转 GEOS Geometry 通用方法
        public static iGeospatial.Geometries.Geometry ConvertToGEOSGeometry(ThomasGIS.Geometries.IGeometry geometry)
        {
            string thomasGISGeometryType = geometry.GetBaseGeometryType();
            iGeospatial.Geometries.GeometryFactory geometryFactory = new iGeospatial.Geometries.GeometryFactory();
            if (thomasGISGeometryType == "Point")
            {
                ThomasGIS.Geometries.IPoint shpPoint = geometry as ThomasGIS.Geometries.IPoint;
                return new iGeospatial.Geometries.Point(new iGeospatial.Coordinates.Coordinate(shpPoint.GetX(), shpPoint.GetY()), geometryFactory);
            }
            else if (thomasGISGeometryType == "SingleLine")
            {
                ThomasGIS.Geometries.ISingleLine singleLine = geometry as ThomasGIS.Geometries.ISingleLine;
                List<iGeospatial.Coordinates.Coordinate> lineStringCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                lineStringCoordinates.Add(new iGeospatial.Coordinates.Coordinate(singleLine.GetStartPoint().GetX(), singleLine.GetStartPoint().GetY()));
                lineStringCoordinates.Add(new iGeospatial.Coordinates.Coordinate(singleLine.GetEndPoint().GetX(), singleLine.GetEndPoint().GetY()));
                return new iGeospatial.Geometries.LineString(lineStringCoordinates.ToArray(), geometryFactory);
            }
            else if (thomasGISGeometryType == "LineString")
            {
                ThomasGIS.Geometries.ILineString lineString = geometry as ThomasGIS.Geometries.ILineString;
                List<ThomasGIS.Geometries.IPoint> pointList = lineString.GetPointList().ToList();
                List<iGeospatial.Coordinates.Coordinate> lineStringCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                for (int i = 0; i < pointList.Count; i++)
                {
                    ThomasGIS.Geometries.IPoint onePoint = pointList[i];
                    lineStringCoordinates.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                }
                return new iGeospatial.Geometries.LineString(lineStringCoordinates.ToArray(), geometryFactory);
            }
            else if (thomasGISGeometryType == "MultiLineString")
            {
                ThomasGIS.Geometries.IMultiLineString multiLineString = geometry as ThomasGIS.Geometries.IMultiLineString;
                List<IEnumerable<ThomasGIS.Geometries.IPoint>> pointList = multiLineString.GetPointList().ToList();

                iGeospatial.Geometries.LineString[] lineStringList = new iGeospatial.Geometries.LineString[pointList.Count];
                for (int i = 0; i < pointList.Count; i++)
                {
                    List<ThomasGIS.Geometries.IPoint> partPointList = pointList[i].ToList();
                    List<iGeospatial.Coordinates.Coordinate> partLineString = new List<iGeospatial.Coordinates.Coordinate>();
                    for (int j = 0; j < partPointList.Count; j++)
                    {
                        ThomasGIS.Geometries.IPoint onePoint = partPointList[j];
                        partLineString.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                    }
                    lineStringList[i] = new iGeospatial.Geometries.LineString(partLineString.ToArray(), geometryFactory);
                }
                return new iGeospatial.Geometries.MultiLineString(lineStringList, geometryFactory);
            }
            else if (thomasGISGeometryType == "Polygon")
            {
                ThomasGIS.Geometries.IPolygon polygon = geometry as ThomasGIS.Geometries.IPolygon;
                List<IEnumerable<IPoint>> pointList = polygon.GetPointList().ToList();

                List<ThomasGIS.Geometries.IPoint> externalRingPointList = pointList[0].ToList();
                List<iGeospatial.Coordinates.Coordinate> externalRingCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                for (int i = 0; i < externalRingPointList.Count; i++)
                {
                    ThomasGIS.Geometries.IPoint onePoint = externalRingPointList[i];
                    externalRingCoordinates.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                }
                iGeospatial.Coordinates.ICoordinateList externalLinearRingCoordinateList = new iGeospatial.Coordinates.CoordinateCollection(externalRingCoordinates.ToArray());
                iGeospatial.Geometries.LinearRing shellLinearRing = new iGeospatial.Geometries.LinearRing(externalLinearRingCoordinateList, geometryFactory);

                int holeNumber = pointList.Count - 1;
                iGeospatial.Geometries.LinearRing[] holeList = new iGeospatial.Geometries.LinearRing[holeNumber];
                for (int i = 1; i < pointList.Count; i++)
                {
                    List<ThomasGIS.Geometries.IPoint> internalRingPointList = pointList[i].ToList();
                    List<iGeospatial.Coordinates.Coordinate> internalRingCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                    for (int j = 0; j < internalRingPointList.Count; j++)
                    {
                        ThomasGIS.Geometries.IPoint onePoint = internalRingPointList[j];
                        internalRingCoordinates.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                    }
                    iGeospatial.Coordinates.ICoordinateList internalLinearRingCoordinateList = new iGeospatial.Coordinates.CoordinateCollection(externalRingCoordinates.ToArray());
                    holeList[i - 1] = new iGeospatial.Geometries.LinearRing(internalLinearRingCoordinateList, geometryFactory);
                }

                return new iGeospatial.Geometries.Polygon(shellLinearRing, holeList, geometryFactory);
            }
            else if (thomasGISGeometryType == "MultiPolygon")
            {
                ThomasGIS.Geometries.IMultiPolygon multiPolygon = geometry as ThomasGIS.Geometries.IMultiPolygon;
                List<IEnumerable<IEnumerable<IPoint>>> mPointList = multiPolygon.GetPointList().ToList();

                iGeospatial.Geometries.Polygon[] polygons = new iGeospatial.Geometries.Polygon[mPointList.Count];

                for (int m = 0; m < mPointList.Count; m++)
                {
                    List<IEnumerable<IPoint>> pointList = mPointList[m].ToList();
                    List<ThomasGIS.Geometries.IPoint> externalRingPointList = pointList[0].ToList();
                    List<iGeospatial.Coordinates.Coordinate> externalRingCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                    for (int i = 0; i < externalRingPointList.Count; i++)
                    {
                        ThomasGIS.Geometries.IPoint onePoint = externalRingPointList[i];
                        externalRingCoordinates.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                    }
                    iGeospatial.Coordinates.ICoordinateList externalLinearRingCoordinateList = new iGeospatial.Coordinates.CoordinateCollection(externalRingCoordinates.ToArray());
                    iGeospatial.Geometries.LinearRing shellLinearRing = new iGeospatial.Geometries.LinearRing(externalLinearRingCoordinateList, geometryFactory);

                    int holeNumber = pointList.Count - 1;
                    iGeospatial.Geometries.LinearRing[] holeList = new iGeospatial.Geometries.LinearRing[holeNumber];
                    for (int i = 1; i < pointList.Count; i++)
                    {
                        List<ThomasGIS.Geometries.IPoint> internalRingPointList = pointList[i].ToList();
                        List<iGeospatial.Coordinates.Coordinate> internalRingCoordinates = new List<iGeospatial.Coordinates.Coordinate>();
                        for (int j = 0; j < internalRingPointList.Count; j++)
                        {
                            ThomasGIS.Geometries.IPoint onePoint = internalRingPointList[j];
                            internalRingCoordinates.Add(new iGeospatial.Coordinates.Coordinate(onePoint.GetX(), onePoint.GetY()));
                        }
                        iGeospatial.Coordinates.ICoordinateList internalLinearRingCoordinateList = new iGeospatial.Coordinates.CoordinateCollection(externalRingCoordinates.ToArray());
                        holeList[i - 1] = new iGeospatial.Geometries.LinearRing(internalLinearRingCoordinateList, geometryFactory);
                    }

                    polygons[m] = new iGeospatial.Geometries.Polygon(shellLinearRing, holeList, geometryFactory);
                }

                return new iGeospatial.Geometries.MultiPolygon(polygons, geometryFactory);
            }
            else
            {
                throw new Exception("几何类型目前未支持！");
            }
        }

        public static IGeometry ConvertToShapefileGeometry(iGeospatial.Geometries.Geometry geometry)
        {
            string wkt = geometry.ToWKT();
            if (wkt == "EMPTY GeometryCollection")
            {
                return null;
            }
            else
            {
                wkt = wkt.ToLower();
                // "POINT()"
                if (wkt.Length < 7) return null;
                string flag = wkt.Substring(0, 5);
                if (flag == "point") return new ShpPoint(wkt);

                // "Polygon()"
                if (wkt.Length < 9) return null;
                flag = wkt.Substring(0, 7);
                if (flag == "polygon") return new ShpPolygon(wkt);

                // "LineString()"
                if (wkt.Length < 10) return null;
                flag = wkt.Substring(0, 10);
                if (flag == "linestring") return new ShpPolyline(wkt);

                // "MultiPolygon()"
                if (wkt.Length < 12) return null;
                flag = wkt.Substring(0, 12);
                if (flag == "multipolygon") return new ShpPolygon(wkt);

                // "MultiLineString()"
                if (wkt.Length < 17) return null;
                flag = wkt.Substring(0, 17);
                if (flag == "multilinestring") return new ShpPolyline(wkt);

                return null;
            }
        }

        public static IGeometry ConvertToOpenGISGeometry(iGeospatial.Geometries.Geometry geometry)
        {
            string wkt = geometry.ToWKT();
            if (wkt != "EMPTY GeometryCollection")
            {
                return null;
            }
            else
            {
                wkt = wkt.ToLower();
                // "POINT()"
                if (wkt.Length < 7) return null;
                string flag = wkt.Substring(0, 5);
                if (flag == "point") return new OpenGIS_Point(wkt);

                // "Polygon()"
                if (wkt.Length < 9) return null;
                flag = wkt.Substring(0, 7);
                if (flag == "polygon") return new OpenGIS_Polygon(wkt);

                // "LineString()"
                if (wkt.Length < 12) return null;
                flag = wkt.Substring(0, 10);
                if (flag == "linestring") return new OpenGIS_LineString(wkt);
                if (flag == "multipoint") return new OpenGIS_MultiPoint(wkt);

                // "MultiPolygon()"
                if (wkt.Length < 14) return null;
                flag = wkt.Substring(0, 14);
                if (flag == "multipolygon") return new OpenGIS_MultiPolygon(wkt);

                // "MultiLineString()"
                if (wkt.Length < 17) return null;
                flag = wkt.Substring(0, 17);
                if (flag == "multilinestring") return new OpenGIS_MultiLineString(wkt);

                return null;
            }
        }
    }
}
