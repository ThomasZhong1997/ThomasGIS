using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThomasGIS.Mesh.Basic;

namespace ThomasGIS.Mesh.IO
{
    public static class TriMeshExporter
    {
        public static void ExportObj(TriMesh mesh, string outputFilePath)
        {
            mesh.RefreshAllIndex();

            FileInfo outputFileInfo = new FileInfo(outputFilePath);
            if (!Directory.Exists(outputFileInfo.DirectoryName))
            {
                return;
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(outputFilePath, FileMode.Create)))
            {
                for (int i = 0; i < mesh.Vertices.Count; i++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("v ");
                    sb.Append(mesh.Vertices[i].Traits.Position.X);
                    sb.Append(' ');
                    sb.Append(mesh.Vertices[i].Traits.Position.Y);
                    sb.Append(' ');
                    sb.Append(mesh.Vertices[i].Traits.Position.Z);
                    sw.WriteLine(sb.ToString());
                }

                Dictionary<string, int> textureCoordinateDict = new Dictionary<string, int>();
                // 输出所有的纹理坐标
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    TriMesh.Face face = mesh.Faces[i];
                    foreach (TriMesh.HalfEdge halfedge in face.HalfEdges)
                    {
                        if (halfedge.Traits.TextureCoordinate == null) continue;

                        string key = halfedge.Traits.TextureCoordinate.X + "-" + halfedge.Traits.TextureCoordinate.Y;
                        if (!textureCoordinateDict.ContainsKey(key))
                        {
                            textureCoordinateDict.Add(key, textureCoordinateDict.Count);
                            sw.WriteLine($"vt {halfedge.Traits.TextureCoordinate.X} {halfedge.Traits.TextureCoordinate.Y}");
                        }
                    }
                }

                Dictionary<string, int> normalCoordinateDict = new Dictionary<string, int>();
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    TriMesh.Face face = mesh.Faces[i];
                    if (face.Normal == null) continue;
                    string key = face.Normal.X + "-" + face.Normal.Y + "-" + face.Normal.Z;

                    if (!normalCoordinateDict.ContainsKey(key))
                    {
                        normalCoordinateDict.Add(key, normalCoordinateDict.Count);
                        sw.WriteLine($"vn {face.Normal.X} {face.Normal.Y} {face.Normal.Z}");
                    }
                }

                // 输出所有的面
                for (int i = 0; i < mesh.Faces.Count; i++)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("f ");

                    foreach (TriMesh.HalfEdge halfEdge in mesh.Faces[i].HalfEdges)
                    {
                        sb.Append(halfEdge.ToVertex.Index + 1);
                        if (halfEdge.Traits.TextureCoordinate != null)
                        {
                            string key = halfEdge.Traits.TextureCoordinate.X + "-" + halfEdge.Traits.TextureCoordinate.Y;
                            sb.Append("/" + (textureCoordinateDict[key] + 1).ToString());
                        }

                        if (mesh.Faces[i].Normal != null)
                        {
                            string key = mesh.Faces[i].Normal.X + "-" + mesh.Faces[i].Normal.Y + "-" + mesh.Faces[i].Normal.Z;
                            sb.Append("/" + (normalCoordinateDict[key] + 1).ToString());
                        }
                        sb.Append(" ");
                    }
                    sw.WriteLine(sb.ToString().Trim(' '));
                }

                sw.Close();
            }
        }
    }
}
