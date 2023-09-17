using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace ThomasGIS.Geometries
{
    public class BoundaryBox
    {
        public double XMin { get; set; } = 0;
        public double YMin { get; set; } = 0;
        public double XMax { get; set; } = 0;
        public double YMax { get; set; } = 0;
        public double ZMin { get; set; } = 0;
        public double ZMax { get; set; } = 0;

        public BoundaryBox() { }

        public BoundaryBox(double xmin, double ymin, double xmax, double ymax)
        {
            XMin = xmin;
            YMin = ymin;
            XMax = xmax;
            YMax = ymax;
        }

        public BoundaryBox(double xMin, double yMin, double xMax, double yMax, double zMin, double zMax) : this(xMin, yMin, xMax, yMax)
        {
            ZMin = zMin;
            ZMax = zMax;
        }

        public BoundaryBox(BoundaryBox self)
        {
            XMin = self.XMin;
            YMin = self.YMin;
            XMax = self.XMax;
            YMax = self.YMax;
            ZMin = self.ZMin;
            ZMax = self.ZMax;
        }
    }
}
