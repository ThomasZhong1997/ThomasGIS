using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Geometry3D;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh
{
    public static class GeometryMathmatics
    {
        #region 求直线交点
        /// <summary>
        /// 通用的计算两条直线交点的函数，可应用于隐式和参数化表示的直线
        /// </summary>
        /// <param name="line1">第一条直线</param>
        /// <param name="line2">第二条直线</param>
        /// <returns>交点坐标</returns>
        public static Vector3D ComputeCrossPosition(Line line1, Line line2)
        {
            if (line1.LineType == 1 && line2.LineType == 1)
            {
                return ComputeCrossPosition1(line1, line2);
            }

            if (line1.LineType == 1 && line2.LineType == 2)
            {
                return ComputeCrossPosition2(line1, line2);
            }

            if (line1.LineType == 2 && line2.LineType == 1)
            {
                return ComputeCrossPosition2(line2, line1);
            }

            throw new Exception("Not support!");
        }

        /// <summary>
        /// 私有函数，用于计算参数化表示的两条直线的交点
        /// </summary>
        /// <param name="line1">第一条直线</param>
        /// <param name="line2">第二条直线</param>
        /// <returns>交点坐标</returns>
        private static Vector3D ComputeCrossPosition1(Line line1, Line line2)
        {
            Vector3D v = line1.P - line1.Q;
            Vector3D u = line2.P - line2.Q;

            Vector3D xV = v.Cross();

            if (u.Dot(xV) == 0) throw new Exception("parallel line: no cross point!");

            double s0 = (line1.Q - line2.Q).Dot(xV) / u.Dot(xV);

            return line2.P * s0 + line2.Q * (1 - s0);
        }

        /// <summary>
        /// 私有函数，用于计算参数化表示和隐式表示的两条直线的交点
        /// </summary>
        /// <param name="line1">参数化表示直线</param>
        /// <param name="line2">隐式表示直线</param>
        /// <returns>交点坐标</returns>
        private static Vector3D ComputeCrossPosition2(Line line1, Line line2)
        {
            Vector3D u = line1.Q - line1.P;
            Vector3D v = line1.P - line2.P;
            Vector3D n = line2.N;

            if (u.Dot(n) == 0) throw new Exception("parallel line: no cross point!");

            double t0 = -v.Dot(n) / u.Dot(n);

            return (1 - t0) * line1.P + t0 * line1.Q;
        }

        #endregion

        #region 光线-平面求交

        #endregion
    }
}
