using ThomasGIS.Coordinates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Geometries.OpenGIS;

namespace ThomasGIS.TextParser
{
    public class Token
    {
        public TokenType Type;
        public string Value;
        public Token(TokenType type, string value)
        {
            this.Type = type;
            this.Value = value;
        }
    }

    public enum TokenType
    {
        ObjectName,
        ObjectBegin,
        ObjectEnd,
        Comma,
        String,
        Number,
        Direction
    }

    static class WKTParser
    {
        static private List<Token> TokenCreator(string wkt)
        {
            // Prj文件的wkt词法分析器，可通用
            List<Token> tokenList = new List<Token>();
            int i = 0;
            while (i < wkt.Length)
            {
                char nowCh = wkt[i];
                if ((nowCh >= 'A' && nowCh <= 'Z') || (nowCh >= 'a' && nowCh <= 'z'))
                {
                    StringBuilder sb = new StringBuilder();
                    while (i + 1 <= wkt.Length && nowCh != '[')
                    {
                        sb.Append(nowCh);
                        if (i == wkt.Length) break;
                        nowCh = wkt[++i];
                    }

                    if (sb.ToString() == "WEST" || sb.ToString() == "NORTH" || sb.ToString() == "EAST" || sb.ToString() == "SOUTH")
                    {
                        tokenList.Add(new Token(TokenType.Direction, sb.ToString().ToUpper()));
                    }
                    else
                    {
                        tokenList.Add(new Token(TokenType.ObjectName, sb.ToString().ToUpper()));
                    }

                }
                else if (nowCh == '[')
                {
                    tokenList.Add(new Token(TokenType.ObjectBegin, "["));
                    i++;
                }
                else if (nowCh == ']')
                {
                    tokenList.Add(new Token(TokenType.ObjectEnd, "]"));
                    i++;
                }
                else if (nowCh == '"')
                {
                    nowCh = wkt[++i];
                    StringBuilder sb = new StringBuilder();
                    while (i + 1 <= wkt.Length && nowCh != '"')
                    {
                        sb.Append(nowCh);
                        if (i == wkt.Length) break;
                        nowCh = wkt[++i];
                    }
                    tokenList.Add(new Token(TokenType.String, sb.ToString()));
                    // 跳过后引号
                    i++;
                }
                else if (nowCh == ' ')
                {
                    i++;
                }
                else if ((nowCh >= '0' && nowCh <= '9') || nowCh == '-')
                {
                    StringBuilder sb = new StringBuilder();
                    while (i + 1 <= wkt.Length && (nowCh >= '0' && nowCh <= '9') || nowCh == '.' || nowCh == '-')
                    {
                        sb.Append(nowCh);
                        if (i == wkt.Length) break;
                        nowCh = wkt[++i];
                    }
                    tokenList.Add(new Token(TokenType.Number, sb.ToString()));
                }
                else if (nowCh == ',')
                {
                    tokenList.Add(new Token(TokenType.Comma, ","));
                    i++;
                }
                else
                {
                    break;
                }
            }

            return tokenList;
        }

        public static CoordinateBase ParsePrjWKT(string wkt)
        {
            List<Token> tokenList = TokenCreator(wkt);

            Token baseToken = tokenList[0];

            if (baseToken.Value.ToLower().Contains("geogcs"))
            {
                baseToken.Value = "GEOGCS";
                return new GeographicCoordinate(tokenList);
            }
            else if (baseToken.Value.ToLower().Contains("projcs"))
            {
                baseToken.Value = "PROJCS";
                return new ProjectedCoordinate(tokenList);
            }
            else
            {
                throw new Exception("WKT Error 002: 当前格式WKT无法解析，请等待新版本支持！");
            }
        }

        public static IGeometry ParseWKT2Shapefile(string wkt)
        {
            string[] typeItems = wkt.Split('(');
            if (typeItems.Length == 1)
            {
                throw new Exception("WKT Error 001: 错误的WKT字符串，缺少首个类型标签！");
            }

            string wktType = typeItems[0].Trim(' ').ToLower();

            switch (wktType)
            {
                case "point":
                    return new ShpPoint(wkt);
                case "linestring":
                case "multilinestring":
                    return new ShpPolyline(wkt);
                case "polygon":
                case "multipolygon":
                    return new ShpPolygon(wkt);
                default:
                    throw new Exception("WKT Error 003: 错误的WKT字符串，目前不支持或错误的类型标签！");
            }
        }

        public static IGeometry ParseWKT2OpenGIS(string wkt, Dictionary<string, string> properties = null)
        {
            string[] typeItems = wkt.Split('(');
            if (typeItems.Length == 1)
            {
                throw new Exception("WKT Error 001: 错误的WKT字符串，缺少首个类型标签！");
            }

            string wktType = typeItems[0].Trim(' ').ToLower();

            switch (wktType)
            {
                case "point":
                    return new OpenGIS_Point(wkt, properties);
                case "multipoint":
                    return new OpenGIS_MultiPoint(wkt, properties);
                case "linestring":
                    return new OpenGIS_LineString(wkt, properties);
                case "multilinestring":
                    return new OpenGIS_MultiLineString(wkt, properties);
                case "polygon":
                    return new OpenGIS_Polygon(wkt, properties);
                case "multipolygon":
                    return new OpenGIS_MultiPolygon(wkt, properties);
                default:
                    throw new Exception("WKT Error 003: 错误的WKT字符串，目前不支持或错误的类型标签！");
            }
        }
    }
}
