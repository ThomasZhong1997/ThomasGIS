using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.Network
{
    public class GeoNetworkArc : SingleLine, INetworkArc
    {
        private int startNodeID;
        private int endNodeID;

        public Dictionary<string, object> PropertyTable { get; } = new Dictionary<string, object>();

        public GeoNetworkArc(GeoNetworkNode startNode, GeoNetworkNode endNode, Dictionary<string, object> propertyTable = null) : base(startNode, endNode)
        {
            this.startNodeID = startNode.GetID();
            this.endNodeID = endNode.GetID();

            if (propertyTable != null)
            {
                foreach (string key in propertyTable.Keys)
                {
                    this.PropertyTable.Add(key, propertyTable[key]);
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
            if (this.PropertyTable.ContainsKey(key))
            {
                return this.PropertyTable[key];
            }
            return null;
        }

        public bool SetProperty(string key, object value)
        {
            this.PropertyTable[key] = value;
            return true;
        }

        public IEnumerable<string> GetKeys()
        {
            return this.PropertyTable.Keys;
        }

        public Dictionary<string, object> GetPropertySheet()
        {
            return this.PropertyTable;
        }
    }
}
