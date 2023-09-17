using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ThomasGIS.Mesh.Basic;
using static ThomasGIS.Mesh.Basic.TriMesh;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshSearch
    {
        public static TriMesh.Vertex[] NeighborVertex(TriMesh.Vertex vertex)
        {
            return vertex.Vertices.ToArray();
        }

        public static TriMesh.Edge[] NeighborEdge(TriMesh.Vertex vertex)
        {
            List<TriMesh.Edge> result = new List<TriMesh.Edge>();
            foreach (TriMesh.HalfEdge halfEdge in vertex.HalfEdges)
            {
                result.Add(halfEdge.Next.Edge);
            }
            return result.ToArray();
        }

        public static TriMesh.Face[] NeighborFace(TriMesh.Vertex vertex)
        {
            return vertex.Faces.ToArray();
        }

        public static TriMesh.Vertex[] NeighborVertex(TriMesh.Edge edge)
        {
            HalfEdge[] oneRingHalfEdges = RetrieveOneRingHalfEdgeOfEdge(edge);
            List<Vertex> result = new List<Vertex>();
            for (int i = 0; i < oneRingHalfEdges.Length; i++)
            {
                result.Add(oneRingHalfEdges[i].ToVertex);
            }
            return result.ToArray();
        }

        public static TriMesh.Edge[] NeighborEdge(TriMesh.Edge edge)
        {
            HalfEdge[] oneRingHalfEdges = RetrieveOneRingHalfEdgeOfEdge(edge);
            List<Edge> result = new List<Edge>();
            for (int i = 0; i < oneRingHalfEdges.Length; i++)
            {
                result.Add(oneRingHalfEdges[i].Edge);
            }
            return result.ToArray();
        }

        public static TriMesh.Face[] NeighborFace(TriMesh.Edge edge)
        {
            List<TriMesh.Face> result = new List<TriMesh.Face>();
            TriMesh.Face face0 = edge.Face0;
            TriMesh.Face face1 = edge.Face1;
            result.Add(face0);
            result.Add(face1);

            Vertex v0 = edge.Vertex0;
            foreach (TriMesh.Face face in v0.Faces)
            {
                if (face != face0 && face != face1)
                {
                    result.Add(face);
                }
            }

            Vertex v1 = edge.Vertex1;
            foreach (TriMesh.Face face in v1.Faces)
            {
                if (face != face0 && face != face1)
                {
                    result.Add(face);
                }
            }

            return result.ToArray();
        }

        public static TriMesh.HalfEdge[] RetrieveOneRingHalfEdgeOfEdge(TriMesh.Edge edge)
        {
            List<TriMesh.HalfEdge> result = new List<TriMesh.HalfEdge>();
            result.AddRange(RetrieveOneRingHalfEdgeOfHalfEdge(edge.HalfEdge0));
            result.AddRange(RetrieveOneRingHalfEdgeOfHalfEdge(edge.HalfEdge1));
            return result.ToArray();
        }

        private static TriMesh.HalfEdge[] RetrieveOneRingHalfEdgeOfHalfEdge(TriMesh.HalfEdge halfEdge)
        {
            List<TriMesh.HalfEdge> result = new List<TriMesh.HalfEdge>();
            TriMesh.HalfEdge forbid1 = halfEdge.Previous;
            TriMesh.HalfEdge forbid2 = halfEdge.Opposite.Next;

            TriMesh.Vertex nextVertex = halfEdge.ToVertex;
            foreach (TriMesh.HalfEdge hf in nextVertex.HalfEdges)
            {
                TriMesh.HalfEdge selectedEdge = hf.Next;
                if (selectedEdge != forbid1 && selectedEdge != forbid2)
                {
                    result.Add(selectedEdge);
                }
            }

            return result.ToArray();
        }

        public static TriMesh.Face[] NeighborFace(TriMesh.Face face)
        {
            HashSet<Face> result = new HashSet<Face>();
            foreach (Vertex vertex in face.Vertices)
            {
                foreach (Face neighborFace in vertex.Faces)
                {
                    if (neighborFace != face)
                    {
                        result.Add(neighborFace);
                    }
                }
            }
            return result.ToArray();
        }

        public static TriMesh.Vertex[] NeighborVertex(TriMesh.Face face)
        {
            HashSet<TriMesh.Vertex> result = new HashSet<TriMesh.Vertex>();
            Face[] neighborFace = NeighborFace(face);
            for (int i = 0; i < neighborFace.Length; i++)
            {
                foreach (Vertex v in neighborFace[i].Vertices)
                {
                    result.Add(v);
                }
            }

            foreach (Vertex rv in face.Vertices)
            {
                result.Remove(rv);
            }

            return result.ToArray();
        }

        public static TriMesh.Edge[] NeighborEdge(TriMesh.Face face)
        {
            HashSet<TriMesh.Edge> result = new HashSet<TriMesh.Edge>();
            foreach (TriMesh.Vertex v1 in face.Vertices)
            {
                foreach (HalfEdge halfEdge in v1.HalfEdges)
                {
                    result.Add(halfEdge.Next.Edge);
                }
            }

            foreach (TriMesh.Edge e1 in face.Edges)
            {
                foreach (Edge re1 in e1.Face0.Edges)
                {
                    result.Remove(re1);
                }

                foreach (Edge re2 in e1.Face1.Edges)
                {
                    result.Remove(re2);
                }
            }

            return result.ToArray();
        }

        private static void RetrieveBoundaryHalfEdge(List<TriMesh.HalfEdge> boundary)
        {
            if (boundary.Count <= 0) return;

            HalfEdge startHf = boundary[0];
            HalfEdge currHf = startHf.Next;
            while (currHf != startHf)
            {
                boundary.Add(currHf);
                currHf = currHf.Next;
            }
        }

        public static List<List<TriMesh.HalfEdge>> RetrieveBoundaryHalfEdgeAll(TriMesh mesh)
        {
            List<List<TriMesh.HalfEdge>> allBoundary = new List<List<HalfEdge>>();
            foreach (TriMesh.HalfEdge edge in mesh.HalfEdges)
            {
                if (edge.OnBoundary)
                {
                    bool exist = false;
                    foreach (List<TriMesh.HalfEdge> existHole in allBoundary)
                    {
                        if (existHole.Contains(edge))
                        {
                            exist = true;
                        }
                    }

                    if (!exist)
                    {
                        List<TriMesh.HalfEdge> hole = new List<TriMesh.HalfEdge>();
                        hole.Add(edge);
                        RetrieveBoundaryHalfEdge(hole);
                        allBoundary.Add(hole);
                    }
                }
            }

            return allBoundary;
        }

        public static List<List<TriMesh.Vertex>> RetrieveBoundaryVertexAll(TriMesh mesh)
        {
            List<List<TriMesh.Vertex>> allBoundary = new List<List<Vertex>>();
            var holes = RetrieveBoundaryHalfEdgeAll(mesh);

            foreach (List<TriMesh.HalfEdge> holeHalfEdges in holes)
            {
                List<TriMesh.Vertex> oneHoleVertex = new List<Vertex>();
                foreach (TriMesh.HalfEdge hf in holeHalfEdges)
                {
                    oneHoleVertex.Add(hf.ToVertex);
                }
                allBoundary.Add(oneHoleVertex);
            }

            return allBoundary;
        }

        public static List<List<TriMesh.Face>> RetrieveFacePatchBySelectedEdge(TriMesh mesh)
        {
            mesh.RefreshAllIndex();

            bool[] faceFlag = new bool[mesh.Faces.Count];
            bool[] hfFlag = new bool[mesh.HalfEdges.Count];
            List<List<TriMesh.Face>> all = new List<List<Face>>();

            foreach (TriMesh.HalfEdge hf in mesh.HalfEdges)
            {
                if (!hfFlag[hf.Index])
                {
                    List<TriMesh.Face> list = new List<TriMesh.Face>();
                    Stack<TriMesh.HalfEdge> stack = new Stack<HalfEdge>();
                    stack.Push(hf);

                    while (stack.Count != 0)
                    {
                        TriMesh.HalfEdge cur = stack.Pop();
                        hfFlag[cur.Index] = true;

                        if (!faceFlag[cur.Face.Index])
                        {
                            list.Add(cur.Face);
                            faceFlag[cur.Face.Index] = true;
                        }

                        TriMesh.HalfEdge[] arr = new TriMesh.HalfEdge[]
                        {
                            cur.Opposite,
                            cur.Next,
                            cur.Previous
                        };

                        foreach (var item in arr)
                        {
                            if (!hfFlag[item.Index] && item.Face != null && item.Edge.Traits.SelectedFlag == 0)
                            {
                                stack.Push(item);
                            }
                        }
                    }

                    if (list.Count != 0)
                    {
                        all.Add(list);
                    }
                }
            }

            return all;
        }
    }
}
