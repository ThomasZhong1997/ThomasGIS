using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Geometry3D
{
    /// <summary>
    /// 直线类，用于进行几何计算
    /// </summary>
    public class Line
    {
        public Vector3D P = null;
        public Vector3D Q = null;
        public Vector3D N = null;

        /// <summary>
        /// 构造函数，输入参数化表示的直线（两点）或隐式表示的直线（一点一法向量）
        /// </summary>
        /// <param name="p">第一个点</param>
        /// <param name="qn">第二个点或者法向量</param>
        /// <param name="useNormal">false时qn为点，true时qn为法向量</param>
        public Line(Vector3D p, Vector3D qn, bool useNormal = false)
        {
            this.P = p;

            if (useNormal)
            {
                this.N = qn;
            }
            else
            {
                this.Q = qn;
            }
        }

        /// <summary>
        /// 获取直线类型，返回1为参数化直线，2为隐式直线，0表示直线有错误
        /// </summary>
        public int LineType
        {
            get
            {
                if (P != null && Q != null) return 1;
                if (P != null && N != null) return 2;
                return 0;
            }
        }
    }
}
