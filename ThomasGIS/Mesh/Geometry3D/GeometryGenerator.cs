using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;
using static ThomasGIS.Mesh.Basic.TriMesh;

namespace ThomasGIS.Mesh.Geometry3D
{
    public static class GeometryGenerator
    {
        public static TriMesh.Vertex[] AddPolygon(TriMesh mesh, int edge, double z, bool topOrBottom)
        {
            List<TriMesh.Vertex> list = new List<TriMesh.Vertex>();
            for (int i = 0; i < edge; i++)
            {
                double x = 0.5 * Math.Cos(Math.PI * 2.0 / edge * i);
                double y = 0.5 * Math.Sin(Math.PI * 2.0 / edge * i);
                list.Add(mesh.Vertices.Add(new VertexTraits(x, y, z)));
            }

            switch (edge)
            {
                case 3:
                    if (topOrBottom)
                    {
                        mesh.AddFace(list[0], list[1], list[2]);
                        // mesh.Faces.AddTriangles(list[0], list[1], list[2]);
                    }
                    else
                    {
                        mesh.AddFace(list[0], list[2], list[1]);
                        // mesh.Faces.AddTriangles(list[0], list[2], list[1]);
                    }
                    break;
                case 4:
                    int o = topOrBottom ? 3 : 1;
                    mesh.AddFace(list[0], list[2], list[o]);
                    mesh.AddFace(list[2], list[0], list[(o + 2) % 4]);
                    // mesh.Faces.AddTriangles(list[0], list[2], list[o]);
                    // mesh.Faces.AddTriangles(list[2], list[0], list[(o + 2) % 4]);
                    break;
                default:
                    TriMesh.Vertex center = mesh.Vertices.Add(new VertexTraits(0, 0, z));
                    for (int i = 0; i < edge; i++)
                    {
                        int next = topOrBottom ? (i + 1) % edge : (i + edge - 1) % edge;
                        mesh.AddFace(center, list[i], list[next]);
                        // mesh.Faces.AddTriangles(center, list[i], list[next]);
                    }
                    break;
            }
            return list.ToArray();
        }

        public static TriMesh CreateCone(int edge, double height)
        {
            TriMesh cone = new TriMesh();
            TriMesh.Vertex[] bottom = AddPolygon(cone, edge, -height / 2.0, false);
            TriMesh.Vertex top = cone.Vertices.Add(new VertexTraits(0, 0, height / 2.0));

            for (int i = 0; i < edge; i++)
            {
                int next = (i + 1) % edge;
                cone.AddFace(top, bottom[i], bottom[next]);
                // cone.Faces.AddTriangles(top, bottom[i], bottom[next]);
            }

            return cone;
        }

        public static TriMesh CreateCylinder(int edge, double height)
        {
            TriMesh cylinder = new TriMesh();
            TriMesh.Vertex[] top = AddPolygon(cylinder, edge, height / 2.0, true);
            TriMesh.Vertex[] bottom = AddPolygon(cylinder, edge, -height / 2.0, false);
            for (int i = 0; i < edge; i++)
            {
                int next = (i + 1) % edge;
                cylinder.AddFace(bottom[i], top[next], top[i]);
                cylinder.AddFace(bottom[i], bottom[next], top[next]);
                // cylinder.Faces.AddTriangles(bottom[i], top[next], top[i]);
                // cylinder.Faces.AddTriangles(bottom[i], bottom[next], top[next]);
            }
            return cylinder;
        }

        public static TriMesh CreateSphere(int precision)
        {
            if (precision < 10) precision = 10;
            if (precision % 2 == 1) precision += 1;
            int n = precision;
            TriMesh sphere = new TriMesh();
            for (int j = -n / 2 + 1; j < n / 2; j++)
            {
                double distance = Math.Sin(j * Math.PI / n);
                double rCircle = Math.Cos(j * Math.PI / n);

                for (int i = 0; i < n; i++)
                {
                    sphere.AppendToVertexList(new Vertex(new VertexTraits(rCircle * Math.Cos(2 * i * Math.PI / n), rCircle * Math.Sin(2 * i * Math.PI / n), distance)));
                }
            }

            for (int i = 0; i < n - 2; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    TriMesh.Vertex topRight = sphere.Vertices[i * n + j];
                    TriMesh.Vertex topLeft = sphere.Vertices[i * n + (j + 1) % n];
                    TriMesh.Vertex bottomRight = sphere.Vertices[(i + 1) * n + j];
                    TriMesh.Vertex bottomLeft = sphere.Vertices[(i + 1) * n + (j + 1) % n];
                    sphere.AddFace(topRight, bottomLeft, bottomRight);
                    sphere.AddFace(topRight, topLeft, bottomLeft);
                }
            }

            int last = sphere.Vertices.Count - 1;

            TriMesh.Vertex tv = new Vertex(new VertexTraits(0, 0, -1));
            TriMesh.Vertex bv = new Vertex(new VertexTraits(0, 0, 1));
            sphere.AppendToVertexList(tv);
            sphere.AppendToVertexList(bv);

            for (int i = 0; i < n; i++)
            {
                sphere.AddFace(tv, sphere.Vertices[(i + 1) % n], sphere.Vertices[i]);
                sphere.AddFace(bv, sphere.Vertices[last - (i + 1) % n], sphere.Vertices[last - i]);
            }

            return sphere;
        }

        public static TriMesh CreateGrid(int m, int n, double length)
        {
            TriMesh mesh = new TriMesh();
            TriMesh.Vertex[,] arr = new TriMesh.Vertex[m, n];
            double x0 = -m * length / 2d;
            double y0 = -n * length / 2d;

            for (int i = 0; i < m; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    mesh.AppendToVertexList(new Vertex(new VertexTraits(x0 + i * length, y0 + j * length, 0d)));
                    // mesh.Vertices.Add(new VertexTraits(x0 + i * length, y0 + j * length, 0d));
                    arr[i, j] = mesh.Vertices[mesh.Vertices.Count - 1];
                }
            }

            for (int i = 0; i < m - 1; i++)
            {
                for (int j = 0; j < n - 1; j++)
                {
                    mesh.AddFace(arr[i + 1, j], arr[i, j + 1], arr[i, j]);
                    mesh.AddFace(arr[i + 1, j], arr[i + 1, j + 1], arr[i, j + 1]);
                }
            }

            return mesh;
        }

        public static TriMesh Clone(TriMesh mesh)
        {
            mesh.RefreshAllIndex();

            TriMesh newMesh = new TriMesh();
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                VertexTraits traits = new VertexTraits(mesh.Vertices[i].Traits.Position.X, mesh.Vertices[i].Traits.Position.Y, mesh.Vertices[i].Traits.Position.Z);
                newMesh.AppendToVertexList(new Vertex(traits));
            }

            for (int i = 0; i < mesh.Faces.Count; i++)
            {
                List<Vertex> faceVetices = new List<Vertex>();
                foreach (Vertex vertex in mesh.Faces[i].Vertices)
                {
                    faceVetices.Add(newMesh.Vertices[vertex.Index]);
                }
                newMesh.AddFace(faceVetices.ToArray());
            }

            // newMesh.TrimExcess();
            newMesh.RefreshAllIndex();
            return newMesh;
        }
    }
}
