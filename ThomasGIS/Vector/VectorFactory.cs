using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Vector
{
    public enum VectorOpenMode
    {
        Common,
        ReadOnly,
        ShpOnly,
        SafeRead
    }

    public class VectorFactory
    {
        public static Shapefile OpenShapefile(string path, VectorOpenMode mode = VectorOpenMode.SafeRead)
        {
            if (mode == VectorOpenMode.SafeRead)
            {
                return new Shapefile(path, 1);
            }
            return new Shapefile(path, mode);
        }

        public static Shapefile CreateShapefile(ESRIShapeType type)
        {
            return new Shapefile(type);
        }

        public static bool CreateShapefileFromKML(string path, out IShapefile pointShapefile, out IShapefile polylineShapefile, out IShapefile polygonShapefile)
        {
            pointShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Point);
            polylineShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);
            polygonShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polygon);

            pointShapefile.AddField("ID", DBFFieldType.Char, 20, 0);
            polylineShapefile.AddField("ID", DBFFieldType.Char, 20, 0);
            polygonShapefile.AddField("ID", DBFFieldType.Char, 20, 0);

            pointShapefile.AddField("Name", DBFFieldType.Char, 20, 0);
            polylineShapefile.AddField("Name", DBFFieldType.Char, 20, 0);
            polygonShapefile.AddField("Name", DBFFieldType.Char, 20, 0);

            pointShapefile.SetCoordinateRef(CoordinateGenerator.CRSParseFromEPSG(4326));
            polylineShapefile.SetCoordinateRef(CoordinateGenerator.CRSParseFromEPSG(4326));
            polygonShapefile.SetCoordinateRef(CoordinateGenerator.CRSParseFromEPSG(4326));

            //try
            //{
                // 打开 KML 文档
                XmlDocument kmlDocument = new XmlDocument();
                kmlDocument.Load(path);

                // 找到节点 Root
                XmlNodeList rootNodeList = kmlDocument.GetElementsByTagName("kml");
                if (rootNodeList.Count != 1)
                {
                    throw new Exception("非法的Xml文件：缺失KML文件头，转换失败");
                }
                XmlNode root = rootNodeList.Item(0);

                // 找到 KML 版本号
                string[] kmlVersionString = root.Attributes.GetNamedItem("xmlns").Value.Split('/');
                string kmlVersion = kmlVersionString[kmlVersionString.Length - 1];

                // 找到 KML 中存储空间位置的 KeyList
                XmlNodeList placeMarkNodeList = kmlDocument.GetElementsByTagName("Placemark");
                if (placeMarkNodeList.Count == 0)
                {
                    throw new Exception("非法的Xml文件：缺失KML空间属性，转换失败");
                }

                // 解析每一个 PlaceMarker 中的空间位置
                for (int i = 0; i < placeMarkNodeList.Count; i++)
                {
                    Dictionary<string, object> properties = new Dictionary<string, object>();
                    // PlaceMarker 的 ID
                    XmlNode onePlaceMark = placeMarkNodeList.Item(i);
                    string placeMarkID = onePlaceMark.Attributes.GetNamedItem("id").Value;
                    properties.Add("ID", placeMarkID);

                    XmlNodeList placeMarkChildren = onePlaceMark.ChildNodes;
                    IShpGeometryBase geometry = null;
                    foreach (XmlNode oneChild in placeMarkChildren)
                    {
                        if (oneChild.Name == "name") 
                        {
                            string featureName = oneChild.InnerText;
                            properties.Add("Name", featureName);
                        }

                        if (oneChild.Name == "Point")
                        {
                            geometry = KMLNodeToPoint(oneChild);
                            if (geometry != null)
                            {
                                pointShapefile.AddFeature(geometry, properties);
                            }
                            break;
                        }
                        else if (oneChild.Name == "LineString")
                        {
                            geometry = KMLNodeToPolyline(oneChild);
                            if (geometry != null)
                            {
                                polylineShapefile.AddFeature(geometry, properties);
                            }
                            break;
                        }
                        else if (oneChild.Name == "Polygon")
                        {
                            geometry = KMLNodeToPolygon(oneChild);
                            if (geometry != null) 
                            {
                                polygonShapefile.AddFeature(geometry, properties);
                            }
                            break;
                        }
                    }
                }
            //}
            //catch (Exception e)
            //{
            //    Console.WriteLine(e.Message);
            //}

            return true;
        }

        private static ShpPoint KMLNodeToPoint(XmlNode pointNode)
        {
            XmlNodeList xmlNodeList = pointNode.ChildNodes;
            foreach (XmlNode oneNode in xmlNodeList)
            {
                if (oneNode.Name == "coordinates")
                {
                    string coordinate = oneNode.InnerText.Trim('\n').Trim('\t').Trim('\n');
                    string[] items = coordinate.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries);
                    double x = Convert.ToDouble(items[0]);
                    double y = Convert.ToDouble(items[1]);
                    return new ShpPoint(x, y);
                }
            }
            return null;
        }

        private static ShpPolyline KMLNodeToPolyline(XmlNode polylineNode)
        {
            XmlNodeList xmlNodeList = polylineNode.ChildNodes;

            foreach (XmlNode oneNode in xmlNodeList)
            {
                if (oneNode.Name == "coordinates")
                {
                    string coordinateString = oneNode.InnerText.Trim('\n').Trim('\t').Trim('\n');
                    ShpPolyline newPolyline = new ShpPolyline();
                    List<Point> pointList = new List<Point>();

                    string[] _3DPointList = coordinateString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                    for (int i = 0; i < _3DPointList.Length; i++)
                    {
                        string onePoint = _3DPointList[i];
                        string[] items = onePoint.Split(',');
                        double x = Convert.ToDouble(items[0]);
                        double y = Convert.ToDouble(items[1]);
                        pointList.Add(new Point(x, y));
                    }

                    newPolyline.AddPart(pointList);


                    return newPolyline;
                }
            }
            return null;
        }

        private static ShpPolygon KMLNodeToPolygon(XmlNode polygonNode) 
        {
            XmlNodeList xmlNodeList = polygonNode.ChildNodes;
            foreach (XmlNode xmlNode in xmlNodeList)
            {
                if (xmlNode.Name == "outerBoundaryIs")
                {
                    XmlNodeList boundaryChildren = xmlNode.ChildNodes;
                    foreach (XmlNode targetLinearRing in boundaryChildren)
                    {
                        if (targetLinearRing.Name == "LinearRing")
                        {
                            XmlNodeList linearRingChildren = targetLinearRing.ChildNodes;
                            foreach (XmlNode targetCoordinate in linearRingChildren)
                            {
                                if (targetCoordinate.Name == "coordinates")
                                {
                                    string coordinateString = targetCoordinate.InnerText.Trim('\n').Trim('\t').Trim('\n');

                                    ShpPolygon newPolygon = new ShpPolygon();
                                    List<Point> pointList = new List<Point>();

                                    string[] _3DPointList = coordinateString.Split(new string[] { " " }, StringSplitOptions.RemoveEmptyEntries);
                                    for (int i = 0; i < _3DPointList.Length; i++)
                                    {
                                        string onePoint = _3DPointList[i];
                                        string[] items = onePoint.Split(',');
                                        double x = Convert.ToDouble(items[0]);
                                        double y = Convert.ToDouble(items[1]);
                                        pointList.Add(new Point(x, y));
                                    }

                                    newPolygon.AddPart(pointList);

                                    return newPolygon;
                                }
                            }
                        }
                    }
                }
            }

            return null;
        }

        public static IShapefile ParseOSMToRoadShapefile(string OSMFilePath)
        {
            XmlDocument OSMDocument = new XmlDocument();
            OSMDocument.Load(OSMFilePath);

            Dictionary<string, Point> fileNodeList = new Dictionary<string, Point>();
            XmlNodeList nodeXMLNodeList = OSMDocument.GetElementsByTagName("node");
            XmlNodeList wayXMLNodeList = OSMDocument.GetElementsByTagName("way");

            for (int i = 0; i < nodeXMLNodeList.Count; i++)
            {
                XmlNode oneNodeXML = nodeXMLNodeList.Item(i);
                XmlElement oneNodeElement = oneNodeXML as XmlElement;
                string nodeID = oneNodeElement.GetAttribute("id");
                string nodeLon = oneNodeElement.GetAttribute("lon");
                string nodeLat = oneNodeElement.GetAttribute("lat");
                fileNodeList.Add(nodeID, new Point(Convert.ToDouble(nodeLon), Convert.ToDouble(nodeLat)));
            }

            IShapefile wayShapefile = VectorFactory.CreateShapefile(ESRIShapeType.Polyline);
            wayShapefile.AddField("OSMID", DBFFieldType.Char, 20, 0);
            wayShapefile.AddField("subtype", DBFFieldType.Char, 20, 0);

            for (int i = 0; i < wayXMLNodeList.Count; i++)
            {
                XmlNode oneWayXML = wayXMLNodeList.Item(i);
                XmlElement oneWayElement = oneWayXML as XmlElement;
                string wayID = oneWayElement.GetAttribute("id");

                ShpPolyline polyline = new ShpPolyline();
                polyline.PartList.Add(0);
                Dictionary<string, object> properties = new Dictionary<string, object>();
                properties.Add("OSMID", wayID);

                XmlNodeList childNodeList = oneWayXML.ChildNodes;
                bool needToAdded = false;
                for (int j = 0; j < childNodeList.Count; j++)
                {
                    XmlNode nodeRefNode = childNodeList.Item(j);
                    if (nodeRefNode.Name == "nd")
                    {
                        XmlElement oneNodeRefElement = nodeRefNode as XmlElement;
                        string nodeRefID = oneNodeRefElement.GetAttribute("ref");
                        if (fileNodeList.ContainsKey(nodeRefID))
                        {
                            polyline.PointList.Add(fileNodeList[nodeRefID]);
                        }
                    }
                    else if (nodeRefNode.Name == "tag")
                    {
                        XmlElement oneWayTagElement = nodeRefNode as XmlElement;
                        string tagKey = oneWayTagElement.GetAttribute("k");
                        string tagValue = oneWayTagElement.GetAttribute("v");
                        if (tagKey == "highway")
                        {
                            properties.Add("subtype", tagValue);
                            needToAdded = true;
                        }
                    }
                }

                if (needToAdded)
                {
                    wayShapefile.AddFeature(polyline, properties);
                }
            }

            return wayShapefile;
        }
    }
}
