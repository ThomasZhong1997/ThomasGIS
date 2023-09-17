using ThomasGIS.TextParser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Grids.GeoTiff;

namespace ThomasGIS.Coordinates
{
    public class CoordinateGenerator
    {
        private static Dictionary<int, string> CRSWktDictionary = null;
        private static Dictionary<int, string> EllipsoidDictionary = null;
        private static Dictionary<int, string> PrimeMeridianDictionary = null;
        private static Dictionary<int, string> UnitOfMeasureDictionary = null;
        private static Dictionary<int, string> DatumDictionary = null;

        static CoordinateGenerator()
        {
            CRSWktDictionary = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(new FileStream("./CRS.csv", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('|');
                    CRSWktDictionary.Add(Convert.ToInt32(items[0]), items[1].TrimEnd('\n'));
                }

            }

            EllipsoidDictionary = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(new FileStream("./Ellipsoid.csv", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('|');
                    EllipsoidDictionary.Add(Convert.ToInt32(items[0]), items[1].TrimEnd('\n'));
                }
            }

            PrimeMeridianDictionary = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(new FileStream("./PrimeMeridian.csv", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('|');
                    PrimeMeridianDictionary.Add(Convert.ToInt32(items[0]), items[1].TrimEnd('\n'));
                }
            }

            UnitOfMeasureDictionary = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(new FileStream("./UnitOfMeasure.csv", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('|');
                    UnitOfMeasureDictionary.Add(Convert.ToInt32(items[0]), items[1].TrimEnd('\n'));
                }
            }

            DatumDictionary = new Dictionary<int, string>();
            using (StreamReader sr = new StreamReader(new FileStream("./Datum.csv", FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] items = line.Split('|');
                    DatumDictionary.Add(Convert.ToInt32(items[0]), items[1].TrimEnd('\n'));
                }
            }
        }

        // 从现有wkt字符串中解析坐标系对象
        public static CoordinateBase ParseFromWkt(string wkt)
        {
            return WKTParser.ParsePrjWKT(wkt);
        }

        // 从现有.prj文件中解析坐标系对象
        public static CoordinateBase ParseFromFile(string filepath)
        {
            string wkt = "";
            using (StreamReader sr = new StreamReader(new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.Read)))
            {
                StringBuilder sb = new StringBuilder();
                while (!sr.EndOfStream)
                {
                    sb.Append(sr.ReadLine().ToString().TrimEnd('\n').TrimEnd('\r'));
                }
                wkt = sb.ToString();
            }
            return WKTParser.ParsePrjWKT(wkt);
        }

        // 从依据SRID生成标准的坐标系对象
        public static CoordinateBase CRSParseFromEPSG(int SRID)
        {
            if (CRSWktDictionary.ContainsKey(SRID))
            {
                CoordinateBase coordinateBase = ParseFromWkt(CRSWktDictionary[SRID]);
                if (coordinateBase.GetCoordinateType() == CoordinateType.Geographic)
                {
                    GeographicCoordinate geographicCoordinate = coordinateBase as GeographicCoordinate;
                    geographicCoordinate.Authority = CreateAuthority(SRID);
                    return geographicCoordinate;
                }
                else if (coordinateBase.GetCoordinateType() == CoordinateType.Projected)
                {
                    ProjectedCoordinate projectedCoordinate = coordinateBase as ProjectedCoordinate;
                    projectedCoordinate.Authority = CreateAuthority(SRID);
                    return projectedCoordinate;
                }
                else
                {
                    throw new Exception("目前无法支持的SRID");
                }
            }

            throw new Exception("目前无法支持的SRID");
        }

        public static Unit UnitParseFromEPSG(int SRID)
        {
            if (UnitOfMeasureDictionary.ContainsKey(SRID))
            {
                string unitInfo = UnitOfMeasureDictionary[SRID];
                string[] items = unitInfo.Split(',');
                Unit newUnit = new Unit();
                newUnit.Name = items[1];
                newUnit.Value = Convert.ToDouble(items[2]) / Convert.ToDouble(items[3]);
                newUnit.Authority = CreateAuthority(SRID);
                return newUnit;
            }

            throw new Exception("目前无法支持的SRID");
        }

        private static Authority CreateAuthority(int SRID)
        {
            Authority newAuthority = new Authority();
            newAuthority.Type = "EPSG";
            newAuthority.SRID = SRID.ToString();
            return newAuthority;
        }

