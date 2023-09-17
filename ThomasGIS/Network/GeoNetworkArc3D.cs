using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.Network
{
    public class GeoNetworkArc3D : SingleLine, INetworkArc
    {
        private int startNodeID;
        private int endNodeID;

        public Dictionary<string, object> PropertySheet { get; } = new Dictionary<string, object>();

        public GeoNetworkArc3D(GeoNetworkNode3D startNode, GeoNetworkNode3D endNode, Dictionary<string, object> propertySheet = null) : base(startNode, endNode)
        {
            this.startNodeID = startNode.GetID();
            this.endNodeID = endNode.GetID();

            if (propertySheet != null)
            {
                foreach (string key in propertySheet.Keys)
                {
                    this.PropertySheet.Add(key, propertySheet[key]);
                }
            }
        }

        public int GetStartNodeID()
        {
            return startNodeID;
        }

        public int GetEndNodeID()
        {
            return endNodeID;
        }

        public object GetProperty(string key)
        {
            if (this.PropertySheet.ContainsKey(key))
            {
                return this.PropertySheet[key];
            }
            return null;
        }

        public bool SetProperty(string key, object value)
        {
            this.PropertySheet[key] = value;
            return true;
        }

        public IEnumerable<string> GetKeys()
        {
            return this.PropertySheet.Keys;
        }

        public Dictionary<string, object> GetPropertySheet()
        {
            return this.PropertySheet;
        }
    }
}
