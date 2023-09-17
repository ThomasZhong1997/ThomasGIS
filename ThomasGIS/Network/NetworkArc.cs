using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Vector;
using ThomasGIS.Geometries;
using System.Security.Cryptography.X509Certificates;
using ThomasGIS.Projections;
using ThomasGIS.Coordinates;
using ThomasGIS.TrajectoryPackage;

namespace ThomasGIS.Network
{
    public class NetworkArc : INetworkArc
    {
        public int StartNodeID { get; }

        public int EndNodeID { get; }

        private Dictionary<string, object> PropertyTable { get; } = new Dictionary<string, object>();

        public NetworkArc(int startNodeID, int endNodeID, Dictionary<string, object> arcProperties = null)
        {
            this.StartNodeID = startNodeID;
            this.EndNodeID = endNodeID;
            if (arcProperties != null)
            {
                foreach (string key in arcProperties.Keys)
                {
                    this.PropertyTable.Add(key, arcProperties[key]);
                }
            }
        }

        public int GetStartNodeID()
        {
            return StartNodeID;
        }

        public int GetEndNodeID()
        {
            return EndNodeID;
        }

        public object GetProperty(string key)
        {
            return this.PropertyTable[key];
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
