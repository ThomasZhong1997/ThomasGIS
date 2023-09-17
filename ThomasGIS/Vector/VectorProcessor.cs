using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;
using ThomasGIS.Geometries.Shapefile;

namespace ThomasGIS.Vector
{
    public static class VectorProcessor
    {
        // 基于单要素创建环形缓冲区，自动识别Geometry类型
        public static IShpPolygon CreateBuffer(IGeometry geometry, double outSideRadius, double inSideRadius, int precision = 32)
        {
            // 如果外环半径小于内环半径，则将两者交换
            if (outSideRadius <= inSideRadius) 
            {
                outSideRadius += inSideRadius;
                inSideRadius -= outSideRadius;
                inSideRadius = -inSideRadius;
                outSideRadius -= inSideRadius;
            }



            return null;
        }

        // 基于Shapefile创建缓冲区
        public static IShapefile CreateBuffer(IShapefile shapefile, double outSideRadius, double inSideRadius, int precision = 32)
        {

            return null;
        }
    }
}