        public static PrimeMeridian PrimeMeridianParseFromEPSG(int SRID)
        {
            if (PrimeMeridianDictionary.ContainsKey(SRID))
            {
                string primeMeridianInfo = PrimeMeridianDictionary[SRID];
                string[] items = primeMeridianInfo.Split(',');
                PrimeMeridian newPrimeMeridian = new PrimeMeridian();
                newPrimeMeridian.Name = items[0];
                newPrimeMeridian.Longitude = Convert.ToDouble(items[1]);
                newPrimeMeridian.Authority = CreateAuthority(SRID);
                return newPrimeMeridian;
            }

            throw new Exception("目前无法支持的SRID");
        }

        public static Spheroid EllipsoidParseFromEPSG(int SRID)
        {
            if (EllipsoidDictionary.ContainsKey(SRID))
            {
                string ellipsoidInfo = EllipsoidDictionary[SRID];
                string[] items = ellipsoidInfo.Split(',');
                Spheroid newSpheroid = new Spheroid();
                newSpheroid.Name = items[0];
                newSpheroid.SemiMajorAxis = Convert.ToDouble(items[1]);
                newSpheroid.InverseFlattening = Convert.ToDouble(items[2]);
                newSpheroid.Authority = CreateAuthority(SRID);
                return newSpheroid;
            }

            throw new Exception("目前无法支持的SRID");
        }

        public static Datum DatumParseFromEPSG(int SRID)
        {
            if (DatumDictionary.ContainsKey(SRID))
            {
                string datumInfo = DatumDictionary[SRID];
                string[] items = datumInfo.Split(',');
                Datum newDatum = new Datum();
                newDatum.Name = items[0];
                int ellipsoidCode = Convert.ToInt32(items[2]);
                newDatum.Spheroid = EllipsoidParseFromEPSG(ellipsoidCode);
                newDatum.Authority = CreateAuthority(SRID);
                return newDatum;
            }

            throw new Exception("目前无法支持的SRID");
        }

        // 由GeoKeys生成标准的坐标系对象
        public static CoordinateBase ParseFormTiffGeoDesc(GeoKeysDesc geoKeysDesc)
        {
            int coordinateType = geoKeysDesc.GTModelTypeGeoKey;

            switch (coordinateType)
            {
                case 0:
                    return null;
                // key = 1024, value = 1 --> PROJCS
                case 1:
                    return ParseProjCoordinate(geoKeysDesc);
                // key = 1024, value = 2 --> GEOGCS
                case 2:
                    return ParseGeogCoordinate(geoKeysDesc);
                case 3:
                    // int verticalGeoKey = BitConverter.ToUInt16(geoKeysDesc.geoKeyValue[4096], 0);
                default:
                    throw new Exception("目前仅支持标准地理坐标系与投影坐标系的GTIFF解析！");
            }
        }

        private static ProjectedCoordinate ParseProjCoordinate(GeoKeysDesc geoKeysDesc)
        {
            int projectedCRSGeoKey = geoKeysDesc.ProjectedCRSGeoKey;
            if (projectedCRSGeoKey != 32767 && projectedCRSGeoKey != -1)
            {
                return CRSParseFromEPSG(projectedCRSGeoKey) as ProjectedCoordinate;
            }
            else
            {
                string citations = geoKeysDesc.ProjectedCitationGeoKey;
                return ParseFromWkt(citations) as ProjectedCoordinate;
            }
        }

