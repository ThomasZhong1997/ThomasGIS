using System;
using System.Collections.Generic;
using System.Text;
using ThomasGIS.Mesh.Basic;

namespace ThomasGIS.Mesh.Util
{
    public static class TriMeshFillCut
    {
        public static void RepairHole(List<TriMesh.Vertex> hole)
        {
            if (hole.Count < 3) return;
            TriMesh.Vertex baseVertex = hole[0];
            for (int i = 1; i < hole.Count - 1; i++)
            {
                TriMesh.Vertex v1 = hole[i];
                TriMesh.Vertex v2 = hole[i + 1];
                baseVertex.Mesh.AddFace(baseVertex, v1, v2);
            }
        }

        public static void RepairAllHole(TriMesh mesh)
        {
            List<List<TriMesh.Vertex>> holes = TriMeshSearch.RetrieveBoundaryVertexAll(mesh);
            for (int i = 0; i < holes.Count; i++)
            {
                RepairHole(holes[i]);
            }
            mesh.RefreshAllIndex();
        }
    }
}
