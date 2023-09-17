using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public interface IProjection
    {
        void Toward(IPoint point, double a = 6378137, double flatten = 298.257223563);

        void Backward(IPoint point, double a = 6378137, double flatten = 298.257223563);

        Dictionary<string, double> GetWrittenParameters();

        bool SetParameters(Dictionary<string, double> inputParameter);

        string GetProjectionName();

        string GetProjectionType();
    }
}
