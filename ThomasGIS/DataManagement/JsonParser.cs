using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.DataManagement
{
    public enum TokenType 
    {
        BeginObject = 1,
        EndObject = 2,
        BeginArray = 4,
        EndArray = 8,
        Null = 16,
        Number = 32,
        String = 64,
        Boolean = 128,
        SepColon = 256,
        SepComma = 512,
        EndDocument = 1024
    }

    public class Token 
    {
        private TokenType TokenType;
        private string Value;

        public Token(TokenType type, string value)
        {
            TokenType = type;
            Value = value;
        }

        public TokenType GetTokenType() 
        {
            return TokenType;
        }

        public string GetValue()
        {
            return Value;
        }
    }

    public class CharReader
    {
        private string _Data { get; set; }
        private int _Position { get; set; } = 0;

        private int Size => _Data.Length;

        public CharReader(string jsonString)
        {
            _Data = jsonString;
            _Position = 0;
        }

        public char Peek()
        {
            if (_Position >= Size)
            {
                return char.MinValue;
            }

            return _Data[Math.Max(0, _Position)];
        }

        public char Next()
        {
            if (HasMore())
            {
                char value = _Data[_Position];
                _Position += 1;
                return value;
            }
            return char.MinValue;
        }

        public void Back()
        {
            _Position = Math.Max(0, --_Position);
        }

        public bool HasMore()
        {
            if (_Position < Size)
            {
                return true;
            }

            return false;
        }

        public bool IsNull(char nowChar)
        {
            StringBuilder nullBuilder = new StringBuilder();
            nullBuilder.Append(nowChar);
            for (int i = 0; i < 3; i++)
            {
                nullBuilder.Append(Next());
            }
            return nullBuilder.ToString() == "null" ? true : false;
        }

        public bool IsTrue(char nowChar)
        {
            StringBuilder trueBuilder = new StringBuilder();
            trueBuilder.Append(nowChar);
            for (int i = 0; i < 3; i++)
            {
                trueBuilder.Append(Next());
            }
            return trueBuilder.ToString().ToLower() == "true" ? true : false;
        }

        public bool IsFalse(char nowChar)
        {
            StringBuilder trueBuilder = new StringBuilder();
            trueBuilder.Append(nowChar);
            for (int i = 0; i < 4; i++)
            {
                trueBuilder.Append(Next());
            }
            return trueBuilder.ToString().ToLower() == "false" ? true : false;
        }

        public string ReadString(char nowChar)
        {
            StringBuilder stringBuilder = new StringBuilder();
            if (nowChar == '"')
            {
                // stringBuilder.Append(nowChar);
                do
                {
                    nowChar = Next();
                    // 转义字符的判断
                    if (nowChar == '\\')
                    {
                        nowChar = Next();
                        if (nowChar == '"' || nowChar == '\\' || nowChar == '/' || nowChar == 'b' || nowChar == 'f' || nowChar == 'n' || nowChar == 'c' || nowChar == 't' || nowChar == 'r' || nowChar == 'u')
                        {
                            // /u转义后后续读取Unicode编码
                            if (nowChar == 'u')
                            {
                                stringBuilder.Append("\\");
                                stringBuilder.Append(nowChar);
                                for (int i = 0; i < 4; i++)
                                {
                                    nowChar = Next();
                                    if (IsHex(nowChar))
                                    {
                                        stringBuilder.Append(nowChar);
                                    }
                                    else
                                    {
                                        throw new Exception($"JSON字符串中存在错误的Unicode编码，位于{_Position}处");
                                    }
                                }
                            }
                            else
                            {
                                stringBuilder.Append("\\");
                                stringBuilder.Append(nowChar);
                            }
                        }
                        else
                        {
                            throw new Exception($"JSON字符串中存在错误的转义字符，位于{_Position}处");
                        }
                    }
                    else if (nowChar == '\r' || nowChar == '\n')
                    {
                        throw new Exception($"JSON字符串中存在错误的换行标记，位于{_Position}处");
                    }
                    else if (nowChar == '"')
                    {
                        break;
                    }
                    else
                    {
                        stringBuilder.Append(nowChar);
                    }
                } while (nowChar != '"');

            }
            else
            {
                throw new Exception($"错误的JSON字符串，位于{_Position}处");
            }
            if (nowChar != '"') throw new Exception($"错误的JSON字符串，位于{_Position}处");
            return stringBuilder.ToString();
        }

        private bool IsNumberMember(char nowChar)
        {
            return (nowChar >= 48 && nowChar <= 57) ? true : false;
        }

        private bool IsNumberBegin(char nowChar)
        {
            return (nowChar >= 48 && nowChar <= 57) || nowChar == '-' ? true : false;
        }

        public bool IsHex(char nowChar)
        {
            return (nowChar >= 48 && nowChar <= 57) || (nowChar >= 65 && nowChar <= 70) || (nowChar >= 97 && nowChar <= 102) ? true : false;
        }

        public void ReadFrac(StringBuilder numberBuilder)
        {
            // 读一组0-9的整数然后再尝试读EXP
            char nowChar = Next();
            if (IsNumberMember(nowChar))
            {
                do
                {
                    numberBuilder.Append(nowChar);
                    nowChar = Next();
                } while (IsNumberMember(nowChar));

                if (nowChar == 'E' || nowChar == 'e')
                {
                    numberBuilder.Append(nowChar);
                    ReadExp(numberBuilder);
                }
                else
                {
                    Back();
                }
            }
            else
            {
                throw new Exception($"JSON字符串中包含异常的小数表达，位于{_Position}处");
            }
        }

        public void ReadExp(StringBuilder numberBuilder)
        {
            // 读一组可为-的整数
            char nowChar = Next();
            if (nowChar == '-')
            {
                numberBuilder.Append(nowChar);
                nowChar = Next();
                if (IsNumberMember(nowChar))
                {
                    while (IsNumberMember(nowChar))
                    {
                        numberBuilder.Append(nowChar);
                        nowChar = Next();
                    }
                    Back();
                }
                else
                {
                    throw new Exception($"JSON字符串中包含异常的科学计数法表达，位于{_Position}处");
                }

            }
            else if (IsNumberMember(nowChar))
            {
                while (IsNumberMember(nowChar))
                {
                    numberBuilder.Append(nowChar);
                    nowChar = Next();
                }
                Back();
            }
            else
            {
                throw new Exception($"JSON字符串中包含异常的科学计数法表达，位于{_Position}处");
            }
        }

        public void ReadAppendixNumber(StringBuilder numberBuilder)
        {
            char nowChar = Next();
            if (nowChar == '.')
            {
                numberBuilder.Append(nowChar);
                ReadFrac(numberBuilder);
                nowChar = Next();
                if (nowChar == 'E' || nowChar == 'e')
                {
                    numberBuilder.Append(nowChar);
                    ReadExp(numberBuilder);
                }
                else    
                {
                    Back();
                }
            }
            else if (nowChar == 'e' || nowChar == 'E')
            {
                numberBuilder.Append(nowChar);
                ReadExp(numberBuilder);
            }
            else
            {
                Back();
            }
        }

        public string ReadNumber(char nowChar)
        {
            StringBuilder numberBuilder = new StringBuilder();
            if (IsNumberBegin(nowChar))
            {
                if (nowChar == '-')
                {
                    numberBuilder.Append(nowChar);
                    nowChar = Next();
                    if (nowChar == '0')
                    {
                        numberBuilder.Append(nowChar);
                        ReadAppendixNumber(numberBuilder);
                    }
                    else if (IsNumberMember(nowChar))
                    {
                        do
                        {
                            numberBuilder.Append(nowChar);
                            nowChar = Next();
                        } while (IsNumberMember(nowChar));
                        Back();
                        ReadAppendixNumber(numberBuilder);
                    }
                    else
                    {
                        throw new Exception($"JSON字符串中包含异常的\"-\"号，位于{_Position}处");
                    }
                }
                else if (nowChar == '0')
                {
                    numberBuilder.Append(nowChar);
                    ReadAppendixNumber(numberBuilder);
                }
                else
                {
                    while (IsNumberMember(nowChar) && HasMore())
                    {

                        numberBuilder.Append(nowChar);
                        nowChar = Next();
                    }
                    Back();
                    ReadAppendixNumber(numberBuilder);
                }
            }
            else
            {
                throw new Exception($"错误的JSON字符串，位于{_Position}处");
            }

            string number = numberBuilder.ToString();
            return number;
        }

        public Token Start()
        {
            // 读到一个不为空格的字符
            char nowChar;
            do {
                nowChar = Next();
            } while (nowChar == ' ');

            if (nowChar == 'n' && IsNull(nowChar))
            {
                return new Token(TokenType.Null, null);
            }
            else if (nowChar == '[')
            {
                return new Token(TokenType.BeginArray, "[");
            }
            else if (nowChar == ']')
            {
                return new Token(TokenType.EndArray, "]");
            }
            else if (nowChar == '{')
            {
                return new Token(TokenType.BeginObject, "{");
            }
            else if (nowChar == '}')
            {
                return new Token(TokenType.EndObject, "}");
            }
            else if (nowChar == ',')
            {
                return new Token(TokenType.SepComma, ",");
            }
            else if (nowChar == ':')
            {
                return new Token(TokenType.SepColon, ":");
            }
            else if (nowChar == 't' && IsTrue(nowChar))
            {
                return new Token(TokenType.Boolean, "true");
            }
            else if (nowChar == 'f' && IsFalse(nowChar))
            {
                return new Token(TokenType.Boolean, "false");
            }
            else if (nowChar == '"')
            {
                string value = ReadString(nowChar);
                return new Token(TokenType.String, value);
            }
            else if (IsNumberBegin(nowChar))
            {
                string value = ReadNumber(nowChar);
                return new Token(TokenType.Number, value);
            }
            else if (nowChar == char.MinValue)
            {
                return new Token(TokenType.EndDocument, "EOF");
            }
            else
            {
                throw new Exception($"错误的JSON字符串，位于{_Position}处");
            }
        }
    }

    public class JsonDataBase { }

    public class JsonObject : JsonDataBase
    {
        private Dictionary<string, object> innerData = new Dictionary<string, object>();

        public object this[string key] 
        {
            get { return innerData[key]; }
            set { innerData[key] = value; }
        }

        public List<string> GetKeys()
        {
            return this.innerData.Keys.ToList();
        }

        public JsonObject(ref List<Token> tokenList)
        {
            Stack<Token> tokenStack = new Stack<Token>();
            // push begin object into stack; remove token in tokenlist
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenStack.Push(nowToken);

            // 声明占位，switch里使用
            Token prevToken = null;

            while (tokenStack.Count > 0 && tokenList.Count > 0)
            {
                nowToken = tokenList[0];
                try
                {
                    switch (nowToken.GetTokenType())
                    {
                        // 读到string类型会有两种情况，依据前一位Token判断
                        case TokenType.String:
                            prevToken = tokenStack.Peek();
                            // 若String是Key，则前一个Token是“，”或者“{”，将Token入栈，读取下一个分号
                            // 第一个元素的前一个一定是“{”，非第一个元素必须是“,”
                            if ((prevToken.GetTokenType() == TokenType.BeginObject && innerData.Count == 0) || prevToken.GetTokenType() == TokenType.SepComma)
                            {
                                // Comma的话可以直接弹出了
                                if (prevToken.GetTokenType() == TokenType.SepComma)
                                {
                                    tokenStack.Pop();
                                }
                                tokenStack.Push(nowToken);
                                tokenList.RemoveAt(0);
                            }
                            // 若String是Value，前一个Token是“:”，则将前两个Token出栈，构成<string, string>键值对加入innerData
                            else if (prevToken.GetTokenType() == TokenType.SepColon)
                            {
                                // 查看Stack内是否存在足够的Token对象
                                if (tokenStack.Count <= 2) throw new Exception("错误的Json字符串，存在错误的键值对！");
                                // 连续弹出两个对象
                                Token colonToken = tokenStack.Pop();
                                Token keyToken = tokenStack.Pop();
                                // Check对象的种类是否正确
                                if (colonToken.GetTokenType() == TokenType.SepColon && keyToken.GetTokenType() == TokenType.String)
                                {
                                    innerData.Add(keyToken.GetValue(), nowToken.GetValue());
                                    // TokenList中移除当前Token，继续读取下一个
                                    tokenList.RemoveAt(0);
                                }
                                else
                                {
                                    throw new Exception($"错误的Json字符串，存在错误的键值对在{keyToken.GetValue()}:{nowToken.GetValue()}处");
                                }
                            }
                            else
                            {
                                throw new Exception($"错误的Json字符串，存在异常字符串无法正确匹配至对象");
                            }
                            break;
                        // 读到分号直接入栈
                        case TokenType.SepColon:
                            tokenStack.Push(nowToken);
                            tokenList.RemoveAt(0);
                            break;
                        // 读到逗号直接入栈
                        case TokenType.SepComma:
                            tokenStack.Push(nowToken);
                            tokenList.RemoveAt(0);
                            break;
                        // Null、Number、Boolean只能作为Value存在，看前面的Colon和Key是否正确
                        case TokenType.Null:
                        case TokenType.Number:
                        case TokenType.Boolean:
                            prevToken = tokenStack.Peek();
                            if (prevToken.GetTokenType() == TokenType.SepColon)
                            {
                                if (tokenStack.Count <= 2) throw new Exception("错误的Json字符串，存在错误的键值对！");
                                Token colonToken = tokenStack.Pop();
                                Token keyToken = tokenStack.Pop();
                                if (colonToken.GetTokenType() == TokenType.SepColon && keyToken.GetTokenType() == TokenType.String)
                                {
                                    innerData.Add(keyToken.GetValue(), nowToken.GetValue());
                                    // TokenList中移除当前Token，继续读取下一个
                                    tokenList.RemoveAt(0);
                                }
                                else
                                {
                                    throw new Exception($"错误的Json字符串，存在错误的键值对在{keyToken.GetValue()}:{nowToken.GetValue()}处！");
                                }
                            }
                            else
                            {
                                throw new Exception($"错误的JSON字符串，存在异常的null或数字无法正确匹配至对象！");
                            }
                            break;
                        // BeginArray JsonArray对象仅可作为Value存在，在“:”后直接递归创建JsonArray对象
                        case TokenType.BeginArray:
                            prevToken = tokenStack.Peek();
                            if (prevToken.GetTokenType() == TokenType.SepColon)
                            {
                                if (tokenStack.Count <= 2) throw new Exception("错误的Json字符串，存在错误的键值对！");
                                Token colonToken = tokenStack.Pop();
                                Token keyToken = tokenStack.Pop();
                                if (colonToken.GetTokenType() == TokenType.SepColon && keyToken.GetTokenType() == TokenType.String)
                                {
                                    JsonArray newArray = new JsonArray(ref tokenList);
                                    innerData.Add(keyToken.GetValue(), newArray);
                                }
                                else
                                {
                                    throw new Exception($"错误的Json字符串，存在异常的数组起始无法正确匹配至对象！");
                                }
                            }
                            else
                            {
                                throw new Exception($"错误的JSON字符串，存在异常的数组起始无法正确匹配至对象！");
                            }
                            break;
                        case TokenType.BeginObject:
                            prevToken = tokenStack.Peek();
                            if (prevToken.GetTokenType() == TokenType.SepColon)
                            {
                                if (tokenStack.Count <= 2) throw new Exception("错误的Json字符串，存在错误的键值对！");
                                Token colonToken = tokenStack.Pop();
                                Token keyToken = tokenStack.Pop();
                                if (colonToken.GetTokenType() == TokenType.SepColon && keyToken.GetTokenType() == TokenType.String)
                                {
                                    JsonObject newObject = new JsonObject(ref tokenList);
                                    innerData.Add(keyToken.GetValue(), newObject);
                                }
                                else
                                {
                                    throw new Exception($"错误的Json字符串，存在异常的数组起始无法正确匹配至对象！");
                                }
                            }
                            else
                            {
                                throw new Exception($"错误的JSON字符串，存在异常的数组起始无法正确匹配至对象！");
                            }
                            break;
                        case TokenType.EndObject:
                            prevToken = tokenStack.Peek();
                            if (prevToken.GetTokenType() != TokenType.BeginObject)
                            {
                                throw new Exception($"无法找到对象开始标识符");
                            }
                            else
                            {
                                tokenStack.Pop();
                                tokenList.RemoveAt(0);
                                if (tokenStack.Count != 0)
                                {
                                    throw new Exception($"堆栈中存在多余的标识");
                                }
                            }
                            break;
                        case TokenType.EndArray:
                        case TokenType.EndDocument:
                        default:
                            throw new Exception($"错误的JSON字符串，Object内存在无法识别的字符");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }
        }
    }

    public class JsonArray : JsonDataBase
    {
        private List<object> innerData = new List<object>();

        public object this[int i]
        {
            get { return innerData[i]; }
            set { innerData[i] = value; }
        }

        public int GetLength()
        {
            return this.innerData.Count;
        }

        public JsonArray(ref List<Token> tokenList)
        {
            Stack<Token> tokenStack = new Stack<Token>();
            // push begin object into stack; remove token in tokenlist
            Token nowToken = tokenList[0];
            tokenList.RemoveAt(0);
            tokenStack.Push(nowToken);

            Token prevToken = null;

            while (tokenStack.Count > 0 && tokenList.Count > 0)
            {
                nowToken = tokenList[0];
                switch (nowToken.GetTokenType())
                {
                    // ","直接添加即可
                    case TokenType.SepComma:
                        tokenStack.Push(nowToken);
                        tokenList.RemoveAt(0);
                        break;
                    case TokenType.BeginArray:
                        prevToken = tokenStack.Peek();
                        if ((prevToken.GetTokenType() == TokenType.BeginArray && innerData.Count == 0) || prevToken.GetTokenType() == TokenType.SepComma)
                        {
                            if (prevToken.GetTokenType() == TokenType.SepComma) tokenStack.Pop();
                            JsonArray newArray = new JsonArray(ref tokenList);
                            innerData.Add(newArray);
                        }
                        else
                        {
                            throw new Exception($"错误的JSON字符串，数组中缺少分隔符！");
                        }
                        break;
                    case TokenType.BeginObject:
                        prevToken = tokenStack.Peek();
                        if ((prevToken.GetTokenType() == TokenType.BeginArray && innerData.Count == 0) || prevToken.GetTokenType() == TokenType.SepComma)
                        {
                            if (prevToken.GetTokenType() == TokenType.SepComma) tokenStack.Pop();
                            JsonObject newObject = new JsonObject(ref tokenList);
                            innerData.Add(newObject);
                        }
                        else
                        {
                            throw new Exception($"错误的JSON字符串，数组中缺少分隔符！");
                        }
                        break;
                    case TokenType.Boolean:
                    case TokenType.Null:
                    case TokenType.String:
                    case TokenType.Number:
                        prevToken = tokenStack.Peek();
                        if ((prevToken.GetTokenType() == TokenType.BeginArray && innerData.Count == 0) || prevToken.GetTokenType() == TokenType.SepComma)
                        {
                            if (prevToken.GetTokenType() == TokenType.SepComma) tokenStack.Pop();
                            innerData.Add(nowToken.GetValue());
                            tokenList.RemoveAt(0);
                        }
                        else
                        {
                            throw new Exception($"错误的JSON字符串，数组中缺少分隔符！");
                        }
                        break;
                    case TokenType.EndArray:
                        prevToken = tokenStack.Peek();
                        if (prevToken.GetTokenType() != TokenType.BeginArray)
                        {
                            throw new Exception($"无法找到数组开始标识符！");
                        }
                        else
                        {
                            tokenStack.Pop();
                            tokenList.RemoveAt(0);
                            if (tokenStack.Count != 0)
                            {
                                throw new Exception($"堆栈中存在多余的标识！");
                            }
                        }
                        break;
                    case TokenType.EndDocument:
                    case TokenType.EndObject:
                    case TokenType.SepColon:
                    default:
                        throw new Exception($"错误的JSON字符串，Array内存在无法识别的字符！");
                }
            }
        }
    }

    public class JsonParser
    {
        public static JsonDataBase Parse(string jsonString)
        {
            List<Token> tokenList = new List<Token>();
            CharReader reader = new CharReader(jsonString);
            while (reader.HasMore())
            {
                Token newToken = reader.Start();
                tokenList.Add(newToken);
            }

            int nowIndex = 0;
            Token nowToken = tokenList[nowIndex++];

            switch (nowToken.GetTokenType()){
                case TokenType.BeginObject:
                    JsonObject newJsonObject = new JsonObject(ref tokenList);
                    return newJsonObject;
                case TokenType.BeginArray:
                    JsonArray newJsonArray = new JsonArray(ref tokenList);
                    return newJsonArray;
                default:
                    throw new Exception("非法的JSON字符串，起始位置标识符错误！");
            }
        }
    }
}
