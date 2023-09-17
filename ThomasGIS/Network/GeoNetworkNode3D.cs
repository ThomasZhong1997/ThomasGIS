using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries.Shapefile;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Network
{
    public class GeoNetworkNode3D : Vector3D, INetworkNode
    {
        private int ID;

        public Dictionary<string, object> PropertySheet { get; } = new Dictionary<string, object>();

        public GeoNetworkNode3D(int nodeId, Vector3D position, Dictionary<string, object> nodeProperties = null) : base(position)
        {
            this.ID = nodeId;

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
            return new ShpPoint3D(X, Y, Z, 0);
        }

        public int GetID()
        {
            return this.ID;
        }

        public IEnumerable<string> GetKeys()
        {
            return this.PropertySheet.Keys;
        }

        public object GetProperty(string key)
        {
            if (this.PropertySheet.ContainsKey(key))
            {
                return this.PropertySheet[key];
            }

            return null;
        }

        public Dictionary<string, object> GetPropertySheet()
        {
            return this.PropertySheet;
        }

        public bool SetProperty(string key, object value)
        {
            if (this.PropertySheet.ContainsKey(key))
            {
                this.PropertySheet[key] = value;
            }
            else
            {
                this.PropertySheet.Add(key, value);
            }

            return true;
        }
    }
}
