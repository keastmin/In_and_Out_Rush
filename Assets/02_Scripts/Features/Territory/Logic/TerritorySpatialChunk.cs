using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritorySpatialChunk
    {
        public TerritorySpatialChunk(
            Vector2Int coordinate,
            IReadOnlyList<int> edgeIndices,
            IReadOnlyList<TerritorySpatialEdge> edges)
        {
            Coordinate = coordinate;
            Vector2 minimum = new(
                coordinate.x * TerritorySegmentTraversal.ChunkSize,
                coordinate.y * TerritorySegmentTraversal.ChunkSize);
            Vector2 maximum = minimum + Vector2.one * TerritorySegmentTraversal.ChunkSize;
            Quadtree = new TerritorySpatialQuadtree(minimum, maximum, edgeIndices, edges);
        }

        public Vector2Int Coordinate { get; }
        public TerritorySpatialQuadtree Quadtree { get; }
    }
}
