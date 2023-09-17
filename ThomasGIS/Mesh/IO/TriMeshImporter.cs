using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ThomasGIS.Mesh.Basic;
using ThomasGIS.Mesh.Vector;

namespace ThomasGIS.Mesh.IO
{
    public static class TriMeshImporter
    {
        public static TriMesh ImportObj(string inputFilePath)
        {
            TriMesh mesh = new TriMesh();
            List<Vector2D> textureCoordinateList = new List<Vector2D>();
            List<Vector3D> normalCoordinateList = new List<Vector3D>();

            using (StreamReader sr = new StreamReader(new FileStream(inputFilePath, FileMode.Open)))
            {
                string nowMtlName = "";
                while (!sr.EndOfStream)
                {
                    string line = sr.ReadLine();
                    string[] parts = line.Split(' ');
                    if (parts[0] == "v")
                    {
                        double x = double.Parse(parts[1]);
                        double y = double.Parse(parts[2]);
                        double z = double.Parse(parts[3]);
                        mesh.AppendToVertexList(new TriMesh.Vertex(new VertexTraits(x, y, z)));
                    }

                    if (parts[0] == "f")
                    {
                        string[] p1 = parts[1].Split('/');
                        TriMesh.Vertex v1 = mesh.Vertices[Convert.ToInt32(p1[0]) - 1];
                        for (int i = 2; i < parts.Length - 1; i++)
                        {
                            string[] p2 = parts[i].Split('/');
                            string[] p3 = parts[i + 1].Split('/');
                            TriMesh.Vertex v2 = mesh.Vertices[Convert.ToInt32(p2[0]) - 1];
                            TriMesh.Vertex v3 = mesh.Vertices[Convert.ToInt32(p3[0]) - 1];
                            TriMesh.Face newFace = mesh.AddFace(v1, v2, v3);

                            if (p1.Length > 1)
                            {
                                List<TriMesh.HalfEdge> faceHalfEdges = newFace.HalfEdges.ToList();
                                foreach (TriMesh.HalfEdge halfedge in faceHalfEdges)
                                {
                                    if (halfedge.ToVertex == v1)
                                    {
                                        halfedge.Traits.MaterialName = nowMtlName;
                                        Vector2D textureCoordinate = textureCoordinateList[Convert.ToInt32(p1[1]) - 1];
                                        halfedge.Traits.TextureCoordinate = new Vector2D(textureCoordinate.X, textureCoordinate.Y);
                                    }
                                    else if (halfedge.ToVertex == v2)
                                    {
                                        halfedge.Traits.MaterialName = nowMtlName;
                                        Vector2D textureCoordinate = textureCoordinateList[Convert.ToInt32(p2[1]) - 1];
                                        halfedge.Traits.TextureCoordinate = new Vector2D(textureCoordinate.X, textureCoordinate.Y);
                                    }
                                    else if (halfedge.ToVertex == v3)
                                    {
                                        halfedge.Traits.MaterialName = nowMtlName;
                                        Vector2D textureCoordinate = textureCoordinateList[Convert.ToInt32(p3[1]) - 1];
                                        halfedge.Traits.TextureCoordinate = new Vector2D(textureCoordinate.X, textureCoordinate.Y);
                                    }
                                }
                            }

                            if (p1.Length > 2)
                            {
                                newFace.Traits.Normal = new Vector3D(normalCoordinateList[Convert.ToInt32(p1[2]) - 1]);
                            }
                        }
                    }

                    if (parts[0] == "vt")
                    {
                        double tx = double.Parse(parts[1]);
                        double ty = double.Parse(parts[2]);
                        textureCoordinateList.Add(new Vector2D(tx, ty));
                    }

                    if (parts[0] == "vn")
                    {
                        double nx = double.Parse(parts[1]);
                        double ny = double.Parse(parts[2]);
                        double nz = double.Parse(parts[3]);
                        normalCoordinateList.Add(new Vector3D(nx, ny, nz));
                    }

                    if (parts[0] == "usemtl")
                    {
                        nowMtlName = parts[1];
                    }
                }
                sr.Close();
            }

            textureCoordinateList.Clear();
            normalCoordinateList.Clear();
            return mesh;
        }
    }
}
