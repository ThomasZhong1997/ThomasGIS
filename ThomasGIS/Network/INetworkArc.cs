using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Network
{
    public interface INetworkArc
    {
        int GetStartNodeID();

        int GetEndNodeID();

        object GetProperty(string key);

        bool SetProperty(string key, object value);

        IEnumerable<string> GetKeys();

        Dictionary<string, object> GetPropertySheet();
    }
}
