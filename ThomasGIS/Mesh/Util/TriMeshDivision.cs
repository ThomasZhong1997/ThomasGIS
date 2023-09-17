using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Geometry3D;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Util
{
    // 三维模型细分类
    public static class TriMeshDivision
    {
        public static TriMesh LoopDivision(TriMesh mesh, bool changeShape = false)
        {
            TriMesh newMesh = new TriMesh();
            mesh.RefreshAllIndex();
            Dictionary<int, TriMesh.Vertex> vMap = new Dictionary<int, TriMesh.Vertex>();
            Dictionary<int, TriMesh.Vertex> eMap = new Dictionary<int, TriMesh.Vertex>();

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Vector3D vx = mesh.Vertices[i].Traits.Position;
                TriMesh.Vertex v = newMesh.Vertices.Add(new VertexTraits(vx.X, vx.Y, vx.Z));
                vMap[mesh.Vertices[i].Index] = v;
            }

            if (changeShape)
            {
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    Vector3D vertex = mesh.Vertices[i].Traits.Position;
                    Vector3D position = new Vector3D(0, 0, 0);
                    if (!mesh.Vertices[i].OnBoundary)
                    {
                        foreach (TriMesh.Vertex neighbor in mesh.Vertices[i].Vertices)
                        {
                            position += neighbor.Traits.Position;
                        }
                        int n = mesh.Vertices[i].VertexCount;
                        double beta = (1.0 / n) * (5.0 / 8.0 - Math.Pow((3.0 / 8.0 + 1.0 / 4.0 * Math.Cos(2 * Math.PI / n)), 2));
                        Vector3D targetPosition = (1 - n * beta) * vertex + beta * position;
                        newMesh.Vertices[i].Traits.Position = targetPosition;
                    }
                    else
                    {
                        int n = 0;
                        foreach (TriMesh.Vertex neighbor in mesh.Vertices[i].Vertices)
                        {
                            if (neighbor.OnBoundary)
                            {
                                position += neighbor.Traits.Position;
                                n++;
                            }
                        }
                        Vector3D targetPosition = 3.0 / 4.0 * vertex + 1.0 / 4.0 * position;
                        newMesh.Vertices[i].Traits.Position = targetPosition;
                    }
                }

                foreach (TriMesh.Edge edge in mesh.Edges)
                {
                    if (!edge.OnBoundary)
                    {
                        Vector3D v0 = edge.Vertex0.Traits.Position;
                        Vector3D v1 = edge.Vertex1.Traits.Position;
                        Vector3D v2 = edge.HalfEdge0.Next.ToVertex.Traits.Position;
                        Vector3D v3 = edge.HalfEdge1.Next.ToVertex.Traits.Position;
                        eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits((v0 + v1) * 3.0 / 8.0 + (v2 + v3) / 8.0));
                    }
                    else
                    {
                        eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(edge)));
                    }
                }
            }
            else
            {
                foreach (TriMesh.Edge edge in mesh.Edges)
                {
                    eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(edge)));
                }
            }

            foreach (TriMesh.Face face in mesh.Faces)
            {
                foreach (TriMesh.HalfEdge halfEdge in face.HalfEdges)
                {
                    newMesh.AddFace(eMap[halfEdge.Edge.Index], vMap[halfEdge.ToVertex.Index], eMap[halfEdge.Next.Edge.Index]);
                }
                newMesh.AddFace(eMap[face.HalfEdge.Previous.Edge.Index], eMap[face.HalfEdge.Edge.Index], eMap[face.HalfEdge.Next.Edge.Index]);
            }

            return newMesh;
        }

        public static TriMesh ModifiedButterflyDivision(TriMesh mesh, double tension = 0)
        {
            TriMesh newMesh = new TriMesh();
            mesh.RefreshAllIndex();
            Dictionary<int, TriMesh.Vertex> vMap = new Dictionary<int, TriMesh.Vertex>();
            Dictionary<int, TriMesh.Vertex> eMap = new Dictionary<int, TriMesh.Vertex>();

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Vector3D vx = mesh.Vertices[i].Traits.Position;
                TriMesh.Vertex v = newMesh.Vertices.Add(new VertexTraits(vx.X, vx.Y, vx.Z));
                vMap[mesh.Vertices[i].Index] = v;
            }

            foreach (TriMesh.Edge edge in mesh.Edges)
            {
                if (!edge.OnBoundary)
                {
                    List<TriMesh.HalfEdge> left = new List<TriMesh.HalfEdge>();
                    List<TriMesh.HalfEdge> right = new List<TriMesh.HalfEdge>();

                    TriMesh.HalfEdge hf0 = edge.HalfEdge0;
                    TriMesh.HalfEdge cur = hf0;
                    while (cur.Opposite.Next != hf0)
                    {
                        left.Add(cur);
                        cur = cur.Opposite.Next;
                    }

                    TriMesh.HalfEdge hf1 = edge.HalfEdge1;
                    cur = hf1;
                    while (cur.Opposite.Next != hf1)
                    {
                        right.Add(cur);
                        cur = cur.Opposite.Next;
                    }

                    if (left.Count == 6 && right.Count == 6)
                    {
                        double[] weight = new double[6];
                        weight[0] = (1.0 / 2.0) - (double)tension;
                        weight[1] = weight[5] = (1.0 / 8.0) + 2 * (double)tension;
                        weight[2] = weight[4] = -(1.0 / 16.0) - (double)tension;
                        weight[3] = (double)tension;

                        Vector3D sum = new Vector3D(0, 0, 0);
                        for (int i = 0; i < 6; i++)
                        {
                            sum += weight[i] * left[i].ToVertex.Traits.Position;
                            sum += weight[i] * right[i].ToVertex.Traits.Position;
                        }
                        double weightSum = weight[0] + weight[1] * 2 + weight[2] * 2 + weight[3];
                        sum /= weightSum;
                        sum /= 2;
                        eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits(sum));
                    }
                    else
                    {
                        Vector3D leftPosition = new Vector3D(0, 0, 0);
                        Vector3D rightPosition = new Vector3D(0, 0, 0);

                        if (left.Count >= 5)
                        {
                            leftPosition = ModifiedButterflyVertex5(left);
                        }
                        else if (left.Count == 4)
                        {
                            leftPosition = ModifiedButterflyVertex4(left);
                        }
                        else
                        {
                            leftPosition = ModifiedButterflyVertex3(left);
                        }

                        if (right.Count >= 5)
                        {
                            rightPosition = ModifiedButterflyVertex5(right);
                        }
                        else if (right.Count == 4)
                        {
                            rightPosition = ModifiedButterflyVertex4(right);
                        }
                        else
                        {
                            rightPosition = ModifiedButterflyVertex3(right);
                        }

                        eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits((leftPosition + rightPosition) / 2.0));
                    }
                }
                else
                {
                    List<TriMesh.HalfEdge> left = new List<TriMesh.HalfEdge>();
                    List<TriMesh.HalfEdge> right = new List<TriMesh.HalfEdge>();
                    TriMesh.HalfEdge hf0 = edge.HalfEdge0;
                    TriMesh.HalfEdge cur = hf0;
                    while (cur.Opposite.Next != hf0)
                    {
                        if (cur.OnBoundary)
                        {
                            left.Add(cur);
                        } 
                        cur = cur.Opposite.Next;
                    }

                    TriMesh.HalfEdge hf1 = edge.HalfEdge1;
                    cur = hf1;
                    while (cur.Opposite.Next != hf1)
                    {
                        if (cur.OnBoundary)
                        {
                            right.Add(cur);
                        }
                        cur = cur.Opposite.Next;
                    }

                    double[] weight = new double[] { 9.0 / 16.0, -1.0 / 16.0 };
                    Vector3D sum = new Vector3D(0, 0, 0);
                    for (int i = 0; i < 2; i++)
                    {
                        sum += weight[i] * left[i].ToVertex.Traits.Position;
                        sum += weight[i] * right[i].ToVertex.Traits.Position;
                    }
                    eMap[edge.Index] = newMesh.Vertices.Add(new VertexTraits(sum));
                }
            }

            foreach (TriMesh.Face face in mesh.Faces)
            {
                foreach (TriMesh.HalfEdge halfEdge in face.HalfEdges)
                {
                    newMesh.AddFace(eMap[halfEdge.Edge.Index], vMap[halfEdge.ToVertex.Index], eMap[halfEdge.Next.Edge.Index]);
                }
                newMesh.AddFace(eMap[face.HalfEdge.Previous.Edge.Index], eMap[face.HalfEdge.Edge.Index], eMap[face.HalfEdge.Next.Edge.Index]);
            }

            return newMesh;
        }

        private static Vector3D ModifiedButterflyVertex5(List<TriMesh.HalfEdge> hfs)
        {
            double[] weight = new double[hfs.Count + 1];
            weight[0] = 3.0 / 4.0;
            Vector3D sum = weight[0] * hfs[0].FromVertex.Traits.Position;
            double weightSum = weight[0];
            for (int i = 0; i < hfs.Count; i++)
            {
                weight[i + 1] = (1.0 / hfs.Count) * (1.0 / 4.0 + Math.Cos((2.0 * i * Math.PI) / hfs.Count) + (1.0 / 2.0) * (Math.Cos((4.0 * i * Math.PI) / hfs.Count)));
                sum += weight[i + 1] * hfs[i].ToVertex.Traits.Position;
                weightSum += weight[i + 1];
            }
            return sum /= weightSum;
        }

        private static Vector3D ModifiedButterflyVertex4(List<TriMesh.HalfEdge> hfs)
        {
            double[] weight = new double[5] { 3.0 / 4.0, 3.0 / 8.0, 0.0, -1.0 / 8.0, 0.0 };
            Vector3D sum = weight[0] * hfs[0].FromVertex.Traits.Position;
            for (int i = 0; i < 4; i++)
            {
                sum += weight[i + 1] * hfs[i].ToVertex.Traits.Position;
            }
            return sum;
        }

        private static Vector3D ModifiedButterflyVertex3(List<TriMesh.HalfEdge> hfs)
        {
            double[] weight = new double[4] { 3.0 / 4.0, 5.0 / 12.0, -1.0 / 12.0, -1.0 / 12.0 };
            Vector3D sum = weight[0] * hfs[0].FromVertex.Traits.Position;
            for (int i = 0; i < 3; i++)
            {
                sum += weight[i + 1] * hfs[i].ToVertex.Traits.Position;
            }
            return sum;
        }

        public static TriMesh Sqrt3Division(TriMesh mesh)
        {
            TriMesh newMesh = new TriMesh();
            mesh.RefreshAllIndex();
            Dictionary<int, TriMesh.Vertex> fMap = new Dictionary<int, TriMesh.Vertex>();
            Dictionary<int, TriMesh.Vertex> vMap = new Dictionary<int, TriMesh.Vertex>();
            foreach (TriMesh.Face face in mesh.Faces)
            {
                fMap[face.Index] = newMesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(face)));
            }

            foreach (TriMesh.Vertex v in mesh.Vertices)
            {
                vMap[v.Index] = newMesh.Vertices.Add(new VertexTraits(v.Traits.Position));
                foreach (TriMesh.HalfEdge hf in v.HalfEdges)
                {
                    if (hf.Face != null && hf.Opposite.Face != null)
                    {
                        newMesh.AddFace(fMap[hf.Face.Index], vMap[v.Index], fMap[hf.Opposite.Face.Index]);
                    }
                }
            }

            foreach (TriMesh.HalfEdge hf in mesh.HalfEdges)
            {
                if (hf.Face == null)
                {
                    newMesh.AddFace(vMap[hf.ToVertex.Index], vMap[hf.FromVertex.Index], fMap[hf.Opposite.Face.Index]);
                }
            }

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                int n = mesh.Vertices[i].VertexCount;
                Vector3D position = mesh.Vertices[i].Traits.Position;
                Vector3D neighborSum = new Vector3D(0, 0, 0);
                double an = (4.0 - 2 * Math.Cos(2.0 * Math.PI / n)) / 9.0;
                foreach (TriMesh.Vertex neighbor in mesh.Vertices[i].Vertices)
                {
                    neighborSum += neighbor.Traits.Position;
                }
                vMap[mesh.Vertices[i].Index].Traits.Position = (1.0 - an) * position + (an / n) * neighborSum;
            }

            return newMesh;
        }

        public static void FaceDivision(TriMesh mesh)
        {
            TriMesh copy = GeometryGenerator.Clone(mesh);
            mesh.Clear();

            TriMesh.Vertex[] faceMap = new TriMesh.Vertex[copy.Faces.Count];
            TriMesh.Vertex[] hfMap = new TriMesh.Vertex[copy.HalfEdges.Count];

            foreach (TriMesh.Vertex v in copy.Vertices)
            {
                mesh.Vertices.Add(new VertexTraits(v.Traits.Position));
            }

            foreach (TriMesh.Face face in copy.Faces)
            {
                faceMap[face.Index] = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(face)));
            }

            foreach (TriMesh.HalfEdge halfEdge in copy.HalfEdges)
            {
                Vector3D position = halfEdge.FromVertex.Traits.Position * 2.0 / 3.0 + halfEdge.ToVertex.Traits.Position / 3.0;
                hfMap[halfEdge.Index] = mesh.Vertices.Add(new VertexTraits(position));
            }

            foreach (TriMesh.Face face in copy.Faces)
            {
                foreach (TriMesh.HalfEdge hf in face.HalfEdges)
                {
                    mesh.AddFace(faceMap[face.Index], hfMap[hf.Index], hfMap[hf.Opposite.Index]);
                    mesh.AddFace(faceMap[face.Index], hfMap[hf.Opposite.Index], hfMap[hf.Next.Index]);
                }
            }

            foreach (TriMesh.Vertex v in copy.Vertices)
            {
                foreach (TriMesh.HalfEdge hf in v.HalfEdges)
                {
                    if (hf.Face != null)
                    {
                        mesh.AddFace(mesh.Vertices[v.Index], hfMap[hf.Index], hfMap[hf.Previous.Opposite.Index]);
                    }
                }
            }
        }
    }
}
