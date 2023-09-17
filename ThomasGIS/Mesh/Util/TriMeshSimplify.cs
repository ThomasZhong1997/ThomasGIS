using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.DataStructure;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Geometry3D;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Util
{
    // 三维模型化简类
    public static class TriMeshSimplify
    {
        private class MergeArgs : IComparable
        {
            public TriMesh.Edge Edge;
            public double Error;
            public Vector3D Position;
            public bool isDeleted = false;

            public MergeArgs(TriMesh.Edge edge, double error, Vector3D position)
            {
                this.Edge = edge;
                this.Error = error;
                this.Position = position;
            }

            int IComparable.CompareTo(object obj)
            {
                if (this.Error > ((MergeArgs)obj).Error)
                {
                    return 1;
                }
                else
                {
                    return -1;
                }
            }
        }

        public static void VertexClusterSimlify(TriMesh mesh, int splitNumber)
        {
            Vector3D minMesh = TriMeshUtil.GetMinCoord(mesh);
            Vector3D maxMesh = TriMeshUtil.GetMaxCoord(mesh);

            double dx = maxMesh.X - minMesh.X;
            double dy = maxMesh.Y - minMesh.Y;
            double dz = maxMesh.Z - minMesh.Z;

            if (dx == 0 && dy == 0 || dx == 0 && dz == 0 || dy == 0 && dz == 0) return;

            double minValue;

            if (dx != 0 && dy != 0)
            {
                minValue = dx < dy ? dx : dy;
            }
            else
            {
                if (dx == 0) minValue = dy;
                else minValue = dx;
            }

            if (dz != 0)
            {
                minValue = minValue < dz ? minValue : dz;
            }

            double scale = minValue / splitNumber;

            int rows = (int)(dx / scale) + 1;
            int cols = (int)(dy / scale) + 1;
            int levels = (int)(dz / scale) + 1;

            List<TriMesh.Vertex>[,,] cube = new List<TriMesh.Vertex>[rows, cols, levels];
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    for (int k = 0; k < levels; k++)
                    {
                        cube[i, j, k] = new List<TriMesh.Vertex>();
                    }
                }
            }

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                int lx = (int)((mesh.Vertices[i].Traits.Position.X - minMesh.X) / scale);
                int ly = (int)((mesh.Vertices[i].Traits.Position.Y - minMesh.Y) / scale);
                int lz = (int)((mesh.Vertices[i].Traits.Position.Z - minMesh.Z) / scale);
                cube[lx, ly, lz].Add(mesh.Vertices[i]);
            }

            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    for (int k = 0; k < levels; k++)
                    {
                        if (cube[i, j, k].Count == 0) continue;
                        double centerX = 0, centerY = 0, centerZ = 0;
                        for (int m = 0; m < cube[i, j, k].Count; m++)
                        {
                            centerX += cube[i, j, k][m].Traits.Position.X;
                            centerY += cube[i, j, k][m].Traits.Position.Y;
                            centerZ += cube[i, j, k][m].Traits.Position.Z;
                        }
                        centerX /= cube[i, j, k].Count;
                        centerY /= cube[i, j, k].Count;
                        centerZ /= cube[i, j, k].Count;

                        ClusterVertices(cube[i, j, k].ToArray(), new Vector3D(centerX, centerY, centerZ));
                    }
                }
            }
        }

        private static void ClusterVertices(TriMesh.Vertex[] vertices, Vector3D position)
        {
            Queue<TriMesh.Edge> connectedEdge = new Queue<TriMesh.Edge>();
            Dictionary<TriMesh.Edge, bool> processedFlag = new Dictionary<TriMesh.Edge, bool>();
            foreach (TriMesh.Vertex v in vertices)
            {
                foreach (TriMesh.HalfEdge hf in v.HalfEdges)
                {
                    foreach (TriMesh.Vertex vx in vertices)
                    {
                        if (hf.ToVertex == vx && !processedFlag.ContainsKey(hf.Edge))
                        {
                            connectedEdge.Enqueue(hf.Edge);
                            processedFlag[hf.Edge] = true;
                        }
                    }
                }
            }

            int n = 0;

            while (connectedEdge.Count > 0)
            {
                TriMesh.Edge e = connectedEdge.Dequeue();
                if (e.HalfEdge0 == null || e.HalfEdge1 == null) continue;

                if (TriMeshModify.EdgeCanMerge(e))
                {
                    TriMeshModify.MergeEdge(e, position, out var a);
                    n = 0;
                }
                else
                {
                    connectedEdge.Enqueue(e);
                    n++;
                    if (n > connectedEdge.Count) break;
                }
            }
        }

        public static int QuadricErrorMetricSimplify(TriMesh mesh, int keepFaceNumber)
        {
            if (keepFaceNumber >= mesh.Faces.Count) return 0;

            mesh.RefreshAllIndex();

            Dictionary<TriMesh.Vertex, Matrix4D> vertexMatrix = new Dictionary<TriMesh.Vertex, Matrix4D>();
            foreach (TriMesh.Vertex vertex in mesh.Vertices)
            {
                Matrix4D q = ComputeVertexQ(vertex);
                vertexMatrix.Add(vertex, q);
            }

            ThomasGISHeap<TriMesh.Edge, MergeArgs> tgHeap = new ThomasGISHeap<TriMesh.Edge, MergeArgs>(true);

            foreach (TriMesh.Edge edge in mesh.Edges)
            {
                Matrix4D QEdge = Matrix4D.ZeroMatrix;
                QEdge = vertexMatrix[edge.Vertex0] + vertexMatrix[edge.Vertex1];
                EdgeError(QEdge, edge, out var position, out var error);
                tgHeap.Add(edge, new MergeArgs(edge, error, position));
            }

            int originFaceNumber = mesh.Faces.Count;
            while (mesh.Faces.Count > keepFaceNumber)
            {
                tgHeap.PopItem(out var edge, out var temp);
                List<MergeArgs> canNotMergeList = new List<MergeArgs>();
                while (!TriMeshModify.EdgeCanMerge(temp.Edge) && tgHeap.Count > 0)
                {
                    canNotMergeList.Add(temp);
                    tgHeap.PopItem(out edge, out temp);
                }

                if (!TriMeshModify.EdgeCanMerge(temp.Edge) && tgHeap.Count == 0) return originFaceNumber - mesh.Faces.Count;

                foreach (MergeArgs arg in canNotMergeList)
                {
                    tgHeap.Add(arg.Edge, arg);
                }

                TriMesh.Vertex centerVertex = TriMeshModify.MergeEdge(temp.Edge, temp.Position, out var deletedEdges);

                foreach (TriMesh.Edge deletedEdge in deletedEdges)
                {
                    tgHeap.Remove(deletedEdge);
                }

                // 更新顶点的二次误差矩阵
                vertexMatrix[centerVertex] = ComputeVertexQ(centerVertex);
                foreach (TriMesh.Vertex neighborVertex in centerVertex.Vertices)
                {
                    vertexMatrix[neighborVertex] = ComputeVertexQ(neighborVertex);
                }

                // 更新边的，这玩意应该是目前顶点周边两周，因为其余顶点也会被影响到
                Dictionary<TriMesh.Edge, MergeArgs> tempList = new Dictionary<TriMesh.Edge, MergeArgs>();
                foreach (TriMesh.Vertex neighborVertex in centerVertex.Vertices)
                {
                    foreach (TriMesh.Edge effectedEdge in neighborVertex.Edges)
                    {
                        if (tgHeap.ContainsKey(effectedEdge))
                        {
                            Matrix4D QEdge = Matrix4D.ZeroMatrix;
                            QEdge = vertexMatrix[effectedEdge.Vertex0] + vertexMatrix[effectedEdge.Vertex1];
                            EdgeError(QEdge, effectedEdge, out var position, out var error);

                            MergeArgs ori = tgHeap[effectedEdge];
                            ori.Error = error;
                            ori.Position = position;
                            tgHeap.Remove(effectedEdge);
                            tempList.Add(effectedEdge, ori);
                        }
                    }
                }

                foreach (KeyValuePair<TriMesh.Edge, MergeArgs> pair in tempList)
                {
                    tgHeap.Add(pair.Key, pair.Value);
                }
            }

            return originFaceNumber - mesh.Faces.Count;
        }

        private static bool EdgeError(Matrix4D QEdge, TriMesh.Edge edge, out Vector3D position, out double error)
        {
            position = new Vector3D(0, 0, 0);

            Matrix4D QDelta = Matrix4D.ZeroMatrix;
            for (int i = 0; i < 12; i++)
            {
                QDelta[i] = QEdge[i];
            }
            QDelta[12] = 0;
            QDelta[13] = 0;
            QDelta[14] = 0;
            QDelta[15] = 1;

            double det = QDelta.Det();
            if (det != 0)
            {
                Matrix4D invM = QDelta.Inv();
                position = invM.Dot(new Vector4D(0, 0, 0, 1));
            }
            else
            {
                double minError = double.MaxValue;
                Vector3D[] vecArr = new Vector3D[3] {
                        edge.Vertex0.Traits.Position,
                        edge.Vertex1.Traits.Position,
                        (edge.Vertex0.Traits.Position + edge.Vertex1.Traits.Position) / 2
                    };

                for (int i = 0; i < 3; i++)
                {
                    double curError = VertexError(QEdge, vecArr[i]);
                    if (curError < minError)
                    {
                        minError = curError;
                        position = vecArr[i];
                    }
                }
            }

            error = VertexError(QEdge, position);
            return true;
        }

        private static double VertexError(Matrix4D m, Vector3D v)
        {
            Vector4D v4d = new Vector4D(v, 1);
            return v4d.Dot(m).Dot(v4d);
        }

        private static Matrix4D ComputeVertexQ(TriMesh.Vertex vertex)
        {
            Matrix4D q = Matrix4D.ZeroMatrix;
            foreach (TriMesh.Face face in vertex.Faces)
            {
                TriMesh.Vertex v1 = face.GetVertex(0);
                TriMesh.Vertex v2 = face.GetVertex(1);
                TriMesh.Vertex v3 = face.GetVertex(2);

                Plane plane = new Plane(v1.Traits.Position, v2.Traits.Position, v3.Traits.Position);
                Matrix4D k = ComputePlane(plane);
                q = q + k;
            }

            return q;
        }

        private static Matrix4D ComputePlane(Plane plane)
        {
            Matrix4D k = Matrix4D.ZeroMatrix;
            double a = plane.A;
            double b = plane.B;
            double c = plane.C;
            double d = plane.D;

            k[0] = a * a;
            k[1] = a * b;
            k[2] = a * c;
            k[3] = a * d;
            k[4] = a * b;
            k[5] = b * b;
            k[6] = b * c;
            k[7] = b * d;
            k[8] = a * c;
            k[9] = b * c;
            k[10] = c * c;
            k[11] = c * d;
            k[12] = a * d;
            k[13] = b * d;
            k[14] = c * d;
            k[15] = d * d;

            return k;
        }

        public static int EdgeMinLengthMergeSimlify(TriMesh mesh, int keepFaceNumber)
        {
            if (keepFaceNumber >= mesh.Faces.Count) return 0;

            int originFaceNumber = mesh.Faces.Count;

            mesh.RefreshAllIndex();

            ThomasGISHeapDouble<TriMesh.Edge> tgHeap = new ThomasGISHeapDouble<TriMesh.Edge>(true);

            foreach (TriMesh.Edge edge in mesh.Edges)
            {
                tgHeap.Add(edge, TriMeshMeasure.ComputeEdgeLength(edge));
            }

            while (mesh.Faces.Count > keepFaceNumber)
            {
                tgHeap.PopItem(out var edge, out var temp);
                List<TriMesh.Edge> canNotMergeList = new List<TriMesh.Edge>();
                while (!TriMeshModify.EdgeCanMerge(edge) && tgHeap.Count > 0)
                {
                    canNotMergeList.Add(edge);
                    tgHeap.PopItem(out edge, out temp);
                }

                if (!TriMeshModify.EdgeCanMerge(edge) && tgHeap.Count == 0) return originFaceNumber - mesh.Faces.Count;

                foreach (TriMesh.Edge edge1 in canNotMergeList)
                {
                    tgHeap.Add(edge1, TriMeshMeasure.ComputeEdgeLength(edge1));
                }

                Vector3D sourcePosition = edge.Vertex0.Traits.Position;
                Vector3D targetPosition = edge.Vertex1.Traits.Position;
                Vector3D centerPosition = (sourcePosition + targetPosition) / 2.0;
                TriMesh.Vertex centerVertex = TriMeshModify.MergeEdge(edge, centerPosition, out var deletedEdges);

                foreach (TriMesh.Edge deletedEdge in deletedEdges)
                {
                    tgHeap.Remove(deletedEdge);
                }

                Dictionary<TriMesh.Edge, double> tempList = new Dictionary<TriMesh.Edge, double>();
                foreach (TriMesh.Edge nearbyEdge in centerVertex.Edges)
                {
                    tempList.Add(nearbyEdge, TriMeshMeasure.ComputeEdgeLength(nearbyEdge));
                }

                foreach (KeyValuePair<TriMesh.Edge, double> pair in tempList)
                {
                    tgHeap.Add(pair.Key, pair.Value);
                }
            }

            return originFaceNumber - mesh.Faces.Count;
        }

        public static double SimplifiedErrorHaudorff(TriMesh originMesh, TriMesh simplifiedMesh)
        {
            double maxO2S = double.MinValue;

            for (int i = 0; i < simplifiedMesh.Vertices.Count; i++)
            {
                double minLength = double.MaxValue;
                for (int j = 0; j < originMesh.Vertices.Count; j++)
                {
                    minLength = Math.Min(minLength, TriMeshMeasure.ComputeLength(simplifiedMesh.Vertices[i], originMesh.Vertices[j]));
                }
                maxO2S = Math.Max(maxO2S, minLength);
            }

            double maxS2O = double.MinValue;
            for (int i = 0; i < originMesh.Vertices.Count; i++)
            {
                double minLength = double.MaxValue;
                for (int j = 0; j < simplifiedMesh.Vertices.Count; j++)
                {
                    minLength = Math.Min(minLength, TriMeshMeasure.ComputeLength(simplifiedMesh.Vertices[i], originMesh.Vertices[j]));
                }
                maxS2O = Math.Max(maxS2O, minLength);
            }

            return Math.Max(maxS2O, maxO2S);
        }

        public static double SimplifiedErrorAverage(TriMesh originMesh, TriMesh simplifiedMesh)
        {
            double maxO2S = 0;

            for (int i = 0; i < simplifiedMesh.Vertices.Count; i++)
            {
                double minLength = double.MaxValue;
                for (int j = 0; j < originMesh.Vertices.Count; j++)
                {
                    minLength = Math.Min(minLength, TriMeshMeasure.ComputeLength(simplifiedMesh.Vertices[i], originMesh.Vertices[j]));
                }
                maxO2S += minLength;
            }

            return maxO2S /= simplifiedMesh.Vertices.Count;
        }

        public static int _567ModelSimplify(TriMesh mesh, int keepFaceCount)
        {
            int originCount = mesh.Faces.Count;
            int prevCount = -1;
            do
            {
                int count = 0;
                for (int i = 0; i < mesh.Edges.Count; i++)
                {
                    if (mesh.Faces.Count > keepFaceCount)
                    {
                        if (EdgeCollapse(mesh.Edges[i]))
                        {
                            count++;
                        }
                    }
                    else
                    {
                        return originCount - keepFaceCount;
                    }
                }

                if (prevCount == 0 && count == 0)
                {
                    break;
                }

                if (count == 0)
                {
                    FlipOne(mesh);
                }

                prevCount = count;
            } while (true);

            return originCount - mesh.Faces.Count;
        }

        private static bool EdgeCanCollapse(TriMesh.Edge edge)
        {
            if (edge.Vertex0.OnBoundary || edge.Vertex1.OnBoundary)
            {
                return false;
            }

            int left = edge.Vertex1.HalfEdgeCount;
            int right = edge.Vertex0.HalfEdgeCount;
            int top = edge.HalfEdge0.Next.ToVertex.HalfEdgeCount;
            int bottom = edge.HalfEdge1.Next.ToVertex.HalfEdgeCount;

            return left + right < 12 && top > 5 && bottom > 5;
        }

        private static bool EdgeCollapse(TriMesh.Edge edge)
        {
            if (EdgeCanCollapse(edge) && TriMeshModify.EdgeCanMerge(edge))
            {
                TriMeshModify.MergeEdge(edge, TriMeshMeasure.GetMidPoint(edge), out var a);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static void FlipOne(TriMesh mesh)
        {
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                if (EdgeFlip(mesh.Edges[i]))
                {
                    break;
                }
            }
        }

        private static bool EdgeCanFlip(TriMesh.Edge edge)
        {
            if (edge.OnBoundary)
            {
                return false;
            }

            int left = edge.Vertex1.HalfEdgeCount;
            int right = edge.Vertex0.HalfEdgeCount;
            int top = edge.HalfEdge0.Next.ToVertex.HalfEdgeCount;
            int bottom = edge.HalfEdge1.Next.ToVertex.HalfEdgeCount;

            double dot = edge.Face0.Normal.Dot(edge.Face1.Normal);

            return left > 5 && right > 5 && top < 7 && bottom < 7 && dot > 0.95;
        }

        private static bool EdgeFlip(TriMesh.Edge edge)
        {
            if (EdgeCanFlip(edge))
            {
                TriMeshModify.EdgeSwap(edge);
                return true;
            }
            return false;
        }
    }
}
