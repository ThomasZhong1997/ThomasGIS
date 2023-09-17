using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS._3DModel.Basic
{
    public class MeshScene
    {
        private List<TriMesh> triMeshList;

        List<string> materials = new List<string>();

        public MeshScene()
        {
            this.triMeshList = new List<TriMesh>();
            materials.Add("m1.jpg");
            materials.Add("m2.jpg");
            materials.Add("m3.jpg");
            materials.Add("m4.png");
            materials.Add("m5.jpg");
            materials.Add("m6.jpg");
            materials.Add("m7.jpg");
            materials.Add("m8.jpg");
        }

        public bool AddTriMesh(TriMesh triMesh)
        {
            this.triMeshList.Add(triMesh);
            return true;
        }

        public bool ExportToObj(string outputPath)
        {
            string mtlPath = outputPath.Substring(0, outputPath.Length - 4) + ".mtl";
            FileInfo mtlPathInfo = new FileInfo(mtlPath);

            using (StreamWriter sw = new StreamWriter(new FileStream(outputPath, FileMode.Create)))
            {
                if (this.materials.Count != 0)
                {
                    sw.WriteLine("mtllib " + mtlPathInfo.Name);
                    sw.WriteLine("");
                }

                int vertexNumber = 1;
                int textureCoorNumber = 1;
                int normalNumber = 1;

                for (int i = 0; i < triMeshList.Count; i++)
                {
                    TriMesh oneTriMesh = triMeshList[i];
                    for (int j = 0; j < oneTriMesh.VertexNumber; j++)
                    {
                        MeshVertex oneVertex = oneTriMesh.vertexList[j];
                        StringBuilder sb = new StringBuilder();
                        sb.Append("v ");
                        sb.Append(oneVertex.X);
                        sb.Append(" ");
                        sb.Append(oneVertex.Y);
                        sb.Append(" ");
                        sb.Append(oneVertex.Z);
                        if (oneVertex.Color != null)
                        {
                            sb.Append(" ");
                            sb.Append(oneVertex.Color.Red);
                            sb.Append(" ");
                            sb.Append(oneVertex.Color.Green);
                            sb.Append(" ");
                            sb.Append(oneVertex.Color.Blue);
                            sb.Append(" ");
                            sb.Append(oneVertex.Color.Alpha);
                        }
                        sw.WriteLine(sb.ToString());
                    }

                    sw.WriteLine("");

                    for (int j = 0; j < oneTriMesh.TextureCoordinateNumber; j++)
                    {
                        IPoint oneTextureCoor = oneTriMesh.textureCoordinateList[j];
                        StringBuilder sb = new StringBuilder();
                        sb.Append("vt ");
                        sb.Append(oneTextureCoor.GetX());
                        sb.Append(" ");
                        sb.Append(oneTextureCoor.GetY());
                        sw.WriteLine(sb.ToString());
                    }
                    sw.WriteLine("");

                    int nowMaterial = -1;
                    for (int j = 0; j < oneTriMesh.FaceNumber; j++)
                    {
                        MeshFace oneFace = oneTriMesh.faceList[j];
                        if (oneFace.MaterialIndex != nowMaterial)
                        {
                            nowMaterial = oneFace.MaterialIndex;
                            sw.WriteLine("usemtl material_" + nowMaterial.ToString());
                        }
                        StringBuilder sb = new StringBuilder();
                        sb.Append("f ");
                        sb.Append(oneFace.Vertex_1 + vertexNumber);
                        sb.Append("/" + (oneFace.TextureCoordinate_1 + textureCoorNumber).ToString());
                        sb.Append(" ");
                        sb.Append(oneFace.Vertex_2 + vertexNumber);
                        sb.Append("/" + (oneFace.TextureCoordinate_2 + textureCoorNumber).ToString());
                        sb.Append(" ");
                        sb.Append(oneFace.Vertex_3 + vertexNumber);
                        sb.Append("/" + (oneFace.TextureCoordinate_3 + textureCoorNumber).ToString());
                        sw.WriteLine(sb.ToString());
                    }

                    sw.WriteLine("");

                    vertexNumber += oneTriMesh.VertexNumber;
                    textureCoorNumber += oneTriMesh.TextureCoordinateNumber;
                    normalNumber += oneTriMesh.NormalNumber;
                }
            }

            using (StreamWriter sw = new StreamWriter(new FileStream(mtlPath, FileMode.Create)))
            {
                for (int i = 0; i < this.materials.Count; i++)
                {
                    sw.WriteLine("newmtl material_" + i.ToString());
                    sw.WriteLine("map_Ka " + materials[i]);
                    sw.WriteLine("map_Kd " + materials[i]);
                    sw.WriteLine("");
                }
            }

            return true;
        }
    }
}
