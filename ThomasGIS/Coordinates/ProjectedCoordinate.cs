using ThomasGIS.TextParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ThomasGIS.Coordinates
{
    public enum DirectionType
    {
        North,
        East,
        South,
        West
    }
    public class ProjectedCoordinate : CoordinateBase
    {
        public GeographicCoordinate GeoCoordinate { get; set; } = null;
        public string Projection { get; set; } = "";
        public string Name { get; set; } = "";
        public Dictionary<string, double> Parameters { get; set; } = new Dictionary<string, double>();
        public Unit Unit { get; set; } = null;
        public List<Axis> AxisList { get; set; } = new List<Axis>();
        public Authority Authority { get; set; } = null;
        public ProjectedCoordinate(List<Token> tokenList)
        {
            // 检测标识符
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            // 检测是否为地理坐标系的WKT
            Token baseToken = tokenList[0];
            if (baseToken.Type != TokenType.ObjectName || baseToken.Value != "PROJCS") throw new Exception("错误的WKT字符串，结构错误！");

            tokenList.RemoveAt(0);

            // 循环处理每个Token
            while (tokenList.Count > 0)
            {
                // 每次处理第一个
                Token nowToken = tokenList[0];

                if (nowToken.Type == TokenType.ObjectName)
                {
                    if (nowToken.Value == "PROJECTION")
                    {
                        tokenList.RemoveAt(0);
                        tokenList.RemoveAt(0);
                        nowToken = tokenList[0];
                        this.Projection = nowToken.Value;
                        tokenList.RemoveAt(0);
                        tokenList.RemoveAt(0);
                    }
                    else if (nowToken.Value == "PARAMETER")
                    {
                        tokenList.RemoveAt(0);
                        tokenList.RemoveAt(0);
                        nowToken = tokenList[0];
                        string paramKey = nowToken.Value;
                        tokenList.RemoveAt(0);
                        tokenList.RemoveAt(0);
                        nowToken = tokenList[0];
                        double paramValue = Convert.ToDouble(nowToken.Value);
                        tokenList.RemoveAt(0);
                        Parameters.Add(paramKey.ToLower(), paramValue);
                        tokenList.RemoveAt(0);
                    }
                    else if (nowToken.Value == "UNIT")
                    {
                        this.Unit = new Unit(tokenList);
                    }
                    else if (nowToken.Value == "AXIS")
                    {
                        this.AxisList.Add(new Axis(tokenList));
                    }
                    else if (nowToken.Value == "AUTHORITY")
                    {
                        this.Authority = new Authority(tokenList);
                    }
                    else if (nowToken.Value == "GEOGCS")
                    {
                        this.GeoCoordinate = new GeographicCoordinate(tokenList);
                    }
                    else
                    {
                        throw new Exception("WKT字符串中存在无法识别的关键字！");
                    }
                }
                else if (nowToken.Type == TokenType.Comma || nowToken.Type == TokenType.ObjectBegin)
                {
                    tokenList.RemoveAt(0);
                }
                else if (nowToken.Type == TokenType.ObjectEnd)
                {
                    tokenList.RemoveAt(0);
                    break;
                }
                else if (nowToken.Type == TokenType.String)
                {
                    this.Name = nowToken.Value;
                    tokenList.RemoveAt(0);
                }
                else
                {
                    throw new Exception("错误的WKT字符串，结构错误！");
                }
            }
        }

        public ProjectedCoordinate(GeographicCoordinate geographicCoordinate, string name)
        {
            this.GeoCoordinate = geographicCoordinate;
            this.Name = name;
            this.Unit = new Unit();
        }

        public override string ExportToWkt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PROJCS[");
            sb.Append($"\"{ Name }\",");
            sb.Append(GeoCoordinate.ExportToWkt() + ",");
            sb.Append($"PROJECTION[\"{ Projection }\"],");

            foreach (string key in Parameters.Keys)
            {
                sb.Append($"PARAMETER[\"{ key }\",{Math.Round(Parameters[key], 6)}],");
            }

            sb.Append(Unit.ToString() + ",");
            for (int i = 0; i < AxisList.Count; i++)
            {
                sb.Append(AxisList[i].ToString() + ",");
            }

            if (Authority != null)
            {
                sb.Append(Authority.ToString() + ",");
            }

            string result = sb.ToString();
            return result.Substring(0, result.Length - 1) + "]";
        }
        public override CoordinateType GetCoordinateType()
        {
            return CoordinateType.Projected;
        }
    }
}
