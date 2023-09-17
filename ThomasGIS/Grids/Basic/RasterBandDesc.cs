using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Grids.Basic
{
    public class RasterBandDesc
    {
        public int height = -1;
        public int width = -1;
        public List<int> colorDepth = new List<int>();
        public int zipFlag = 0;
        public int reverseFalg = 0;
        public int lineOffset = -1;
        public int lineNumber = -1;
        public int imageByteNumber = -1;
        public int horizontalOffset_1 = 3000000;
        public int horizontalOffset_2 = 10000;
        public int verticalOffset_1 = 3000000;
        public int verticalOffset_2 = 10000;
        public string software = "ThomasGIS";
        public string time = "";
        public int colorBandOffset = -1;
        public int samplesPerPixel = 3;
        public int tileWidth = -1;
        public int tileHeight = -1;
        public List<int> tileOffsets = new List<int>();
        public List<int> tileByteCounts = new List<int>();
        public List<int> sampleFormat = new List<int>();
        public int extraSamples = -1;
        public int RGB = 1;
    }
}
