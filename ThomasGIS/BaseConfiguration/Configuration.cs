using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ThomasGIS.BaseConfiguration
{
    public static class Configuration
    {
        private static Dictionary<string, string> packageEnvironment = new Dictionary<string, string>();

        static Configuration() 
        {
            string confFilePath = "./conf.xml";
            XmlDocument xd = new XmlDocument();
            xd.Load(confFilePath);
            XmlNodeList propertyList = xd.GetElementsByTagName("property");
            foreach (XmlNode oneProperty in propertyList) 
            {
                XmlNodeList keyValue = oneProperty.ChildNodes;
                string key = keyValue.Item(0).InnerText;
                string value = keyValue.Item(1).InnerText;
                if (packageEnvironment.ContainsKey(key))
                {
                    packageEnvironment[key] = value;
                }
                else
                {
                    packageEnvironment.Add(key, value);
                }
            }
        }

        public static string GetConfiguration(string key) 
        {
            if (packageEnvironment.ContainsKey(key)) 
            {
                return packageEnvironment[key];
            }
            return "";
        }

        public static void SetConfiguration(string key, string value) 
        {
            if (packageEnvironment.ContainsKey(key))
            {
                packageEnvironment[key] = value;
            }
            else
            {
                packageEnvironment.Add(key, value);
            }
        }
    }
}
