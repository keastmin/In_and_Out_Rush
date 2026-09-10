using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridTerritoryChunkClassifier
    {
        public void Invalidate()
        {
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

            if (territory == null)
            {
                return;
            }

            int chunkColumnCount = gridCalculator.ChunkColumnCount;
            int chunkRowCount = gridCalculator.ChunkRowCount;
            Vector2Int chunkMin = gridCalculator.GetChunkMinCellIndex(chunkState.Key);

            for (int localRow = 0; localRow < chunkRowCount; localRow++)
            {
                int row = chunkMin.y + localRow;
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
                    chunkState.TerritoryCells[localRow * chunkColumnCount + localCol] =
                        territory.IsPointInPolygon(centerPoint)
                            ? byte.MaxValue
                            : (byte)0;
                }
            }
        }
    }
}
