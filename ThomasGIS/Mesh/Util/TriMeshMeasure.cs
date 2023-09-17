using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshMeasure
    {
        #region 长度计算

        /// <summary>
        /// 计算三维模型Trimesh中一条边的笛卡尔长度
        /// </summary>
        /// <param name="edge">目标边</param>
        /// <returns>返回长度</returns>
        public static double ComputeEdgeLength(TriMesh.Edge edge)
        {
            Vector3D v1 = edge.Vertex0.Traits.Position;
            Vector3D v2 = edge.Vertex1.Traits.Position;

            return Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
        }

        /// <summary>
        /// 计算三维模型Trimesh中两个顶点间的笛卡尔长度
        /// </summary>
        /// <param name="vx1">第一个顶点</param>
        /// <param name="vx2">第二个顶点</param>
        /// <returns>返回长度</returns>
        public static double ComputeLength(TriMesh.Vertex vx1, TriMesh.Vertex vx2)
        {
            Vector3D v1 = vx1.Traits.Position;
            Vector3D v2 = vx2.Traits.Position;
            return Math.Sqrt(Math.Pow(v1.X - v2.X, 2) + Math.Pow(v1.Y - v2.Y, 2) + Math.Pow(v1.Z - v2.Z, 2));
        }

        #endregion

        #region 中点计算

        /// <summary>
        /// 计算三维模型Trimesh中一条边的中点
        /// </summary>
        /// <param name="edge">目标边</param>
        /// <returns>返回中点坐标</returns>
        public static Vector3D GetMidPoint(TriMesh.Edge edge)
        {
            Vector3D v1 = edge.Vertex0.Traits.Position;
            Vector3D v2 = edge.Vertex1.Traits.Position;
            return (v1 + v2) / 2.0;
        }

        /// <summary>
        /// 计算三维模型Trimesh中两个顶点的中点坐标
        /// </summary>
        /// <param name="v1">第一个顶点</param>
        /// <param name="v2">第二个顶点</param>
        /// <returns>返回中点坐标</returns>
        public static Vector3D GetMidPoint(TriMesh.Vertex v1, TriMesh.Vertex v2)
        {
            Vector3D v11 = v1.Traits.Position;
            Vector3D v12 = v2.Traits.Position;
            return (v11 + v12) / 2.0;
        }

        /// <summary>
        /// 计算三维模型Trimesh中一个面的中点
        /// </summary>
        /// <param name="face">目标面</param>
        /// <returns>返回中点坐标</returns>
        public static Vector3D GetMidPoint(TriMesh.Face face)
        {
            Vector3D sum = new Vector3D(0, 0, 0);
            foreach (TriMesh.Vertex v in face.Vertices)
            {
                sum += v.Traits.Position;
            }
            return sum /= face.VertexCount;
        }

        #endregion

        #region 面积计算

        /// <summary>
        /// 计算三维模型Trimesh中一个面的面积
        /// </summary>
        /// <param name="face">目标面</param>
        /// <returns>返回浮点型面积</returns>
        public static double ComputeFaceArea(TriMesh.Face face)
        {
            Vector3D v1 = face.GetVertex(0).Traits.Position;
            Vector3D v2 = face.GetVertex(1).Traits.Position;
            Vector3D v3 = face.GetVertex(2).Traits.Position;

            double a = Math.Sqrt(Vector3D.Dot(v1 - v2, v1 - v2));
            double b = Math.Sqrt(Vector3D.Dot(v3 - v2, v3 - v2));
            double c = Math.Sqrt(Vector3D.Dot(v1 - v3, v1 - v3));

            double p = (a + b + c) / 2.0;

            return Math.Sqrt(p * (p - a) * (p - b) * (p - c));
        }

        /// <summary>
        /// 计算三维模型Trimesh中一个顶点的Voronoi面积
        /// </summary>
        /// <param name="vertex">目标顶点</param>
        /// <returns>返回浮点型面积</returns>
        public static double ComputeVertexAreaVoronoi(TriMesh.Vertex vertex)
        {
            double sum = 0;
            Vector3D mid = vertex.Traits.Position;
            foreach (TriMesh.HalfEdge hf in vertex.HalfEdges)
            {
                Vector3D bottom = hf.ToVertex.Traits.Position;
                Vector3D left = hf.Opposite.Next.ToVertex.Traits.Position;
                Vector3D right = hf.Next.ToVertex.Traits.Position;

                double cota = (mid - left).Dot(bottom - left) / (mid - left).Cross(bottom - left).Length();
                double cotb = (mid - right).Dot(bottom - right) / (mid - right).Cross(bottom - right).Length();
                double d = Math.Pow(mid.DistanceWith(bottom), 2);

                sum += (cota + cotb) * d;
            }
            double area = sum / 8.0;
            return area;
        }

        /// <summary>
        /// 计算三维模型Trimesh中一个顶点的混合面积
        /// </summary>
        /// <param name="vertex">目标顶点</param>
        /// <returns>返回浮点型面积</returns>
        public static double ComputeVertexAreaMixed(TriMesh.Vertex vertex)
        {
            double area = 0;
            foreach (TriMesh.HalfEdge hf in vertex.HalfEdges)
            {
                TriMesh.Vertex p1 = hf.FromVertex;
                TriMesh.Vertex p2 = hf.ToVertex;
                TriMesh.Vertex p3 = hf.Next.ToVertex;

                Vector3D v1 = p1.Traits.Position;
                Vector3D v2 = p2.Traits.Position;
                Vector3D v3 = p3.Traits.Position;

                double dis1 = Math.Pow((v3 - v2).Length(), 2);
                double dis2 = Math.Pow((v3 - v1).Length(), 2);
                double dis3 = Math.Pow((v1 - v2).Length(), 2);

                double cot1 = (v2 - v1).Dot(v3 - v1) / (v2 - v1).Cross(v3 - v1).Length();
                double cot2 = (v3 - v2).Dot(v1 - v2) / (v3 - v2).Cross(v1 - v2).Length();
                double cot3 = (v1 - v3).Dot(v2 - v3) / (v1 - v3).Cross(v2 - v3).Length();

                if (cot1 > 0 && cot2 > 0 && cot3 > 0)
                {
                    area += (dis2 * cot2 + dis3 * cot3) / 8.0;
                }
                else
                {
                    if (hf.Face == null) continue;
                    double faceArea = ComputeFaceArea(hf.Face);
                    if (cot1 < 0)
                    {
                        area += faceArea / 2.0;
                    }
                    else
                    {
                        area += faceArea / 4.0;
                    }
                }
            }
            return area;
        }

        #endregion

        #region 体积计算

        /// <summary>
        /// 计算Trimesh的体积
        /// </summary>
        /// <param name="mesh">目标Trimesh</param>
        /// <returns>返回浮点型体积</returns>
        public static double ComputeVolumn(TriMesh mesh)
        {
            int n = mesh.Faces.Count;
            double volumn = 0;
            for (int i = 0; i < n; i++)
            {
                Vector3D vertexA = mesh.Faces[i].GetVertex(0).Traits.Position;
                Vector3D vertexB = mesh.Faces[i].GetVertex(1).Traits.Position;
                Vector3D vertexC = mesh.Faces[i].GetVertex(2).Traits.Position;

                double v123 = vertexA.X * vertexB.Y * vertexC.Z;
                double v231 = vertexA.Y * vertexB.Z * vertexC.X;
                double v312 = vertexA.Z * vertexB.X * vertexC.Y;
                double v132 = vertexA.X * vertexB.Z * vertexC.Y;
                double v213 = vertexA.Y * vertexB.X * vertexC.Z;
                double v321 = vertexA.Z * vertexB.Y * vertexC.X;

                volumn += (v123 + v231 + v312 - v132 - v213 - v321) / 6.0;
            }
            return volumn;
        }

        #endregion

        #region 角度计算

        /// <summary>
        /// 根据余弦定理计算目标半边的对角大小
        /// </summary>
        /// <param name="hf">目标半边</param>
        /// <returns>对角大小，单位为弧度</returns>
        public static double ComputeAngle(TriMesh.HalfEdge hf)
        {
            TriMesh.HalfEdge prev = hf.Previous;
            TriMesh.HalfEdge next = hf.Next;

            double nowLength = TriMeshMeasure.ComputeEdgeLength(hf.Edge);
            double prevLength = TriMeshMeasure.ComputeEdgeLength(prev.Edge);
            double nextLength = TriMeshMeasure.ComputeEdgeLength(next.Edge);

            double cosA = (prevLength * prevLength + nextLength * nextLength - nowLength * nowLength) / (2.0 * (prevLength * nextLength));
            if (cosA < 0) cosA += Math.PI;
            return Math.Acos(cosA);
        }

        /// <summary>
        /// 计算两个向量之间的夹角，引用参数传递
        /// </summary>
        /// <param name="v1">第一个向量</param>
        /// <param name="v2">第二个向量</param>
        /// <returns>夹角大小，返回弧度</returns>
        public static double ComputeAngle(ref Vector3D v1, ref Vector3D v2)
        {
            double up = v1.Dot(v2);
            double cosAB = up / v1.Length() / v2.Length();
            double angle = Math.Acos(cosAB);
            if (angle < 0) angle += Math.PI;
            return angle;
        }

        /// <summary>
        /// 计算两个向量之间的夹角
        /// </summary>
        /// <param name="v1">第一个向量</param>
        /// <param name="v2">第二个向量</param>
        /// <returns>夹角大小，返回弧度</returns>
        public static double ComputeAngle(Vector3D v1, Vector3D v2)
        {
            double up = v1.Dot(v2);
            double cosAB = up / v1.Length() / v2.Length();
            double angle = Math.Acos(cosAB);
            if (angle < 0) angle += Math.PI;
            return angle;
        }

        /// <summary>
        /// 计算一条edge对应的两个面间的夹角
        /// </summary>
        /// <param name="edge">目标边</param>
        /// <returns>夹角大小，返回弧度</returns>
        public static double ComputeDihedralAngle(TriMesh.Edge edge)
        {
            TriMesh.Face face1 = edge.Face0;
            TriMesh.Face face2 = edge.Face1;

            if (face1 != null && face2 != null)
            {
                Vector3D face1Normal = face1.Normal.Normalize();
                Vector3D face2Normal = face2.Normal.Normalize();

                double cosa = face1Normal.Dot(face2Normal) / (face1Normal.Length() * face2Normal.Length());
                return Math.Acos(cosa);
            }
            else
            {
                return 0;
            }
        }

        #endregion


        #region 曲率计算

        private static Vector3D ComputeK(TriMesh.Vertex v, double mixedArea)
        {
            Vector3D sum = Vector3D.Zero;
            Vector3D mid = v.Traits.Position;

            foreach (TriMesh.HalfEdge hf in v.HalfEdges)
            {
                Vector3D bottom = hf.ToVertex.Traits.Position;
                Vector3D left = hf.Opposite.Next.ToVertex.Traits.Position;
                Vector3D right = hf.Next.ToVertex.Traits.Position;

                double cota = (mid - left).Dot(bottom - left) / (mid - left).Cross(bottom - left).Length();
                double cotb = (mid - right).Dot(bottom - right) / (mid - right).Cross(bottom - right).Length();

                sum += (cota + cotb) * (bottom - mid);
            }

            return sum / mixedArea / 2.0d;
        }

        /// <summary>
        /// 计算顶点的平均曲率
        /// </summary>
        /// <param name="v">目标顶点</param>
        /// <returns>返回双精度曲率</returns>
        public static double ComputeMeanCurvature(TriMesh.Vertex v)
        {
            double mixedArea = TriMeshMeasure.ComputeVertexAreaMixed(v);
            Vector3D K = TriMeshMeasure.ComputeK(v, mixedArea);
            return (K / 2.0).Length();
        }

        /// <summary>
        /// 计算顶点的高斯曲率
        /// </summary>
        /// <param name="v">目标顶点</param>
        /// <returns>返回双精度曲率</returns>
        public static double ComputeGaussianCurvature(TriMesh.Vertex v)
        {
            bool onBoundary = false;
            double curvature = 0;

            foreach (TriMesh.HalfEdge hf in v.HalfEdges)
            {
                if (hf.OnBoundary)
                {
                    onBoundary = true;
                }
                else
                {
                    Vector3D v1 = (hf.Previous.FromVertex.Traits.Position - v.Traits.Position).Normalize();
                    Vector3D v2 = (hf.ToVertex.Traits.Position - v.Traits.Position).Normalize();
                    curvature -= ComputeAngle(ref v1, ref v2);
                }
            }

            curvature += onBoundary ? Math.PI : 2 * Math.PI;
            return curvature;
        }

        #endregion

    }
}
