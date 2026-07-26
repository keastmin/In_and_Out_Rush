using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTerritoryChunkClassifier
    {
        private const float BoundaryEpsilon = 0.0001f;

        private List<float>[] _crossingsByRow;
        private List<Vector4>[] _boundaryEdgesByRow;
        private bool[] _builtRows;
        private Territory _cachedTerritory;
        private GridCalculator _cachedGridCalculator;
        private float _cachedGridOriginX;
        private float _cachedCellSize;
        private int _cachedChunkY;
        private bool _hasCachedChunkBand;

        public void Invalidate()
        {
            _cachedTerritory = null;
            _cachedGridCalculator = null;
            _hasCachedChunkBand = false;
            ClearBuiltRows();
        }

        public void Rebuild(
            InfiniteGridChunkState chunkState,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize,
            Territory territory)
        {
            System.Array.Clear(
                chunkState.TerritoryCells,
                0,
                chunkState.TerritoryCells.Length);

            IReadOnlyList<Vector2> vertices =
                territory != null ? territory.Vertices : null;
            if (vertices == null || vertices.Count < 3)
            {
                return;
            }

            int chunkColumnCount = gridCalculator.ChunkColumnCount;
            int chunkRowCount = gridCalculator.ChunkRowCount;
            Vector2Int chunkMin = gridCalculator.GetChunkMinCellIndex(chunkState.Key);
            PrepareScanlineBand(
                chunkState.Key.Y,
                chunkRowCount,
                gridCalculator,
                gridOrigin.x,
                cellSize,
                territory);

            for (int localRow = 0; localRow < chunkRowCount; localRow++)
            {
                int row = chunkMin.y + localRow;
                List<float> crossings = _crossingsByRow[localRow];
                List<Vector4> boundaryEdges = _boundaryEdgesByRow[localRow];
                if (!_builtRows[localRow])
                {
                    Vector3 firstCenter =
                        gridCalculator.GetCellCenterPositionFromCellIndex(
                            gridOrigin,
                            chunkMin.x,
                            row,
                            cellSize);
                    BuildScanline(
                        vertices,
                        firstCenter.x,
                        crossings,
                        boundaryEdges);
                    _builtRows[localRow] = true;
                }

                for (int localCol = 0; localCol < chunkColumnCount; localCol++)
                {
                    int col = chunkMin.x + localCol;
                    Vector3 center =
                        gridCalculator.GetCellCenterPositionFromCellIndex(
                            gridOrigin,
                            col,
                            row,
                            cellSize);
                    Vector2 centerPoint = new Vector2(center.x, center.z);
                    bool isInside =
                        IsPointOnBoundary(centerPoint, boundaryEdges) ||
                        IsInsideFromCrossings(center.z, crossings);
                    chunkState.TerritoryCells[localRow * chunkColumnCount + localCol] =
                        isInside ? byte.MaxValue : (byte)0;
                }
            }
        }

        private void PrepareScanlineBand(
            int chunkY,
            int chunkRowCount,
            GridCalculator gridCalculator,
            float gridOriginX,
            float cellSize,
            Territory territory)
        {
            EnsureScanlineStorage(chunkRowCount);
            if (_hasCachedChunkBand &&
                _cachedChunkY == chunkY &&
                _cachedGridCalculator == gridCalculator &&
                _cachedTerritory == territory &&
                _cachedGridOriginX.Equals(gridOriginX) &&
                _cachedCellSize.Equals(cellSize))
            {
                return;
            }

            _cachedChunkY = chunkY;
            _cachedGridCalculator = gridCalculator;
            _cachedTerritory = territory;
            _cachedGridOriginX = gridOriginX;
            _cachedCellSize = cellSize;
            _hasCachedChunkBand = true;
            ClearBuiltRows();
        }

        private void EnsureScanlineStorage(int rowCount)
        {
            if (_builtRows != null && _builtRows.Length == rowCount)
            {
                return;
            }

            _crossingsByRow = new List<float>[rowCount];
            _boundaryEdgesByRow = new List<Vector4>[rowCount];
            _builtRows = new bool[rowCount];
            for (int row = 0; row < rowCount; row++)
            {
                _crossingsByRow[row] = new List<float>();
                _boundaryEdgesByRow[row] = new List<Vector4>();
            }
        }

        private void ClearBuiltRows()
        {
            if (_builtRows != null)
            {
                System.Array.Clear(_builtRows, 0, _builtRows.Length);
            }
        }

        private static void BuildScanline(
            IReadOnlyList<Vector2> vertices,
            float scanX,
            List<float> crossings,
            List<Vector4> boundaryEdges)
        {
            crossings.Clear();
            boundaryEdges.Clear();

            for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
            {
                Vector2 current = vertices[i];
                Vector2 previous = vertices[j];
                float deltaX = previous.x - current.x;
                float deltaY = previous.y - current.y;

                if ((current.x > scanX) != (previous.x > scanX))
                {
                    float atY =
                        deltaY * (scanX - current.x) / deltaX + current.y;
                    crossings.Add(atY);
                }

                if (scanX >= Mathf.Min(current.x, previous.x) - BoundaryEpsilon &&
                    scanX <= Mathf.Max(current.x, previous.x) + BoundaryEpsilon)
                {
                    boundaryEdges.Add(
                        new Vector4(
                            current.x,
                            current.y,
                            previous.x,
                            previous.y));
                }
            }

            crossings.Sort();
        }

        private static bool IsPointOnBoundary(
            Vector2 point,
            IReadOnlyList<Vector4> boundaryEdges)
        {
            for (int i = 0; i < boundaryEdges.Count; i++)
            {
                Vector4 edge = boundaryEdges[i];
                if (IsPointOnSegment(
                        point,
                        new Vector2(edge.x, edge.y),
                        new Vector2(edge.z, edge.w)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            Vector2 segment = end - start;
            if (Vector2.SqrMagnitude(segment) <= BoundaryEpsilon * BoundaryEpsilon)
            {
                return Vector2.SqrMagnitude(point - start) <=
                       BoundaryEpsilon * BoundaryEpsilon;
            }

            Vector2 offset = point - start;
            float cross = segment.x * offset.y - segment.y * offset.x;
            if (Mathf.Abs(cross) > BoundaryEpsilon)
            {
                return false;
            }

            return point.x >= Mathf.Min(start.x, end.x) - BoundaryEpsilon &&
                   point.x <= Mathf.Max(start.x, end.x) + BoundaryEpsilon &&
                   point.y >= Mathf.Min(start.y, end.y) - BoundaryEpsilon &&
                   point.y <= Mathf.Max(start.y, end.y) + BoundaryEpsilon;
        }

        private static bool IsInsideFromCrossings(
            float pointY,
            IReadOnlyList<float> crossings)
        {
            int lower = 0;
            int upper = crossings.Count;
            while (lower < upper)
            {
                int middle = lower + ((upper - lower) >> 1);
                if (crossings[middle] <= pointY)
                {
                    lower = middle + 1;
                }
                else
                {
                    upper = middle;
                }
            }

            return ((crossings.Count - lower) & 1) == 1;
        }
    }
}
