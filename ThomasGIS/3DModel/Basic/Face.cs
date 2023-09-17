using System;
using System.Collections.Generic;
using System.Text;

namespace ThomasGIS._3DModel.Basic
{
    public class MeshFace
    {
        public int Vertex_1;
        public int Vertex_2;
        public int Vertex_3;

        public int TextureCoordinate_1;
        public int TextureCoordinate_2;
        public int TextureCoordinate_3;

        public int Normal_1;
        public int Normal_2;
        public int Normal_3;

        public int MaterialIndex;

        public MeshFace(int vertex_1, int vertex_2, int vertex_3, int textureCoordinate_1, int textureCoordinate_2, int textureCoordinate_3, int normal_1, int normal_2, int normal_3, int materialIndex)
        {
            this.Vertex_1 = vertex_1;
            this.Vertex_2 = vertex_2;
            this.Vertex_3 = vertex_3;

            this.TextureCoordinate_1 = textureCoordinate_1;
            this.TextureCoordinate_2 = textureCoordinate_2;
            this.TextureCoordinate_3 = textureCoordinate_3;

            this.Normal_1 = normal_1;
            this.Normal_2 = normal_2;
            this.Normal_3 = normal_3;

            this.MaterialIndex = materialIndex;
        }
    }
}
