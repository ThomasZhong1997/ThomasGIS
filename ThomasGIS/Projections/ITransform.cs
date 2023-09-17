using ThomasGIS.Geometries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Projections
{
    public interface ITransform
    {
        void Toward(IPoint point);

        void Backward(IPoint point);
    }
}
