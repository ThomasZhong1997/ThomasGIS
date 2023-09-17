using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS.Helpers
{
    class StandardzationHelper
    {
        public static double Sigmoid(double inputValue)
        {
            if (inputValue < -20) return 0;
            double result = 1.0 / (1.0 + Math.Exp(-inputValue));
            return result;
        }

        public static double Relu(double inputValue)
        {
            if (inputValue < 0) return 0;
            return inputValue;
        }
    }
}
