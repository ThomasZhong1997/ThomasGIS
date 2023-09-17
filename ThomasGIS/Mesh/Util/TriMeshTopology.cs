using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshTopology
    {
        public static int CountComponent(TriMesh mesh, bool color)
        {
            mesh.RefreshAllIndex();

            int count = 0;
            bool[] visited = new bool[mesh.Faces.Count];
            Queue<TriMesh.Face> queue = new Queue<TriMesh.Face>();
            queue.Enqueue(mesh.Faces[0]);
            visited[0] = true;

            while (queue.Count != 0)
            {
                TriMesh.Face face = queue.Dequeue();
                if (color)
                {
                    face.Traits.SelectedFlag = 1;
                    face.Traits.Color = TriMeshUtil.SetRandomColor(count);
                }

                foreach (TriMesh.HalfEdge hf in face.HalfEdges)
                {
                    if (hf.Opposite.Face != null && !visited[hf.Opposite.Face.Index])
                    {
                        queue.Enqueue(hf.Opposite.Face);
                        visited[hf.Opposite.Face.Index] = true;
                    }
                }

                if (queue.Count == 0)
                {
                    count++;
                    for (int i = 0; i < visited.Length; i++)
                    {
                        if (!visited[i])
                        {
                            queue.Enqueue(mesh.Faces[i]);
                            visited[i] = true;
                            break;
                        }
                    }
                }
            }

            return count;
        }

        public static int EulerCharacteristicCount(TriMesh mesh)
        {
            return mesh.Vertices.Count - mesh.Edges.Count + mesh.Faces.Count;
        }

        public static int CountBoundary(TriMesh mesh)
        {
            return TriMeshSearch.RetrieveBoundaryHalfEdgeAll(mesh).Count;
        }

        public static int CountGenus(TriMesh mesh)
        {
            int euler = EulerCharacteristicCount(mesh);
            int b = CountBoundary(mesh);
            int c = CountComponent(mesh, false);
            return c - (euler + b) / 2;
        }
    }
}
