using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Coordinates;
using ThomasGIS.Geometries;

namespace ThomasGIS.Vector
{
    public interface IGeoJson
    {
        bool AddFeature(string wkt, Dictionary<string, string> properties=null);

        bool RemoveFeature(int index);

        IEnumerable<IGeometry> GetFeatures();

        IGeometry GetFeature(int index);

        int GetFeatureNumber();
    }
}
