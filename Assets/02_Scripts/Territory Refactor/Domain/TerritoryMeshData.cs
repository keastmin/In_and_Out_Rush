using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryMeshData
    {
        public TerritoryMeshData(List<Vector2> vertices, List<int> triangles)
        {
            Vertices = vertices;
            Triangles = triangles;
        }

        public List<Vector2> Vertices { get; }
        public List<int> Triangles { get; }
    }
}
