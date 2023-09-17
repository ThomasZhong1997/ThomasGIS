using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Geometries.OpenGIS
{
    public class OpenGIS_Point : Point
    {
        public Dictionary<string, string> Properties { get; } = new Dictionary<string, string>();

        public OpenGIS_Point(double x, double y, Dictionary<string, string> properties=null) : base(x, y)
        {
            if (properties == null)
            {
                foreach (KeyValuePair<string, string> oneProperty in properties)
                {
                    properties.Add(oneProperty.Key, oneProperty.Value);
                }
            }
        }

        public OpenGIS_Point(string wkt, Dictionary<string, string> properties = null) : base(wkt)
        {
            if (properties == null)
            {
                foreach (KeyValuePair<string, string> oneProperty in properties)
                {
                    properties.Add(oneProperty.Key, oneProperty.Value);
                }
            }
        }

        public new string GetGeometryType()
        {
            return "OpenGIS_Point";
        }
    }
}
