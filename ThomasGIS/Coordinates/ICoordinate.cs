using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ThomasGIS.Coordinates
{
    public interface ICoordinate
    {
        string ExportToWkt();
    }
}
