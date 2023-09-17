using System;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ThomasGIS.TextParser;

namespace ThomasGIS.Coordinates
{
    public class GeographicCoordinate : CoordinateBase
    {
        public string Name { get; set; } = null;
        public Datum Datum { get; set; } = null;
        public PrimeMeridian Primem { get; set; } = null;
        public Unit Unit { get; set; } = null;

        public List<Axis> AxisList = new List<Axis>();

        public Authority Authority { get; set; } = null;

        public GeographicCoordinate()
        {
            this.Datum = new Datum();
            this.Primem = new PrimeMeridian();
            this.Unit = new Unit();
            this.Unit.Name = "Degree";
            this.Unit.Value = 0.0000001;
        }

        public GeographicCoordinate(List<Token> tokenList)
        {
            // 检测标识符
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            // 检测是否为地理坐标系的WKT
            Token baseToken = tokenList[0];
            if (baseToken.Type != TokenType.ObjectName || baseToken.Value != "GEOGCS") throw new Exception("错误的WKT字符串，结构错误！");

            tokenList.RemoveAt(0);

            // 循环处理每个Token
            while (tokenList.Count > 0)
            {
                // 每次处理第一个
                Token nowToken = tokenList[0];

                if (nowToken.Type == TokenType.ObjectName)
                {
                    if (nowToken.Value == "DATUM")
                    {
                        this.Datum = new Datum(tokenList);
                    }
                    else if (nowToken.Value == "PRIMEM")
                    {
                        this.Primem = new PrimeMeridian(tokenList);
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

        public override string ExportToWkt()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("GEOGCS[");
            sb.Append($"\"{ Name }\",");
            sb.Append(Datum.ToString() + ",");
            sb.Append(Primem.ToString() + ",");
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
            return CoordinateType.Geographic;
        }
    }
}
