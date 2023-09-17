using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public class NetworkNode : INetworkNode
    {
        public int ID { get; set; }

        public Dictionary<string, object> PropertySheet = new Dictionary<string, object>();

        public NetworkNode(int nodeID, Dictionary<string, object> nodeProperties = null)
        {
            this.ID = nodeID;
            if (nodeProperties != null)
            {
                foreach (string key in nodeProperties.Keys)
                {
                    this.PropertySheet.Add(key, nodeProperties[key]);
                }
            }
        }

        public int GetID()
        {
            return this.ID;
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
