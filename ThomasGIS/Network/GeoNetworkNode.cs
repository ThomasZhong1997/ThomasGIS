using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Network;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Network
{
    public class GeoNetworkNode : Point, INetworkNode
    {
        private int ID;

        private Dictionary<string, object> PropertySheet = new Dictionary<string, object>();

        public GeoNetworkNode(int nodeID, double x, double y, Dictionary<string, object> nodeProperties = null) : base(x, y)
        {
            this.X = x;
            this.Y = y;
            this.ID = nodeID;

            if (nodeProperties != null)
            {
                foreach (string key in nodeProperties.Keys)
                {
                    this.PropertySheet.Add(key, nodeProperties[key]);
                }
            }
        }

        public IShpPoint TransferToShpPoint()
        {
            return new ShpPoint(X, Y);
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
