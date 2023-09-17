using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Coordinates
{
    public abstract class CoordinateBase : ICoordinate
    {
        public virtual string ExportToWkt()
        {
            return "";
        }

        public virtual CoordinateType GetCoordinateType()
        {
            return CoordinateType.Unknown;
        }
    }
}