        private static GeographicCoordinate ParseGeogCoordinate(GeoKeysDesc geoKeysDesc)
        {
            int geodeticCRSGeoKey = geoKeysDesc.GeogGeodeticCRSGeoKey;
            if (geodeticCRSGeoKey != 32767)
            {
                return CRSParseFromEPSG(geodeticCRSGeoKey) as GeographicCoordinate;
            }
            else
            {
                GeographicCoordinate newGeogCoordinate = new GeographicCoordinate();
                string citations = geoKeysDesc.GeogCitationGeoKey;
                Dictionary<string, string> citationDict = new Dictionary<string, string>();

                string[] citationItems = citations.Split('|');
                foreach (string citation in citationItems)
                {
                    if (citation.Contains('='))
                    {
                        string[] oneItemContent = citation.Split('=');
                        string citationKey = oneItemContent[0].Trim(' ');
                        string citationValue = oneItemContent[1].Trim(' ');
                        citationDict.Add(citationKey, citationValue);
                    }
                }

                newGeogCoordinate.Name = citationDict["GCS Name"];

                if (geoKeysDesc.GeogAngularUnitsGeoKey == -1 && geoKeysDesc.GeogLinearUnitsGeoKey == -1)
                {
                    throw new Exception("GIFF文件的Geokey损坏");
                }

                // 单位
                if (geoKeysDesc.GeogAngularUnitsGeoKey != -1)
                {
                    if (geoKeysDesc.GeogAngularUnitsGeoKey != 32767)
                    {
                        newGeogCoordinate.Unit = UnitParseFromEPSG(geoKeysDesc.GeogAngularUnitsGeoKey);
                    }
                    else
                    {
                        double unitValue = geoKeysDesc.GeogAngularUnitSizeGeoKey;
                        newGeogCoordinate.Unit = new Unit();
                        newGeogCoordinate.Unit.Name = "Degree";
                        newGeogCoordinate.Unit.Value = unitValue;
                    }
                }

                if (geoKeysDesc.GeogAzimuthUnitGeoKey != -1)
                {
                    if (geoKeysDesc.GeogAzimuthUnitGeoKey != 32767)
                    {
                        newGeogCoordinate.Unit = UnitParseFromEPSG(geoKeysDesc.GeogAzimuthUnitGeoKey);
                    }
                    else
                    {
                        double unitValue = geoKeysDesc.GeogAngularUnitSizeGeoKey;
                        newGeogCoordinate.Unit = new Unit();
                        newGeogCoordinate.Unit.Name = "Degree";
                        newGeogCoordinate.Unit.Value = unitValue;
                    }
                }

                if (geoKeysDesc.GeogLinearUnitsGeoKey != -1)
                {
                    if (geoKeysDesc.GeogLinearUnitsGeoKey != 32767)
                    {
                        newGeogCoordinate.Unit = UnitParseFromEPSG(geoKeysDesc.GeogLinearUnitsGeoKey);
                    }
                    else
                    {
                        double unitValue = geoKeysDesc.GeogLinearUnitSizeGeoKey;
                        newGeogCoordinate.Unit = new Unit();
                        newGeogCoordinate.Unit.Name = "Metre";
                        newGeogCoordinate.Unit.Value = unitValue;
                    }
                }


                if (geoKeysDesc.GeogGeodeticDatumGeoKey != 32767 && geoKeysDesc.GeogGeodeticDatumGeoKey != -1)
                {
                    newGeogCoordinate.Datum = DatumParseFromEPSG(geoKeysDesc.GeogGeodeticDatumGeoKey);
                }
                else
                {
                    Datum newDatum = new Datum();
                    newDatum.Name = citationDict["Datum"];
                    if (geoKeysDesc.GeogPrimeMeridianGeoKey != 32767 && geoKeysDesc.GeogPrimeMeridianGeoKey != -1)
                    {
                        newGeogCoordinate.Primem = PrimeMeridianParseFromEPSG(geoKeysDesc.GeogPrimeMeridianGeoKey);
                    }
                    else
                    {
                        PrimeMeridian newPrimeMeridian = new PrimeMeridian();
                        newPrimeMeridian.Name = citationDict["Primem"];
                        newPrimeMeridian.Longitude = geoKeysDesc.GeogPrimeMeridianLongitudeGeoKey;
                        newGeogCoordinate.Primem = newPrimeMeridian;
                    }

                    if (geoKeysDesc.GeogEllipsoidGeoKey != 32767 && geoKeysDesc.GeogEllipsoidGeoKey != -1)
                    {
                        newDatum.Spheroid = EllipsoidParseFromEPSG(geoKeysDesc.GeogEllipsoidGeoKey);
                    }
                    else
                    {
                        Spheroid newSpheroid = new Spheroid();
                        newSpheroid.Name = citationDict["Ellipsoid"];
                        newSpheroid.SemiMajorAxis = geoKeysDesc.EllipsoidSemiMajorAxisGeoKey;
                        if (geoKeysDesc.EllipsoidInvFlatteningGeoKey == -1)
                        {
                            newSpheroid.InverseFlattening = geoKeysDesc.EllipsoidSemiMajorAxisGeoKey / (geoKeysDesc.EllipsoidSemiMajorAxisGeoKey - geoKeysDesc.EllipsoidSemiMinorAxisGeoKey);
                        }
                        else
                        {
                            newSpheroid.InverseFlattening = geoKeysDesc.EllipsoidInvFlatteningGeoKey;
                        }
                        newDatum.Spheroid = newSpheroid;
                    }

                    newGeogCoordinate.Datum = newDatum;
                }

                return newGeogCoordinate;
            }
        }
    }
}