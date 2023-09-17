using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Geometries
{
    public interface ISingleLine : IGeometry
    {
        IPoint GetStartPoint();

        IPoint GetEndPoint();

        bool SetStartX(double x);

        bool SetStartY(double y);

        bool SetEndX(double x);

        bool SetEndY(double y);
    }
}
