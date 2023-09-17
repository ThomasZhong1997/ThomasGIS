using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS.Network
{
    public interface INetworkNode
    {
        int GetID();

        object GetProperty(string key);

        bool SetProperty(string key, object value);

        IEnumerable<string> GetKeys();

        Dictionary<string, object> GetPropertySheet();
    }
}
