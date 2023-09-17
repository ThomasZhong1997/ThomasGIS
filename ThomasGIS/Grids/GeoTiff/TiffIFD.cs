using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Grids.GeoTiff
{
    public class TiffIFD
    {
        public int DENumber { get; }

        public Dictionary<int, TiffDE> DEList { get; }

        public TiffIFD(int deNumber)
        {
            this.DENumber = deNumber;
            this.DEList = new Dictionary<int, TiffDE>();
        }
    }
}
