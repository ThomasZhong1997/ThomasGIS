using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThomasGIS.Mesh.Vector;
using System.Linq;
using ThomasGIS.Mesh.Util;

namespace ThomasGIS.Mesh.Basic
{
    public class TriMesh
    {
        // 半边
        private List<HalfEdge> halfEdges;
        // 边
        private EdgeCollection edges;
        // 面
        private FaceCollection faces;
        // 顶点
        private VertexCollection vertices;

        public TriMesh()
        {
            halfEdges = new List<HalfEdge>();
            edges = new EdgeCollection(this);
            faces = new FaceCollection(this);
            vertices = new VertexCollection(this);
        }

        public FaceCollection Faces
        {
            get { return faces; }
        }

        public VertexCollection Vertices
        {
            get { return vertices; }
        }

        public EdgeCollection Edges
        {
            get { return edges; }
        }

        public List<HalfEdge> HalfEdges => halfEdges;

        public Face AddFace(params Vertex[] faceVertices)
        {
            int n = faceVertices.Length;

            if (n < 3) throw new ArgumentException("Face don't have enough vertices!");

            TriMesh.HalfEdge[] faceHalfedges = new TriMesh.HalfEdge[n];
            bool[] isUsedVertex = new bool[n];

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                faceHalfedges[i] = this.Validate(faceVertices[i], faceVertices[j]);
                isUsedVertex[i] = (faceVertices[i].HalfEdge != null);
            }

