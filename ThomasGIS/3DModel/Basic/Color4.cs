using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS._3DModel.Basic
{
    public class Color4
    {
        public int Red;
        public int Green;
        public int Blue;
        public double Alpha;

        public Color4(int red, int green, int blue, double alpha)
        {
            this.Red = red;
            this.Green = green;
            this.Blue = blue;
            this.Alpha = alpha;
        }
    }
}
