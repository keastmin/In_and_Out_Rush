using System.Collections.Generic;
using ProjectIO.Territory;
using UnityEngine;

public class Territory
{
    private List<Vector2> _vertices = new();
    private TerritorySpatialIndex _spatialIndex = new();

    public IReadOnlyList<Vector2> Vertices => _vertices;

    public void Expand(List<Vector2> path)
    {
        TryExpand(path);
    }

    public bool TryExpand(List<Vector2> path)
    {
        return TryExpand(path, out _);
    }

    public bool TryExpand(List<Vector2> path, out TerritoryMeshData meshData)
    {
        if (!TerritoryExpansionCalculator.TryCalculate(_vertices, path, out meshData))
            return false;

        ReplaceVertices(meshData.Vertices);
        return true;
    }

    public static bool TryCalculateExpansion(
        IReadOnlyList<Vector2> sourceVertices,
        IReadOnlyList<Vector2> trailPoints,
        out TerritoryMeshData meshData)
        => TerritoryExpansionCalculator.TryCalculate(sourceVertices, trailPoints, out meshData);

    public void ReplaceVertices(IReadOnlyList<Vector2> vertices)
    {
        var nextVertices = new List<Vector2>(vertices?.Count ?? 0);
        if (vertices != null)
        {
            for (int i = 0; i < vertices.Count; i++)
                nextVertices.Add(vertices[i]);
        }

        var nextIndex = new TerritorySpatialIndex();
        nextIndex.Rebuild(nextVertices);
        _vertices = nextVertices;
        _spatialIndex = nextIndex;
    }

    public bool IsPointOnBoundary(Vector2 point)
        => _spatialIndex.IsPointOnBoundary(point);

    public bool IsPointInPolygon(Vector2 point)
        => _spatialIndex.Contains(point);

    public static Mesh GenerateMesh(IReadOnlyList<Vector2> polygon)
    {
        if (!TerritoryPolygonTriangulator.TryBuildMeshData(
                polygon,
                out List<Vector2> normalized,
                out List<int> triangles,
                out string reason))
        {
            Debug.LogError(reason);
            return null;
        }

        return GenerateMesh(new TerritoryMeshData(normalized, triangles));
    }

    public static Mesh GenerateMesh(TerritoryMeshData meshData)
    {
        if (meshData == null || meshData.Vertices == null || meshData.Vertices.Count < 3 ||
            meshData.Triangles == null || meshData.Triangles.Count < 3)
        {
            return null;
        }

        Vector3[] vertices = new Vector3[meshData.Vertices.Count];
        for (int i = 0; i < meshData.Vertices.Count; i++)
            vertices[i] = new Vector3(meshData.Vertices[i].x, 0f, meshData.Vertices[i].y);

        var mesh = new Mesh
        {
            name = "PolygonMesh",
            vertices = vertices
        };
        if (vertices.Length > 65535)
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        var triangles = new int[meshData.Triangles.Count];
        for (int i = 0; i < triangles.Length; i++)
            triangles[i] = meshData.Triangles[i];
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }
}
