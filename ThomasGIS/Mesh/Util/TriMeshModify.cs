using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Geometry3D;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshModify
    {
        public static bool EdgeCanMerge(TriMesh.Edge edge)
        {
            // 第一种非流形判断，一个edge只能对应两个面
            TriMesh.Vertex top = edge.HalfEdge0.Next.ToVertex;
            TriMesh.Vertex bottom = edge.HalfEdge1.Next.ToVertex;

            foreach (TriMesh.Vertex vertex1 in edge.Vertex0.Vertices)
            {
                foreach (TriMesh.Vertex vertex2 in edge.Vertex1.Vertices)
                {
                    if (vertex1 == vertex2 && vertex1 != top && vertex1 != bottom)
                    {
                        return false;
                    }
                }
            }

            // 第二种非流形判断，两个顶点都在边界上
            bool v0OnBoundary = false;
            bool v1OnBoundary = false;
            foreach (TriMesh.HalfEdge halfEdge in edge.Vertex0.HalfEdges)
            {
                if (halfEdge.OnBoundary && halfEdge != edge.HalfEdge1 && halfEdge != edge.HalfEdge0.Next)
                {
                    v0OnBoundary = true;
                }
            }

            foreach (TriMesh.HalfEdge halfEdge in edge.Vertex1.HalfEdges)
            {
                if (halfEdge.OnBoundary && halfEdge != edge.HalfEdge0 && halfEdge != edge.HalfEdge1.Next)
                {
                    v1OnBoundary = true;
                }
            }

            if (v0OnBoundary && v1OnBoundary) return false;

            // 
            if (edge.HalfEdge0.Next.Next.ToVertex == edge.Vertex1 && edge.HalfEdge0.Face == null)
            {
                return false;
            }

            if (edge.HalfEdge1.Next.Next.ToVertex == edge.Vertex0 && edge.HalfEdge1.Face == null)
            {
                return false;
            }

            List<TriMesh.HalfEdge> halfedgeSet = new List<TriMesh.HalfEdge>();
            foreach (var item in edge.Vertex0.HalfEdges)
            {
                halfedgeSet.Add(item.Next.Opposite);
            }

            if (halfedgeSet.Count < 3)
            {
                return false;
            }

            if (halfedgeSet[0].Next == halfedgeSet[1] && halfedgeSet[1].Next == halfedgeSet[2] && halfedgeSet[2].Next == halfedgeSet[0])
            {
                return false;
            }

            return true;
        }

        private static TriMesh.Edge MergeOneSide(TriMesh.HalfEdge halfEdge)
        {
            TriMesh mesh = halfEdge.Mesh;
            TriMesh.Edge removeEdge = null;

            if (halfEdge.OnBoundary)
            {
                halfEdge.ToVertex.HalfEdge = halfEdge.Next;
                halfEdge.Previous.Next = halfEdge.Next;
                halfEdge.Next.Previous = halfEdge.Previous;
            }
            else
            {
                TriMesh.Edge remain = halfEdge.Next.Edge;
                TriMesh.Edge remove = halfEdge.Previous.Edge;
                TriMesh.HalfEdge outerLeft = halfEdge.Previous.Opposite;
                TriMesh.HalfEdge outerRight = halfEdge.Next.Opposite;
                //if (outerLeft.Next == outerRight || outerLeft.Previous == outerRight)
                //{
                //    outerLeft.Edge.Traits.SelectedFlag = 1;
                //    halfEdge.FromVertex.Traits.SelectedFlag = 1;
                //    outerLeft.ToVertex.Traits.SelectedFlag = 1;
                //}

                remain.HalfEdge0 = outerRight;
                outerLeft.Opposite = outerRight;
                outerRight.Opposite = outerLeft;
                outerLeft.Edge = remain;
                outerRight.Edge = remain;

                halfEdge.ToVertex.HalfEdge = outerLeft; 

                TriMesh.Vertex top = halfEdge.Next.ToVertex;
                top.HalfEdge = outerRight;

                mesh.Faces.Remove(halfEdge.Face);
                mesh.HalfEdges.Remove(halfEdge.Previous);
                mesh.HalfEdges.Remove(halfEdge.Next);
                mesh.Edges.Remove(remove);

                halfEdge.Previous.Previous = null;
                halfEdge.Previous.Next = null;
                halfEdge.Previous.Opposite = null;
                halfEdge.Next.Previous = null;
                halfEdge.Next.Next = null;
                halfEdge.Next.Opposite = null;
                remove.HalfEdge0 = null;
                removeEdge = remove;
            }

            mesh.HalfEdges.Remove(halfEdge);
            halfEdge.ToVertex = null;
            halfEdge.Next = null;
            halfEdge.Previous = null;
            halfEdge.Opposite = null;
            halfEdge.Face = null;
            return removeEdge;
        }

        public static TriMesh.Vertex MergeEdge(TriMesh.Edge edge, Vector3D newPosition, out List<TriMesh.Edge> deletedEdgeList)
        {
            if (!EdgeCanMerge(edge)) throw new Exception("Input edge can not be merged!");

            deletedEdgeList = new List<TriMesh.Edge>();
            TriMesh mesh = edge.Mesh;

            TriMesh.Vertex v0 = edge.Vertex0;
            TriMesh.Vertex v1 = edge.Vertex1;

            TriMesh.HalfEdge hf0 = edge.HalfEdge0;
            TriMesh.HalfEdge hf1 = edge.HalfEdge1;

            v0.Traits.Position = newPosition;

            foreach (TriMesh.HalfEdge halfEdge in v1.HalfEdges)
            {
                halfEdge.Opposite.ToVertex = v0;
            }

            TriMesh.Edge removeEdge1 = MergeOneSide(hf0);
            TriMesh.Edge removeEdge2 = MergeOneSide(hf1);

            if (removeEdge1 != null) deletedEdgeList.Add(removeEdge1);
            if (removeEdge2 != null) deletedEdgeList.Add(removeEdge2);

            // RemoveVertex(v1);
            mesh.Vertices.Remove(v1);
            // RemoveEdge(edge);
            mesh.Edges.Remove(edge);
            v1.HalfEdge = null;
            edge.HalfEdge0 = null;
            return v0;
        }

        public static bool EdgeSwap(TriMesh.Edge edge)
        {
            if (edge.OnBoundary) return false;

            TriMesh.HalfEdge hf1 = edge.HalfEdge0;
            TriMesh.HalfEdge hf2 = edge.HalfEdge1;
            TriMesh.Vertex top = hf1.ToVertex;
            TriMesh.Vertex bottom = hf2.ToVertex;

            TriMesh.HalfEdge topLeft = hf1.Next;
            TriMesh.HalfEdge bottomLeft = hf1.Previous;
            TriMesh.HalfEdge topRight = hf2.Previous;
            TriMesh.HalfEdge bottomRight = hf2.Next;

            top.HalfEdge = topLeft;
            bottom.HalfEdge = bottomRight;
            hf1.ToVertex = topLeft.ToVertex;
            hf2.ToVertex = bottomRight.ToVertex;
            hf1.Face.HalfEdge = hf1;
            hf2.Face.HalfEdge = hf2;
            topLeft.Face = hf2.Face;
            bottomRight.Face = hf1.Face;

            ConnectHalfedge(topLeft, hf2, topRight);
            ConnectHalfedge(bottomRight, hf1, bottomLeft);

            return true;
        }

        private static void ConnectHalfedge(params TriMesh.HalfEdge[] halfEdges)
        {
            for (int i = 0; i < halfEdges.Length; i++)
            {
                TriMesh.HalfEdge prev = halfEdges[i];
                TriMesh.HalfEdge next = halfEdges[(i + 1) % halfEdges.Length];
                prev.Next = next;
            }
        }

        private static TriMesh.HalfEdge[] FindGroup(TriMesh.Vertex v, TriMesh.Vertex begin, TriMesh.Vertex end)
        {
            List<TriMesh.HalfEdge> group = new List<TriMesh.HalfEdge>();

            TriMesh.HalfEdge startHf = v.FindHalfEdgeTo(begin);

            if (startHf == null) throw new Exception("start halfedge can't find, check begin vertex and end vertex");

            TriMesh.HalfEdge currentHf = startHf;

            while (currentHf.ToVertex != end)
            {
                group.Add(currentHf);
                currentHf = currentHf.Opposite.Next;
            }
            group.Add(currentHf);

            return group.ToArray();
        }

        private static TriMesh.HalfEdge[] AddInnerTriangle(params TriMesh.Vertex[] vertices)
        {
            if (vertices.Length < 3) throw new Exception("error");

            TriMesh mesh = vertices[0].Mesh;
            // 构造一个三角面，我觉得他有什么大病一样
            TriMesh.Face face = new TriMesh.Face();
            mesh.AppendToFaceList(face);

            // 为这个面构造三个半边
            TriMesh.HalfEdge[] halfedgeSet = new TriMesh.HalfEdge[3];
            for (int i = 0; i < halfedgeSet.Length; i++)
            {
                // 设置一下半边从哪里来到哪里去，以及临近的 Face
                halfedgeSet[i] = new TriMesh.HalfEdge(default(HalfedgeTraits));
                halfedgeSet[i].ToVertex = vertices[(i + 1) % halfedgeSet.Length];
                halfedgeSet[i].Face = face;
                mesh.AppendToHalfedgeList(halfedgeSet[i]);
            }

            // 设置这个面的起始 halfedge
            face.HalfEdge = halfedgeSet[0];

            // 应该就是把三个半边连起来
            for (int i = 0; i < halfedgeSet.Length; i++)
            {
                TriMesh.HalfEdge prevHalfEdge = halfedgeSet[i];
                TriMesh.HalfEdge nextHalfEdge = halfedgeSet[(i + 1) % halfedgeSet.Length];
                prevHalfEdge.Next = nextHalfEdge;
                nextHalfEdge.Previous = prevHalfEdge;
            }
            return halfedgeSet;
        }

        // oldHf是分割前的半边，newHf是新生成的三角形的半边，两个半边是重合的
        private static void InsertEdge(TriMesh.HalfEdge newHf, TriMesh.HalfEdge oldHf)
        {
            // 新的不动，copy旧的Opposite
            newHf.Opposite = oldHf.Opposite;
            // 很巧妙的将oldHf移到另一边
            newHf.Next.Opposite = oldHf;
            // 自己体会吧....难以用语言描述，反正是对的，一定要先设置这个再做下一句
            oldHf.Opposite.Opposite = newHf;
            oldHf.Opposite = newHf.Next;
            oldHf.Edge.HalfEdge0 = oldHf;

            // 反正就是很奇怪的把半边和边都移了个位置，然后生成了新边
            TriMesh.Edge edge = new TriMesh.Edge(default(EdgeTraits));
            edge.HalfEdge0 = newHf;
            newHf.Edge = edge;
            newHf.Opposite.Edge = edge;
            newHf.Next.Edge = oldHf.Edge;

            newHf.Mesh.AppendToEdgeList(edge);
        }

        public static TriMesh.Vertex VertexSplit(TriMesh.Vertex v1, TriMesh.Vertex share1, TriMesh.Vertex share2, Vector3D v1Position, Vector3D v2Position, int fixedIndex)
        {
            TriMesh mesh = v1.Mesh;
            // 找到share1和share2之间的所有半边，另外一半可以用Opposite去改
            TriMesh.HalfEdge[] halfedgeSet = FindGroup(v1, share1, share2);
            // 将当前存在的点移动至v1Position
            v1.Traits.Position = v1Position;
            v1.HalfEdge = halfedgeSet[halfedgeSet.Length - 1];

            // 新建顶点v2
            TriMesh.Vertex v2 = new TriMesh.Vertex(new VertexTraits(v2Position));
            // fixedIndex暂时不知道干啥的
            v2.Traits.FixedIndex = fixedIndex;
            v2.HalfEdge = halfedgeSet[0];

            mesh.Vertices.Add(v2);

            for (int i = 0; i < halfedgeSet.Length - 1; i++)
            {
                halfedgeSet[i].Opposite.ToVertex = v2;
            }

            TriMesh.HalfEdge[] triangle1 = AddInnerTriangle(v1, v2, share1);
            InsertEdge(triangle1[1], halfedgeSet[0]);

            TriMesh.HalfEdge[] triangle2 = AddInnerTriangle(v2, v1, share2);
            InsertEdge(triangle2[1], halfedgeSet[halfedgeSet.Length - 1]);

            TriMesh.Edge edge = new TriMesh.Edge(default(EdgeTraits));
            edge.HalfEdge0 = triangle1[0];
            triangle1[0].Edge = edge;
            triangle2[0].Edge = edge;
            triangle1[0].Opposite = triangle2[0];
            triangle2[0].Opposite = triangle1[0];

            mesh.AppendToEdgeList(edge);

            return v2;
        }

        public static bool RemoveVertex3(TriMesh.Vertex top)
        {
            TriMesh mesh = top.Mesh;

            TriMesh.HalfEdge leftToRight = null;
            foreach (TriMesh.HalfEdge item in top.HalfEdges)
            {
                if (!item.Next.OnBoundary)
                {
                    leftToRight = item.Next;
                    break;
                }
            }

            if (leftToRight == null) return false;

            TriMesh.Vertex left, bottom, right, mid;
            left = leftToRight.FromVertex;
            right = leftToRight.ToVertex;
            mid = top.Mesh.Vertices.Add(new VertexTraits((left.Traits.Position + right.Traits.Position) / 2.0));
            bottom = leftToRight.Opposite.Next.ToVertex;

            mesh.RemoveEdge(leftToRight.Edge);

            mesh.AddFace(top, left, mid);
            mesh.AddFace(top, mid, right);
            mesh.AddFace(bottom, right, mid);
            mesh.AddFace(bottom, mid, left);

            return true;
        }

        public static bool RemoveVertex4(TriMesh.Vertex cur)
        {
            TriMesh mesh = cur.Mesh;

            TriMesh.HalfEdge curToLeft = null;
            foreach (TriMesh.HalfEdge halfEdge in cur.HalfEdges)
            {
                if (halfEdge.Face != null && halfEdge.Face.FaceCount == 3)
                {
                    curToLeft = halfEdge;
                    break;
                }
            }

            if (curToLeft == null) return false;

            TriMesh.HalfEdge leftToRight = curToLeft.Next;
            TriMesh.HalfEdge rightToCur = leftToRight.Next;
            TriMesh.Vertex left = curToLeft.ToVertex;
            TriMesh.Vertex right = leftToRight.ToVertex;

            TriMesh.Vertex vl = curToLeft.Opposite.Next.ToVertex;
            TriMesh.Vertex vr = rightToCur.Opposite.Next.ToVertex;
            TriMesh.Vertex vm = leftToRight.Opposite.Next.ToVertex;

            TriMesh.Vertex lm = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(left, cur)));
            TriMesh.Vertex rm = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(right, cur)));
            TriMesh.Vertex mm = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(left, right)));

            TriMesh.Vertex sm = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(lm, rm)));
            TriMesh.Vertex sl = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(lm, mm)));
            TriMesh.Vertex sr = mesh.Vertices.Add(new VertexTraits(TriMeshMeasure.GetMidPoint(mm, rm)));

            mesh.RemoveEdge(curToLeft.Edge);
            mesh.RemoveEdge(leftToRight.Edge);
            mesh.RemoveEdge(rightToCur.Edge);

            mesh.AddFace(vl, left, lm);
            mesh.AddFace(vl, lm, cur);
            mesh.AddFace(vr, cur, rm);
            mesh.AddFace(vr, rm, right);
            mesh.AddFace(mm, left, vm);
            mesh.AddFace(mm, vm, right);

            mesh.AddFace(cur, lm, sm);
            mesh.AddFace(cur, sm, rm);
            mesh.AddFace(lm, left, sl);
            mesh.AddFace(sl, left, mm);
            mesh.AddFace(rm, sr, right);
            mesh.AddFace(sr, mm, right);

            mesh.AddFace(sm, lm, sl);
            mesh.AddFace(sm, sr, rm);
            mesh.AddFace(sm, sl, sr);
            mesh.AddFace(sl, mm, sr);

            return true;
        }

        public static bool SplitVertex7(TriMesh.Vertex vertex)
        {
            if (vertex.VertexCount <= 7) return false;

            List<TriMesh.HalfEdge> keepVertex = new List<TriMesh.HalfEdge>();
            List<TriMesh.HalfEdge> moveVertex = new List<TriMesh.HalfEdge>();

            if (!vertex.OnBoundary)
            {
                // 分成两块
                foreach (TriMesh.HalfEdge v in vertex.HalfEdges)
                {
                    if (keepVertex.Count < 6)
                    {
                        keepVertex.Add(v);
                    }
                    else
                    {
                        moveVertex.Add(v);
                    }
                }
            }
            else
            {
                TriMesh.HalfEdge halfEdge = vertex.HalfEdge;
                TriMesh.HalfEdge cur = halfEdge;
                while (cur.Opposite.Next != halfEdge)
                {
                    if (cur.Edge.OnBoundary) break;
                    cur = cur.Opposite.Next;
                }

                keepVertex.Add(cur.Previous.Opposite.Previous.Opposite);
                keepVertex.Add(cur.Previous.Opposite);
                keepVertex.Add(cur);
                keepVertex.Add(cur.Opposite.Next);
                keepVertex.Add(cur.Opposite.Next.Opposite.Next);

                if (cur.Opposite.Next.Edge.OnBoundary)
                {
                    keepVertex.Add(cur.Opposite.Next.Opposite.Next.Opposite.Next);
                }
                else if (cur.Previous.Opposite.Edge.OnBoundary)
                {
                    keepVertex.Add(cur.Previous.Opposite.Previous.Opposite.Previous.Opposite);
                }

                TriMesh.HalfEdge left = keepVertex[5];
                TriMesh.HalfEdge right = keepVertex[0];
                while (left.Opposite.Next != right)
                {
                    moveVertex.Add(left.Opposite.Next);
                    left = left.Opposite.Next;
                }
            }

            TriMesh.Vertex share1 = keepVertex[0].ToVertex;
            TriMesh.Vertex share2 = keepVertex[5].ToVertex;

            TriMesh.Edge targetEdge = moveVertex[moveVertex.Count / 2].Edge;
            Vector3D newPosition = (targetEdge.Vertex0.Traits.Position + targetEdge.Vertex1.Traits.Position) / 2.0;

            TriMesh.Vertex newV = TriMeshModify.VertexSplit(vertex, share2, share1, vertex.Traits.Position, newPosition, 0);

            return true;
        }

        public static TriMesh.HalfEdge[] Sort(LinkedList<TriMesh.Edge> edgeList)
        {
            List<TriMesh.HalfEdge> result = new List<TriMesh.HalfEdge>();
            TriMesh.Edge firstEdge = edgeList.First.Value;
            edgeList.RemoveFirst();

            TriMesh.HalfEdge hf0 = firstEdge.HalfEdge0;
            TriMesh.HalfEdge hf1 = firstEdge.HalfEdge1;

            result.Add(hf0);
            TriMesh.Vertex prevVertex = hf0.ToVertex;
            TriMesh.Vertex nextVertex = hf0.ToVertex;

            while (true)
            {
                prevVertex = nextVertex;
                foreach (TriMesh.HalfEdge hf in nextVertex.HalfEdges)
                {
                    if (edgeList.Contains(hf.Edge))
                    {
                        result.Add(hf);
                        nextVertex = hf.ToVertex;
                        edgeList.Remove(hf.Edge);
                        break;
                    }
                }

                if (prevVertex == nextVertex)
                {
                    break;
                }
            }

            prevVertex = hf1.ToVertex;
            nextVertex = hf1.ToVertex;

            while (true)
            {
                prevVertex = nextVertex;
                foreach (TriMesh.HalfEdge hf in nextVertex.HalfEdges)
                {
                    if (edgeList.Contains(hf.Edge))
                    {
                        result.Insert(0, hf.Opposite);
                        nextVertex = hf.ToVertex;
                        edgeList.Remove(hf.Edge);
                        break;
                    }
                }

                if (prevVertex == nextVertex)
                {
                    break;
                }
            }

            return result.ToArray();
        }
    }
}
