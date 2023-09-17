using ThomasGIS.TextParser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Coordinates
{
    public class Datum
    {
        public string Name { get; set; } = null;
        public Spheroid Spheroid { get; set; } = null;
        public Authority Authority { get; set; } = null;
        public ToWGS84 ToWGS84 { get; set; } = null;

        public Datum()
        {
            this.Spheroid = new Spheroid();
        }

        public Datum(List<Token> tokenList) 
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "DATUM") throw new Exception("错误的WKT字符串，结构错误！");

            while (tokenList.Count > 0)
            {
                Token nowToken = tokenList[0];
                if (nowToken.Type == TokenType.ObjectName)
                {
                    if (nowToken.Value == "SPHEROID")
                    {
                        this.Spheroid = new Spheroid(tokenList);
                    }
                    else if (nowToken.Value == "TOWGS84")
                    {
                        this.ToWGS84 = new ToWGS84(tokenList);
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
                else if (nowToken.Type == TokenType.String)
                {
                    this.Name = nowToken.Value;
                    tokenList.RemoveAt(0);
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
                else
                {
                    throw new Exception("错误的WKT字符串，结构错误！");
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("DATUM[");
            sb.Append($"\"{ Name }\",");
            if (Spheroid != null)
            {
                sb.Append(Spheroid.ToString() + ",");
            }

            if (ToWGS84 != null)
            {
                sb.Append(ToWGS84.ToString() + ",");
            }

            if (Authority != null)
            {
                sb.Append(Authority.ToString() + ",");
            }

            string result = sb.ToString();

            return result.Substring(0, result.Length - 1) + "]";
        }
    }

    public class ToWGS84 
    {
        public int v1 = 0, v2 = 0, v3 = 0, v4 = 0, v5 = 0, v6 = 0, v7 = 0;

        public ToWGS84()
        {
            
        }

        public ToWGS84(List<Token> tokenList)
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "TOWGS84") throw new Exception("错误的WKT字符串，结构错误！");

            // 写死结构即可，内部的东西固定了
            // 移除[
            tokenList.RemoveAt(0);
            // 第一个数
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v1 = Convert.ToInt32(nowToken.Value);
            // 第二个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v2 = Convert.ToInt32(nowToken.Value);
            // 第三个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v3 = Convert.ToInt32(nowToken.Value);
            // 第四个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v4 = Convert.ToInt32(nowToken.Value);
            // 第五个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v5 = Convert.ToInt32(nowToken.Value);
            // 第六个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v6 = Convert.ToInt32(nowToken.Value);
            // 第七个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            v7 = Convert.ToInt32(nowToken.Value);
            // 顺便]也被扔掉了，所以完事了！
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("TOWGS84[");
            sb.Append(v1);
            sb.Append(",");
            sb.Append(v2);
            sb.Append(",");
            sb.Append(v3);
            sb.Append(",");
            sb.Append(v4);
            sb.Append(",");
            sb.Append(v5);
            sb.Append(",");
            sb.Append(v6);
            sb.Append(",");
            sb.Append(v7);
            sb.Append("]");
            return sb.ToString();
        }
    }

    public class Unit
    {
        public string Name { get; set; } = "Meter";
        public double Value { get; set; } = 1.0;
        public Authority Authority { get; set; } = null;
        public Unit()
        {
            
        }

        public Unit(List<Token> tokenList)
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "UNIT") throw new Exception("错误的WKT字符串，结构错误！");

            while (tokenList.Count > 0)
            {
                Token nowToken = tokenList[0];
                if (nowToken.Type == TokenType.ObjectName)
                {
                    if (nowToken.Value == "AUTHORITY")
                    {
                        this.Authority = new Authority(tokenList);
                    }
                    else
                    {
                        throw new Exception("WKT字符串中存在无法识别的关键字！");
                    }
                }
                else if (nowToken.Type == TokenType.String)
                {
                    this.Name = nowToken.Value;
                    tokenList.RemoveAt(0);
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
                else if (nowToken.Type == TokenType.Number)
                {
                    this.Value = Convert.ToDouble(nowToken.Value);
                    tokenList.RemoveAt(0);
                }
                else
                {
                    throw new Exception("错误的WKT字符串，结构错误！");
                }
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("UNIT[");
            sb.Append($"\"{ Name }\",");
            sb.Append(Value);
            sb.Append(",");
            if (Authority != null) 
            {
                sb.Append(Authority.ToString() + ",");
            }
            string result = sb.ToString();
            return result.Substring(0, result.Length - 1) + "]";
        }
    }

    public class PrimeMeridian
    {
        public string Name { get; set; } = "";
        public double Longitude { get; set; } = 0;
        public Authority Authority { get; set; } = null;
        public PrimeMeridian(List<Token> tokenList)
        {
            {
                if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

                Token baseToken = tokenList[0];
                tokenList.RemoveAt(0);

                if (baseToken.Value != "PRIMEM") throw new Exception("错误的WKT字符串，结构错误！");

                while (tokenList.Count > 0)
                {
                    Token nowToken = tokenList[0];
                    if (nowToken.Type == TokenType.ObjectName)
                    {
                        if (nowToken.Value == "AUTHORITY")
                        {
                            this.Authority = new Authority(tokenList);
                        }
                        else
                        {
                            throw new Exception("WKT字符串中存在无法识别的关键字！");
                        }
                    }
                    else if (nowToken.Type == TokenType.String)
                    {
                        this.Name = nowToken.Value;
                        tokenList.RemoveAt(0);
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
                    else if (nowToken.Type == TokenType.Number)
                    {
                        this.Longitude = Convert.ToDouble(nowToken.Value);
                        tokenList.RemoveAt(0);
                    }
                    else
                    {
                        throw new Exception("错误的WKT字符串，结构错误！");
                    }
                }
            }
        }
        public PrimeMeridian() 
        {

        }
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("PRIMEM[");
            sb.Append($"\"{ Name }\",");
            sb.Append(Longitude);
            sb.Append(",");
            if (Authority != null)
            {
                sb.Append(Authority.ToString() + ",");
            }
            string result = sb.ToString();
            return result.Substring(0, result.Length - 1) + "]";
        }
    }

    public class Spheroid
    {
        public string Name { get; set; }
        public double SemiMajorAxis { get; set; }
        public double InverseFlattening { get; set; }
        public Authority Authority { get; set; } = null;
        public Spheroid()
        {
            this.Name = "";
            this.SemiMajorAxis = 6378137.0;
            this.InverseFlattening = 298.257223563;
        }
        public Spheroid(List<Token> tokenList)
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "SPHEROID") throw new Exception("错误的WKT字符串，结构错误！");

            // 写死结构即可，内部的东西固定了
            // 移除[
            tokenList.RemoveAt(0);
            // 第一个是Name
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.Name = nowToken.Value;
            // 第二个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.SemiMajorAxis = Convert.ToDouble(nowToken.Value);
            // 第三个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.InverseFlattening = Convert.ToDouble(nowToken.Value);

            nowToken = tokenList[0];
            if (nowToken.Value == "AUTHORITY")
            {
                this.Authority = new Authority(tokenList);
                tokenList.RemoveAt(0);
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("SPHEROID[");
            sb.Append($"\"{ Name }\",");
            sb.Append(SemiMajorAxis);
            sb.Append(",");
            sb.Append(InverseFlattening);
            sb.Append(",");
            if (Authority != null)
            {
                sb.Append(Authority.ToString());
            }
            string result = sb.ToString();
            return result.Substring(0, result.Length - 1) + "]";
        }
    }

    public class Authority
    {
        public string Type { get; set; } = "EPSG";
        public string SRID { get; set; } = "4326";
        public Authority()
        {
            
        }
        public Authority(List<Token> tokenList)
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "AUTHORITY") throw new Exception("错误的WKT字符串，结构错误！");

            // 写死结构即可，内部的东西固定了
            // 移除[
            tokenList.RemoveAt(0);
            // 第一个是Name
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.Type = nowToken.Value;
            // 第二个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.SRID = nowToken.Value;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AUTHORITY[");
            sb.Append($"\"{ Type }\",");
            sb.Append($"\"{ SRID }\"]");
            return sb.ToString();
        }
    }

    public class Extension
    {
        public string Name { get; set; }
        public string Content { get; set; }
        public Authority authority { get; set; }
        public Extension(List<Token> tokenList)
        {
            
        }
    }

    public class Axis
    {
        public string Name = "";
        public string Direction = "";
        public Axis(List<Token> tokenList)
        {
            if (tokenList.Count < 1) throw new Exception("错误的WKT字符串，数据不完整！");

            Token baseToken = tokenList[0];
            tokenList.RemoveAt(0);

            if (baseToken.Value != "AXIS") throw new Exception("错误的WKT字符串，结构错误！");

            // 写死结构即可，内部的东西固定了
            // 移除[
            tokenList.RemoveAt(0);
            // 第一个是Name
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.Name = nowToken.Value;
            // 第二个数
            nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenList.RemoveAt(0);
            this.Direction = nowToken.Value;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("AXIS[");
            sb.Append($"\"{ Name }\",");
            sb.Append($"{ Direction }]");
            return sb.ToString();
        } 
    }
}
