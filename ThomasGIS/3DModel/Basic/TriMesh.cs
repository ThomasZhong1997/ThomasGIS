using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Geometries;

namespace ThomasGIS._3DModel.Basic
{
    public class TriMesh
    {
        public List<MeshVertex> vertexList;
        public List<IPoint> textureCoordinateList;
        public List<IPoint3D> normalList;
        public List<MeshFace> faceList;

        public int VertexNumber => vertexList.Count;
        public int TextureCoordinateNumber => textureCoordinateList.Count;
        public int NormalNumber => normalList.Count;
        public int FaceNumber => faceList.Count;

        public TriMesh()
        {
            vertexList = new List<MeshVertex>();
            textureCoordinateList = new List<IPoint>();
            normalList = new List<IPoint3D>();
            faceList = new List<MeshFace>();
        }

        public int AddVertex(MeshVertex meshVertex)
        {
            this.vertexList.Add(meshVertex);
            return this.vertexList.Count - 1;
        }

        public int AddTextureCoordinate(IPoint uvPoint)
        {
            this.textureCoordinateList.Add(uvPoint);
            return this.textureCoordinateList.Count - 1;
        }

        public int AddNormal(IPoint3D normal)
        {
            this.normalList.Add(normal);
            return this.normalList.Count - 1;
        }

        public int AddFace(MeshFace meshFace)
        {
            this.faceList.Add(meshFace);
            return this.faceList.Count - 1;
        }
    }
}
