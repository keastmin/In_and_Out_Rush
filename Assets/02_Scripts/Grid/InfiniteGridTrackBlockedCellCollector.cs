using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTrackBlockedCellCollector
    {
        private const int CandidateCellPadding = 2;

        public void Collect(
            ISet<Vector2Int> blockedCellIndices,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize,
            IReadOnlyList<Vector3> trackVertices,
            float overlapRadius,
            InfiniteGridTrackOverlapTester overlapTester)
        {
            if (blockedCellIndices == null ||
                gridCalculator == null ||
                trackVertices == null ||
                trackVertices.Count < 2 ||
                overlapTester == null)
            {
                return;
            }

            float safeCellSize = Mathf.Max(0.001f, cellSize);
            float safeRadius = Mathf.Max(0f, overlapRadius);
            float candidateExtent = safeCellSize + safeRadius;

            for (int i = 0; i < trackVertices.Count; i++)
            {
                Vector3 start = trackVertices[i];
                Vector3 end = trackVertices[(i + 1) % trackVertices.Count];
                CollectSegmentCells(
                    blockedCellIndices,
                    gridCalculator,
                    gridOrigin,
                    safeCellSize,
                    start,
                    end,
                    safeRadius,
                    candidateExtent,
                    overlapTester);
            }
        }

        private static void CollectSegmentCells(
            ISet<Vector2Int> blockedCellIndices,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize,
            Vector3 trackStart,
            Vector3 trackEnd,
            float overlapRadius,
            float candidateExtent,
            InfiniteGridTrackOverlapTester overlapTester)
        {
            float minimumX =
                Mathf.Min(trackStart.x, trackEnd.x) - candidateExtent;
            float maximumX =
                Mathf.Max(trackStart.x, trackEnd.x) + candidateExtent;
            float minimumZ =
                Mathf.Min(trackStart.z, trackEnd.z) - candidateExtent;
            float maximumZ =
                Mathf.Max(trackStart.z, trackEnd.z) + candidateExtent;

            Vector2Int firstCorner =
                gridCalculator.GetNearestCellIndexFromWorldPosition(
                    gridOrigin,
                    new Vector3(minimumX, gridOrigin.y, minimumZ),
                    cellSize);
            int minimumColumn = firstCorner.x;
            int maximumColumn = firstCorner.x;
            int minimumRow = firstCorner.y;
            int maximumRow = firstCorner.y;

            IncludeCorner(
                gridCalculator,
                gridOrigin,
                cellSize,
                minimumX,
                maximumZ,
                ref minimumColumn,
                ref maximumColumn,
                ref minimumRow,
                ref maximumRow);
            IncludeCorner(
                gridCalculator,
                gridOrigin,
                cellSize,
                maximumX,
                minimumZ,
                ref minimumColumn,
                ref maximumColumn,
                ref minimumRow,
                ref maximumRow);
            IncludeCorner(
                gridCalculator,
                gridOrigin,
                cellSize,
                maximumX,
                maximumZ,
                ref minimumColumn,
                ref maximumColumn,
                ref minimumRow,
                ref maximumRow);

            minimumColumn -= CandidateCellPadding;
            maximumColumn += CandidateCellPadding;
            minimumRow -= CandidateCellPadding;
            maximumRow += CandidateCellPadding;

            for (int column = minimumColumn; column <= maximumColumn; column++)
            {
                for (int row = minimumRow; row <= maximumRow; row++)
                {
                    Vector2Int cellIndex = new Vector2Int(column, row);
                    if (blockedCellIndices.Contains(cellIndex))
                    {
                        continue;
                    }

                    Vector3 cellCenter =
                        gridCalculator.GetCellCenterPositionFromCellIndex(
                            gridOrigin,
                            column,
                            row,
                            cellSize);
                    if (overlapTester.IsSegmentOverlapping(
                            cellCenter,
                            cellSize,
                            trackStart,
                            trackEnd,
                            overlapRadius))
                    {
                        blockedCellIndices.Add(cellIndex);
                    }
                }
            }
        }

        private static void IncludeCorner(
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize,
            float worldX,
            float worldZ,
            ref int minimumColumn,
            ref int maximumColumn,
            ref int minimumRow,
            ref int maximumRow)
        {
            Vector2Int corner =
                gridCalculator.GetNearestCellIndexFromWorldPosition(
                    gridOrigin,
                    new Vector3(worldX, gridOrigin.y, worldZ),
                    cellSize);
            minimumColumn = Mathf.Min(minimumColumn, corner.x);
            maximumColumn = Mathf.Max(maximumColumn, corner.x);
            minimumRow = Mathf.Min(minimumRow, corner.y);
            maximumRow = Mathf.Max(maximumRow, corner.y);
        }
    }
}
