using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Helpers;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Vector;
using static ThomasGIS.Mesh.Basic.TriMesh;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshUtil
    {
        private static List<Color4> colorList = new List<Color4>();
        public static double ComputeEdgeAvgLength(TriMesh mesh)
        {
            double sum = 0;
            for (int i = 0; i < mesh.Edges.Count; i++)
            {
                Vector3D v1 = mesh.Edges[i].Vertex0.Traits.Position;
                Vector3D v2 = mesh.Edges[i].Vertex1.Traits.Position;
                sum += v1.DistanceWith(v2);
            }

            return sum / mesh.Edges.Count;
        }

        public static void AddNoise(TriMesh mesh, double threshold)
        {
            Random random = new Random();
            double avgLength = TriMeshUtil.ComputeEdgeAvgLength(mesh);
            threshold *= avgLength;
            foreach (TriMesh.Vertex item in mesh.Vertices)
            {
                Vector3D normal = item.Normal.Normalize();
                double scale = threshold * (random.NextDouble() - 0.5f);
                item.Traits.Position.X += normal.X * scale;
                item.Traits.Position.Y += normal.Y * scale;
                item.Traits.Position.Z += normal.Z * scale;
            }
        }

        public static Vector3D GetMaxCoord(TriMesh mesh)
        {
            Vector3D maxCoord = new Vector3D(double.MinValue, double.MinValue, double.MinValue);
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Vector3D v = mesh.Vertices[i].Traits.Position;
                maxCoord = Vector3D.Max(maxCoord, v);
            }
            return maxCoord;
        }

        public static Vector3D GetMinCoord(TriMesh mesh)
        {
            Vector3D minCoord = new Vector3D(double.MaxValue, double.MaxValue, double.MaxValue);
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                Vector3D v = mesh.Vertices[i].Traits.Position;
                minCoord = Vector3D.Min(minCoord, v);
            }
            return minCoord;
        }

        public static void ScaleToUnit(TriMesh mesh, double scale)
        {
            Vector3D max = new Vector3D(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3D min = new Vector3D(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

            foreach (TriMesh.Vertex v in mesh.Vertices)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (v.Traits.Position[i] > max[i])
                    {
                        max[i] = v.Traits.Position[i];
                    }

                    if (v.Traits.Position[i] < min[i])
                    {
                        min[i] = v.Traits.Position[i];
                    }
                }
            }

            Vector3D d = max - min;

            double s = (d.X > d.Y) ? d.X : d.Y;
            s = (s > d.Z) ? s : d.Z;

            if (s <= 0) return;

            foreach (TriMesh.Vertex vertex in mesh.Vertices)
            {
                vertex.Traits.Position /= s;
                vertex.Traits.Position *= scale;
            }
        }

        public static void MoveToCenter(TriMesh mesh)
        {
            Vector3D max = new Vector3D(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            Vector3D min = new Vector3D(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);

            foreach (TriMesh.Vertex v in mesh.Vertices)
            {
                for (int i = 0; i < 3; i++)
                {
                    if (v.Traits.Position[i] > max[i])
                    {
                        max[i] = v.Traits.Position[i];
                    }

                    if (v.Traits.Position[i] < min[i])
                    {
                        min[i] = v.Traits.Position[i];
                    }
                }
            }

            Vector3D center = (max + min) / 2.0;
            foreach (TriMesh.Vertex vertex in mesh.Vertices)
            {
                vertex.Traits.Position -= center;
            }
        }

        public static void GroupVertice(TriMesh mesh)
        {
            mesh.RefreshAllIndex();

            foreach (TriMesh.Vertex vertex in mesh.Vertices)
            {
                if (vertex.Traits.SelectedFlag != 0)
                {
                    vertex.Traits.SelectedFlag = 255;
                }
            }

            byte id = 0;
            Queue queue = new Queue();
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                if (mesh.Vertices[i].Traits.SelectedFlag == 255)
                {
                    id++;
                    mesh.Vertices[i].Traits.SelectedFlag = id;
                    queue.Enqueue(i);
                    while (queue.Count > 0)
                    {
                        int current = (int)queue.Dequeue();
                        foreach (TriMesh.Vertex neighbor in mesh.Vertices[current].Vertices)
                        {
                            if (mesh.Vertices[neighbor.Index].Traits.SelectedFlag == 255)
                            {
                                mesh.Vertices[neighbor.Index].Traits.SelectedFlag = id;
                                queue.Enqueue(neighbor.Index);
                            }
                        }
                    }
                }
            }
        }

        public static void InverseFace(TriMesh mesh)
        {
            List<TriMesh.Vertex[]> faces = new List<Vertex[]>();
            foreach (TriMesh.Face face in mesh.Faces)
            {
                TriMesh.HalfEdge hf = face.HalfEdge;
                TriMesh.Vertex[] arr = new TriMesh.Vertex[] { hf.Next.ToVertex, hf.ToVertex, hf.FromVertex };
                faces.Add(arr);
            }

            TriMesh.Vertex[] vertices = new TriMesh.Vertex[mesh.Vertices.Count];
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                vertices[i] = mesh.Vertices[i];
                vertices[i].HalfEdge = null;
            }
            mesh.Clear();

            foreach (var v in vertices)
            {
                mesh.AppendToVertexList(v);
            }

            foreach (var face in faces)
            {
                mesh.AddFace(face);
            }
        }

        public static Color4 SetRandomColor(int count)
        {
            if (count >= colorList.Count)
            {
                Random rd = new Random();
                int R = rd.Next(255);
                int G = rd.Next(255);
                int B = rd.Next(255);
                int A = rd.Next(255);
                Color4 color = new Color4(R, G, B, A);
                colorList.Add(color);
            }

            return colorList[count];
        }
    }
}
