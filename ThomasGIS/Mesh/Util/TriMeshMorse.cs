using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;

namespace ThomasGIS.Mesh.Util
{
    public enum MorseVertexType
    {
        Regular,
        Saddle,
        Minium,
        Maxium
    }

    public class TriMeshMorse
    {
        public TriMesh Mesh;
        private MorseVertexType[] morseType;
        private double[] function;

        public TriMeshMorse(TriMesh mesh)
        {
            this.Mesh = mesh;
            this.function = GetX(mesh);
            this.AdjustFunction(mesh, ref this.function);
            this.morseType = ComputeMorse(mesh, this.function);
        }

        public double[] GetX(TriMesh mesh)
        {
            List<double> X = new List<double>(mesh.Vertices.Count);

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                X.Add(mesh.Vertices[i].Traits.Position.X);
            }

            return X.ToArray();
        }

        public double[] ComputeCosX(TriMesh mesh, double cos)
        {
            double[] function = new double[mesh.Vertices.Count];

            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                function[i] = Math.Cos(mesh.Vertices[i].Traits.Position.X * cos);
            }

            return function;
        }

        private bool IsMax(TriMesh.Vertex v, ref double[] function)
        {
            bool result = true;
            foreach (TriMesh.Vertex nv in v.Vertices)
            {
                if (function[nv.Index] >= function[v.Index])
                {
                    result = false;
                    break;
                }
            }
            return result;
        }

        public void AdjustFunction(TriMesh mesh, ref double[] function)
        {
            LinkedList<TriMesh.Edge> link = new LinkedList<TriMesh.Edge>();
            foreach (TriMesh.Edge edge in mesh.Edges)
            {
                if (function[edge.Vertex0.Index] == function[edge.Vertex1.Index])
                {
                    link.AddLast(edge);
                }
            }

            while (link.Count != 0)
            {
                TriMesh.HalfEdge[] path = TriMeshModify.Sort(link);
                {
                    double adj = 0.00000001;
                    if (IsMax(path[0].FromVertex, ref function))
                    {
                        adj = -adj;
                    }

                    foreach (TriMesh.HalfEdge hf in path)
                    {
                        function[hf.ToVertex.Index] = function[hf.FromVertex.Index] + adj;
                    }
                }
            }
        }

        public int CountChange(TriMesh.Vertex v, double[] function)
        {
            double mid = function[v.Index];
            int count = 0;

            foreach (TriMesh.HalfEdge hf in v.HalfEdges)
            {
                TriMesh.HalfEdge lk = hf.Next;
                double from = function[lk.FromVertex.Index];
                double to = function[lk.ToVertex.Index];

                if ((from > mid) != (to > mid))
                {
                    count++;
                }
            }

            return count;
        }

        public int ComputeSaddleType(TriMesh.Vertex saddle, double[] function)
        {
            int m = CountChange(saddle, function) / 2 - 1;
            return m;
        }

        public MorseVertexType[] ComputeMorse(TriMesh mesh, double[] function)
        {
            MorseVertexType[] morseVertexTypes = new MorseVertexType[mesh.Vertices.Count];
            foreach (TriMesh.Vertex v in mesh.Vertices)
            {
                double mid = function[v.Index];
                int change = CountChange(v, function);

                if (change == 2)
                {
                    morseVertexTypes[v.Index] = MorseVertexType.Regular;
                }
                else if (change > 2 && change % 2 == 0)
                {
                    morseVertexTypes[v.Index] = MorseVertexType.Saddle;
                }
                else if (change == 0)
                {
                    double round = function[v.HalfEdge.ToVertex.Index];
                    if (round > mid)
                    {
                        morseVertexTypes[v.Index] = MorseVertexType.Minium;
                    }
                    else
                    {
                        morseVertexTypes[v.Index] = MorseVertexType.Maxium;
                    }
                }
                else
                {
                    throw new Exception("不应该为奇数");
                }
            }
            return morseVertexTypes;
        }

        //private TriMesh.HalfEdge[] FindExtreme(TriMesh.Vertex v, bool maxOrMin)
        //{
        //    switch (this.morseType[v.Index])
        //    {
        //        case MorseVertexType.Regular:
        //        case MorseVertexType.Minium:
        //        case MorseVertexType.Maxium:
        //            TriMesh.HalfEdge hf = FindExtreme(v.HalfEdges, maxOrMin);
        //            if (hf != null)
        //            {
        //                return new TriMesh.HalfEdge[] { hf };
        //            }
        //            break;
        //        case MorseVertexType.Saddle:
        //            return FindSaddleExtreme(v, maxOrMin);
        //        default:
        //            break;
        //    }
        //    return new TriMesh.HalfEdge[0];
        //}

        //public TriMesh.HalfEdge[][] FindPath(TriMesh.Vertex v, bool ascOrDesc)
        //{
        //    List<TriMesh.HalfEdge[]> all = new List<TriMesh.HalfEdge[]>();
        //    List<TriMesh.HalfEdge> path = new List<TriMesh.HalfEdge>();
        //    Stack<TriMesh.HalfEdge> stack = new Stack<TriMesh.HalfEdge>();
        //    stack.Push(v.HalfEdge.Opposite);

        //    while (stack.Count != 0)
        //    {
        //        if (stack.Count > 100000)
        //        {
        //            throw new Exception("Error!");
        //        }

        //        TriMesh.HalfEdge cur = stack.Pop();
        //        if (cur.ToVertex != v)
        //        {
        //            path.Add(cur);
        //        }
        //        TriMesh.HalfEdge[] extreme = FindExtreme(cur.ToVertex, ascOrDesc);

        //        if (extreme.Length == 0)
        //        {
        //            all.Add(path.ToArray());
        //        }

        //        foreach (TriMesh.HalfEdge hf in extreme)
        //        {
        //            if (!this.WithoutSaddle || this.morseType[hf.ToVertex.Index] != MorseVertexType.Saddle)
        //            {
        //                stack.Push(hf);
        //            }
        //            else
        //            {
        //                TriMesh.HalfEdge start = hf.Opposite;
        //                double saddleValue = this.function[hf.ToVertex.Index];
        //                double roundValue = this.function[start.Next.ToVertex.Index];
        //                while (!this.Compare(saddleValue, roundValue, ascOrDesc))
        //                {
        //                    start = start.Previous.Opposite;
        //                    roundValue = this.function[start.Next.ToVertex.Index];
        //                    path.Add(start.Next);
        //                }
        //                stack.Push(start.Next);
        //            }
        //        }
        //    }
        //    return all.ToArray();
        //}
    }
}