            TriMesh.Face f = new TriMesh.Face();
            this.AppendToFaceList(f);

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                if (faceHalfedges[i] == null)
                {
                    TriMesh.Edge newEdge = this.CreateNewEdge(faceVertices[i], faceVertices[j]);
                    faceHalfedges[i] = newEdge.HalfEdge0;
                }
                faceHalfedges[i].Face = f;
            }

            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                this.ConnectHalfedge(faceHalfedges[i], faceHalfedges[j], isUsedVertex[j]);
            }

            f.HalfEdge = faceHalfedges[0];
            return f;
        }

        public void RemoveEdge(Edge edge)
        {
            if (edge.HalfEdge0.Next == edge.HalfEdge1)
            {
                edge.HalfEdge0.Previous.Next = edge.HalfEdge1.Next;
                edge.HalfEdge1.Next.Previous = edge.HalfEdge0.Previous;
                edge.Vertex1.HalfEdge = edge.HalfEdge0.Previous;
                edge.Vertex0.HalfEdge = null;
            }
            else if (edge.HalfEdge1.Next == edge.HalfEdge0)
            {
                edge.HalfEdge1.Previous.Next = edge.HalfEdge0.Next;
                edge.HalfEdge0.Next.Previous = edge.HalfEdge1.Previous;
                edge.Vertex0.HalfEdge = edge.HalfEdge1.Previous;
                edge.Vertex1.HalfEdge = null;
            }
            else
            {
                if (edge.Face0 != null)
                {
                    this.RemoveFace(edge.Face0);
                }

                if (edge.Face1 != null)
                {
                    this.RemoveFace(edge.Face1);
                }

                edge.HalfEdge0.Next.Previous = edge.HalfEdge1.Previous;
                edge.HalfEdge0.Previous.Next = edge.HalfEdge1.Next;
                edge.HalfEdge1.Next.Previous = edge.HalfEdge0.Previous;
                edge.HalfEdge1.Previous.Next = edge.HalfEdge0.Next;
                edge.HalfEdge0.ToVertex.HalfEdge = edge.HalfEdge0.Next;
                edge.HalfEdge1.ToVertex.HalfEdge = edge.HalfEdge1.Next;
                edge.HalfEdge0.Next.Face = null;
                edge.HalfEdge0.Previous.Face = null;
                edge.HalfEdge1.Next.Face = null;
                edge.HalfEdge1.Previous.Face = null;
            }

            this.halfEdges.Remove(edge.HalfEdge1);
            this.halfEdges.Remove(edge.HalfEdge0);
            this.edges.Remove(edge);
        }

        public void RemoveEdge(int index)
        {
            TriMesh.Edge deletedEdge = this.edges[index];
            RemoveEdge(deletedEdge);
        }

        public void RemoveFace(Face face)
        {
            foreach (TriMesh.HalfEdge halfedge in face.HalfEdges)
            {
                halfedge.Face = null;
            }
            this.Faces.Remove(face);
        }

        public void RemoveFace(int index)
        {
            TriMesh.Face deletedFace = this.Faces[index];
            RemoveFace(deletedFace);
        }

        public void RemoveVertex(Vertex vertex)
        {
            foreach (HalfEdge halfEdge in vertex.HalfEdges)
            {
                if (halfEdge.Next != null)
                {
                    halfEdge.Next.Face = null;
                    halfEdge.Opposite.Previous.Next = halfEdge.Next;
                    halfEdge.Next.Previous = halfEdge.Opposite.Previous;
                }

                if (halfEdge.ToVertex != null && halfEdge.ToVertex.HalfEdge == halfEdge.Opposite)
                {
                    halfEdge.ToVertex.HalfEdge = halfEdge.Next;
                }

                this.halfEdges.Remove(halfEdge);
                this.halfEdges.Remove(halfEdge.Opposite);
            }

            foreach (Face face in vertex.Faces)
            {
                RemoveFace(face);
            }

            foreach (Edge edge in vertex.Edges)
            {
                RemoveEdge(edge);
            }

            this.vertices.Remove(vertex);

            RefreshAllIndex();
        }

        public void RemoveVertex(int index)
        {
            Vertex deletedVertex = this.vertices[index];
            RemoveVertex(deletedVertex);
        }

        // 寻找需要分割的顶点v在两个共享点begin和end之间的半边
 
        public void OuterHalfedgeBothOld(HalfEdge cur, HalfEdge next)
        {
            if (cur.Next == next) return;

            TriMesh.HalfEdge closeHalfedge = cur.Opposite;
            do
            {
                closeHalfedge = closeHalfedge.Previous.Opposite;
            } while (closeHalfedge.Face != null && closeHalfedge != next && closeHalfedge != cur.Opposite);

            if (closeHalfedge == next || closeHalfedge == cur.Opposite)
            {
                throw new Exception("Unable to find an opening to relink an existing face.");
            }

            TriMesh.HalfEdge openHalfedge = closeHalfedge.Previous;

            openHalfedge.Next = cur.Next;
            cur.Next.Previous = openHalfedge;
            next.Previous.Next = closeHalfedge;
            closeHalfedge.Previous = next.Previous;
        }

        public void OuterHalfedgeBothNew(HalfEdge cur, HalfEdge next, bool vertexIsUsed)
        {
            // vertex已经被使用则需要进行特殊处理
            if (vertexIsUsed)
            {
                TriMesh.Vertex vertex = cur.ToVertex;
                TriMesh.HalfEdge closeHalfedge = null;

                // 遍历从V出发的在添加面之前就已经存在半边，获得第一条在边界上的半边close
                foreach (TriMesh.HalfEdge h in vertex.HalfEdges)
                {
                    if (h == null) break;
                    if (h.Face == null)
                    {
                        closeHalfedge = h;
                        break;
                    }
                }

                
                // 半边close的上一条半边为open，添加的面在open和close之间
                TriMesh.HalfEdge openHalfedge = closeHalfedge.Previous;

                // 连接半边open到cur的反向边，连接next的反向半边到半边close
                cur.Opposite.Previous = openHalfedge;
                openHalfedge.Next = cur.Opposite;
                next.Opposite.Next = closeHalfedge;
                closeHalfedge.Previous = next.Opposite;

                return;
            }

            // 连接cur的反向半边到next的反向半边
            cur.Opposite.Previous = next.Opposite;
            next.Opposite.Next = cur.Opposite;

            return;
        }

        public void ConnectHalfedge(HalfEdge cur, HalfEdge next, bool vertexIsUsed)
        {
            bool curIsNew = cur.Next == null;
            bool nextIsNew = next.Previous == null;

            if (curIsNew && nextIsNew)
            {
                this.OuterHalfedgeBothNew(cur, next, vertexIsUsed);
            }
            // 仅cur是新添加的，则连接next的前一条半边到cur的反向半边
            else if (curIsNew && !nextIsNew)
            {
                cur.Opposite.Previous = next.Previous;
                next.Previous.Next = cur.Opposite;
            }
            // next是新添加的，则连接next的反向半边到cur的下一条半边
            else if (!curIsNew && nextIsNew)
            {
                cur.Next.Previous = next.Opposite;
                next.Opposite.Next = cur.Next;
            }
            else
            {
                this.OuterHalfedgeBothOld(cur, next);
            }

            cur.Next = next;
            next.Previous = cur;
        }

        public Edge CreateNewEdge(Vertex sourceVertex, Vertex targetVertex)
        {
            TriMesh.Edge edge = new TriMesh.Edge(default(EdgeTraits));
            this.AppendToEdgeList(edge);
            // 添加一条边和两条半边
            TriMesh.HalfEdge hf0 = new TriMesh.HalfEdge(default(HalfedgeTraits));
            this.AppendToHalfedgeList(hf0);
            hf0.Opposite = new TriMesh.HalfEdge(default(HalfedgeTraits));
            this.AppendToHalfedgeList(hf0.Opposite);

            // 设置两条半边之间的关系
            hf0.Opposite.Opposite = hf0;

            // 设置边与半边的关系
            edge.HalfEdge0 = hf0;
            hf0.Edge = edge;
            hf0.Opposite.Edge = edge;

            // 设置半边与顶点的关系
            hf0.ToVertex = targetVertex;
            hf0.Opposite.ToVertex = sourceVertex;
            if (sourceVertex.HalfEdge == null)
            {
                sourceVertex.HalfEdge = hf0;
            }

            return edge;
        }

        public HalfEdge Validate(Vertex sourceVertex, Vertex targetVertex)
        {
            // 如果sourceVertex为空，抛出异常
            if (sourceVertex == null) throw new Exception("source vertex is empty!");

            // 如果sourceVertex不在边界上，抛出异常
            if (!sourceVertex.OnBoundary) throw new Exception("source vertex not on boundary!");

            // 获取从sourceVertex到targetVertex的halfedge
            TriMesh.HalfEdge hf = null;
            foreach (HalfEdge oneHf in sourceVertex.HalfEdges)
            {
                if (oneHf == null) break;
                if (oneHf.ToVertex == targetVertex)
                {
                    hf = oneHf;
                }
            }

            // 如果hf不为null且不在边界上抛出异常
            if (hf != null && !hf.OnBoundary) throw new Exception("error!");

            return hf;
        }

        public void AppendToHalfedgeList(HalfEdge halfEdge)
        {
            halfEdge.Mesh = this;
            this.halfEdges.Add(halfEdge);
        }

        public void AppendToFaceList(Face face)
        {
            face.Mesh = this;
            face.Index = this.faces.Count;
            this.faces.Add(face);
        }

        public void AppendToEdgeList(Edge edge)
        {
            edge.Mesh = this;
            this.edges.Add(edge);
        }

        public void AppendToVertexList(Vertex vertex)
        {
            vertex.Mesh = this;
            vertex.Index = this.vertices.Count;
            this.vertices.Add(vertex);
        }

        public void RefreshAllIndex()
        {
            for (int i = 0; i < vertices.Count; i++)
            {
                vertices[i].Index = i;
            }

            for (int i = 0; i < faces.Count; i++)
            {
                faces[i].Index = i;
            }

            for (int i = 0; i < edges.Count; i++)
            {
                edges[i].Index = i;
            }

            for (int i = 0; i < halfEdges.Count; i++)
            {
                halfEdges[i].Index = i;
            }
        }

        public void Clear()
        {
            this.faces.Clear();
            this.edges.Clear();
            this.vertices.Clear();
            this.halfEdges.Clear();
        }

        public class Vertex
        {
            public VertexTraits Traits;
            private HalfEdge halfEdge;
            private TriMesh mesh;
            private int index;

            public Vector3D Normal
            {
                get
                {
                    if (this.Traits.Normal == null)
                    {
                        return ComputeUniformWeightNormal();
                    }
                    return this.Traits.Normal;
                }
            }

            public int Index
            {
                get { return index; }
                set { index = value; }
            }

            public Vertex(VertexTraits vertexTraits)
            {
                this.Traits = vertexTraits;
            }

            public Vertex(int id, VertexTraits vertexTraits)
            {
                this.index = id;
                this.Traits = vertexTraits;
            }

            public IEnumerable<Vector3D> VertexPositionV3d
            {
                get
                {
                    foreach (TriMesh.HalfEdge hf in HalfEdges)
                    {
                        yield return hf.ToVertex.Traits.Position;
                    }
                }
            }

            public IEnumerable<Vector3D> VertexNormalV3d
            {
                get
                {
                    foreach (TriMesh.HalfEdge hf in HalfEdges)
                    {
                        yield return hf.ToVertex.Normal;
                    }
                }
            }

            public TriMesh Mesh
            {
                get { return mesh; }
                set { mesh = value; }
            }

            public IEnumerable<Edge> Edges
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        yield return h.Edge;
                    }
                }
            }

            public IEnumerable<HalfEdge> HalfEdges
            {
                get
                {
                    HalfEdge h = this.halfEdge;
                    if (h != null)
                    {
                        do
                        {
                            yield return h;
                            h = h.Opposite.Next;
                        } while (h != this.halfEdge);
                    }
                }
            }

            public IEnumerable<Face> Faces
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        if (h.Face != null)
                        {
                            yield return h.Face;
                        }  
                    }
                }
            }

            public IEnumerable<Vertex> Vertices
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        yield return h.ToVertex;
                    }
                }
            }

            public bool OnBoundary
            {
                get
                {
                    if (this.halfEdge == null) return true;

                    foreach (HalfEdge h in HalfEdges)
                    {
                        if (h.OnBoundary)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            public int VertexCount
            {
                get
                {
                    int count = 0;
                    foreach (Vertex v in Vertices)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public int EdgeCount
            {
                get
                {
                    int count = 0;
                    foreach (Edge e in Edges)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public int FaceCount
            {
                get
                {
                    int count = 0;
                    foreach (Face f in Faces)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public int HalfEdgeCount
            {
                get
                {
                    int count = 0;
                    foreach (HalfEdge hf in HalfEdges)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public HalfEdge HalfEdge
            {
                get { return this.halfEdge; }
                set { this.halfEdge = value; }
            }

            public HalfEdge FindHalfEdgeTo(Vertex v)
            {
                foreach (HalfEdge halfEdge in HalfEdges)
                {
                    if (halfEdge.ToVertex == v)
                    {
                        return halfEdge;
                    }
                }

                return null;
            }

            public Vector3D ComputeUniformWeightNormal()
            {
                double xs = 0;
                double ys = 0;
                double zs = 0;
                foreach (Face face in this.Faces)
                {
                    Vector3D faceNormal = face.Normal;
                    xs += faceNormal.X;
                    ys += faceNormal.Y;
                    zs += faceNormal.Z;
                }
                this.Traits.Normal = (new Vector3D(xs, ys, zs)).Normalize();
                return this.Traits.Normal;
            }

            public Vector3D ComputeAreaWeightNormal()
            {
                this.Traits.Normal = new Vector3D(0, 0, 0);
                foreach (Face face in this.Faces)
                {
                    this.Traits.Normal += TriMeshMeasure.ComputeFaceArea(face) * face.Normal;
                }
                this.Traits.Normal.Normalize();
                return this.Traits.Normal;
            }

            public Vector3D ComputeTipAngleWeightNormal()
            {
                this.Traits.Normal = new Vector3D(0, 0, 0);
                foreach (HalfEdge hf in this.HalfEdges)
                {
                    double angle = TriMeshMeasure.ComputeAngle(hf.Next);
                    this.Traits.Normal += angle * hf.Face.Normal;
                }
                this.Traits.Normal.Normalize();
                return this.Traits.Normal;
            }

            public Vector3D ComputeSphereInscribedNormal()
            {
                this.Traits.Normal = new Vector3D(0, 0, 0);
                Vector3D[] vec = this.VertexPositionV3d.ToArray();
                double[] len = new double[vec.Length];

                for (int i = 0; i < vec.Length; i++)
                {
                    len[i] = vec[i].Length();
                }

                for (int i = 0; i < vec.Length; i++)
                {
                    int j = (i + 1) % vec.Length;
                    Vector3D e1 = vec[i];
                    Vector3D e2 = -vec[j];
                    this.Traits.Normal += e1.Cross(e2) / (len[i] * len[i] * len[j] * len[j]);
                }
                this.Traits.Normal.Normalize();
                return this.Traits.Normal;
            }
        }

        public class Face
        {
            public TriMesh mesh;

            public FaceTraits Traits = new FaceTraits();

            private HalfEdge halfEdge;

            public int Index;

            public Face()
            {
                Traits = new FaceTraits();
            }

            public Face(FaceTraits faceTraits)
            {
                if (faceTraits == null)
                {
                    this.Traits = new FaceTraits();
                }
                else
                {
                    Traits = faceTraits;
                }
            }

            public TriMesh Mesh
            {
                get { return mesh; }
                set { mesh = value; }
            }

            public IEnumerable<Edge> Edges
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        yield return h.Edge;
                    }
                }
            }

            public IEnumerable<HalfEdge> HalfEdges
            {
                get
                {
                    HalfEdge h = this.halfEdge;
                    do
                    {
                        yield return h;
                        h = h.Next;
                    } while (h != this.halfEdge);
                }
            }

            public IEnumerable<Face> Faces
            {
                get
                {
                    // 返回的是相邻面，妙啊，半边数据结构妙啊
                    foreach (HalfEdge h in HalfEdges)
                    {
                        if (h.Opposite.Face != null)
                            yield return h.Opposite.Face;
                    }
                }
            }

            public IEnumerable<Vertex> Vertices
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        yield return h.ToVertex;
                    }
                }
            }

            public bool OnBoundary
            {
                get
                {
                    foreach (HalfEdge h in HalfEdges)
                    {
                        if (h.Opposite.OnBoundary)
                        {
                            return true;
                        }
                    }
                    return false;
                }
            }

            public int EdgeCount
            {
                get
                {
                    int count = 0;
                    foreach (Edge e in Edges)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public int FaceCount
            {
                get
                {
                    int count = 0;
                    foreach (Face f in Faces)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public HalfEdge HalfEdge
            {
                get { return this.halfEdge; }
                set { this.halfEdge = value; }
            }

            public int HalfedgeCount
            {
                get
                {
                    int count = 0;
                    foreach (HalfEdge h in HalfEdges)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public int VertexCount
            {
                get
                {
                    int count = 0;
                    foreach (Vertex v in Vertices)
                    {
                        ++count;
                    }
                    return count;
                }
            }

            public Vector3D Normal
            {
                get 
                {
                    Vector3D v1 = GetVertex(0).Traits.Position;
                    Vector3D v2 = GetVertex(1).Traits.Position;
                    Vector3D v3 = GetVertex(2).Traits.Position;

                    this.Traits.Normal = (v2 - v1).Cross(v3 - v1);

                    return this.Traits.Normal;
                }
            }

            public Vertex GetVertex(int index)
            {
                return this.Vertices.ToList()[index];
            }
        }

        public class HalfEdge
        {
            public HalfedgeTraits Traits { get; set; } = new HalfedgeTraits();

            private Edge edge;

            private Face face;

            private HalfEdge nextHalfEdge;
            private HalfEdge oppositeHalfEdge;
            private HalfEdge previousHalfEdge;

            private Vertex vertex;

            private TriMesh mesh;

            public int Index = 0;

            public HalfEdge(HalfedgeTraits halfedgeTraits)
            {
                this.Traits = halfedgeTraits;
            }

            public bool OnBoundary
            {
                get
                {
                    return face == null;
                }
            }

            public TriMesh Mesh
            {
                get { return mesh; }
                set { mesh = value; }
            }

            public HalfEdge Opposite
            {
                get { return oppositeHalfEdge; }
                set { oppositeHalfEdge = value; }
            }

            public Vertex FromVertex
            {
                get { return Opposite.ToVertex; }
            }

            public Vertex ToVertex
            {
                get { return vertex; }
                set { vertex = value; }
            }

            public Edge Edge
            {
                get { return edge; }
                set { edge = value; }
            }

            public Face Face
            {
                get { return face; }
                set { face = value; }
            }

            public HalfEdge Next
            {
                get { return nextHalfEdge; }
                set { nextHalfEdge = value; }
            }

            public HalfEdge Previous
            {
                get { return previousHalfEdge; }
                set { previousHalfEdge = value; }
            }
        }

        public class Edge
        {
            public EdgeTraits Traits = new EdgeTraits();
            private HalfEdge halfEdge;
            public int Index = 0;
            private TriMesh mesh;

            public Edge(EdgeTraits edgeTraits)
            {
                this.Traits = edgeTraits;
            }

            public TriMesh Mesh
            {
                get { return mesh; }
                set { mesh = value; }
            }

            public Face Face0
            {
                get
                {
                    return halfEdge.Face;
                }
            }

            public Face Face1
            {
                get
                {
                    return halfEdge.Opposite.Face;
                }
            }

            public HalfEdge HalfEdge0
            {
                get { return halfEdge; }
                set { halfEdge = value; }
            }

            public HalfEdge HalfEdge1
            {
                get { return halfEdge.Opposite; }
            }

            public Vertex Vertex0
            {
                get { return halfEdge.ToVertex; }
            }

            public Vertex Vertex1
            {
                get { return halfEdge.Opposite.ToVertex; }
            }

            public bool OnBoundary
            {
                get { return halfEdge.OnBoundary || halfEdge.Opposite.OnBoundary; }
            }
        }

        public class FaceCollection : List<Face>
        {
            private TriMesh mesh;
            public FaceCollection(TriMesh mesh)
            {
                this.mesh = mesh;
            }
        }

        public class VertexCollection : List<Vertex>
        {
            private TriMesh mesh;
            public VertexCollection(TriMesh mesh)
            {
                this.mesh = mesh;
            }

            public Vertex Add(VertexTraits vertexTraits)
            {
                Vertex newVertex = new Vertex(this.Count, vertexTraits);
                newVertex.Mesh = mesh;
                base.Add(newVertex);
                return newVertex;
            }

            public new bool Add(TriMesh.Vertex v)
            {
                v.Mesh = mesh;
                base.Add(v);
                return true;
            }
        }

        public class EdgeCollection : List<Edge>
        {
            private TriMesh mesh;
            public EdgeCollection(TriMesh mesh)
            {
                this.mesh = mesh;
            }
        }
    }
}
