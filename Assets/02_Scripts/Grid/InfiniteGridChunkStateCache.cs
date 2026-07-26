using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridChunkStateCache
    {
        // 청크 상태는 카메라 범위가 이동해도 재사용하며, 실제 변경된 청크만 무효화한다.
        private readonly Dictionary<GridChunkKey, InfiniteGridChunkState> _chunkStates = new();
        private readonly HashSet<GridChunkKey> _visibleChunkKeys = new();
        private readonly InfiniteGridTerritoryChunkClassifier _territoryClassifier = new();

        private long _chunkUseSequence;

        public void Invalidate()
        {
            _territoryClassifier.Invalidate();
            foreach (InfiniteGridChunkState chunkState in _chunkStates.Values)
            {
                chunkState.TerritoryRevision = -1;
                chunkState.BaseStateRevision = -1;
            }
        }

        public void InvalidateChunks(IEnumerable<GridChunkKey> chunkKeys)
        {
            if (chunkKeys == null)
            {
                return;
            }

            foreach (GridChunkKey key in chunkKeys)
            {
                if (_chunkStates.TryGetValue(key, out InfiniteGridChunkState chunkState))
                {
                    chunkState.BaseStateRevision = -1;
                }
            }
        }

        public void InvalidateWorldBounds(
            Vector2 changedMin,
            Vector2 changedMax,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize)
        {
            if (gridCalculator == null || cellSize <= 0f)
            {
                Invalidate();
                return;
            }

            const float boundaryPadding = 0.0001f;
            changedMin -= Vector2.one * boundaryPadding;
            changedMax += Vector2.one * boundaryPadding;
            _territoryClassifier.Invalidate();

            foreach (InfiniteGridChunkState chunkState in _chunkStates.Values)
            {
                if (HasCellCenterInsideBounds(
                        chunkState.Key,
                        changedMin,
                        changedMax,
                        gridCalculator,
                        gridOrigin,
                        cellSize))
                {
                    chunkState.TerritoryRevision = -1;
                    chunkState.BaseStateRevision = -1;
                }
            }
        }

        public void RebuildVisibleAtlas(
            GridVisibleChunkRange visibleRange,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize,
            Territory territory,
            InfiniteGridOccupancyIndex occupancyIndex,
            Color32[] targetPixels,
            int targetWidth,
            Vector2Int visibleCellMin,
            int maxCachedChunkCount)
        {
            if (gridCalculator == null ||
                occupancyIndex == null ||
                targetPixels == null ||
                targetWidth <= 0)
            {
                return;
            }

            RefreshVisibleChunkKeys(visibleRange);
            for (int chunkY = visibleRange.Min.Y; chunkY <= visibleRange.Max.Y; chunkY++)
            {
                for (int chunkX = visibleRange.Min.X; chunkX <= visibleRange.Max.X; chunkX++)
                {
                    GridChunkKey key = new GridChunkKey(chunkX, chunkY);
                    InfiniteGridChunkState chunkState =
                        GetOrCreateChunkState(key, gridCalculator);
                    if (chunkState.TerritoryRevision < 0)
                    {
                        _territoryClassifier.Rebuild(
                            chunkState,
                            gridCalculator,
                            gridOrigin,
                            cellSize,
                            territory);
                        chunkState.TerritoryRevision = 0;
                        chunkState.BaseStateRevision = -1;
                    }

                    if (chunkState.BaseStateRevision < 0)
                    {
                        RebuildBlockedState(
                            chunkState,
                            gridCalculator,
                            occupancyIndex);
                    }

                    chunkState.LastUseSequence = GetNextChunkUseSequence();
                    CopyChunkStateToAtlas(
                        chunkState,
                        gridCalculator,
                        targetPixels,
                        targetWidth,
                        visibleCellMin);
                }
            }

            EvictUnusedChunkStates(maxCachedChunkCount);
        }

        public void Clear()
        {
            _chunkStates.Clear();
            _visibleChunkKeys.Clear();
            _territoryClassifier.Invalidate();
            _chunkUseSequence = 0;
        }

        private void RefreshVisibleChunkKeys(GridVisibleChunkRange visibleRange)
        {
            _visibleChunkKeys.Clear();
            for (int chunkY = visibleRange.Min.Y; chunkY <= visibleRange.Max.Y; chunkY++)
            {
                for (int chunkX = visibleRange.Min.X; chunkX <= visibleRange.Max.X; chunkX++)
                {
                    _visibleChunkKeys.Add(new GridChunkKey(chunkX, chunkY));
                }
            }
        }

        private static bool HasCellCenterInsideBounds(
            GridChunkKey key,
            Vector2 changedMin,
            Vector2 changedMax,
            GridCalculator gridCalculator,
            Vector3 gridOrigin,
            float cellSize)
        {
            int chunkColumnCount = gridCalculator.ChunkColumnCount;
            int chunkRowCount = gridCalculator.ChunkRowCount;
            Vector2Int chunkMin = gridCalculator.GetChunkMinCellIndex(key);

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
                    if (center.x >= changedMin.x &&
                        center.x <= changedMax.x &&
                        center.z >= changedMin.y &&
                        center.z <= changedMax.y)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private InfiniteGridChunkState GetOrCreateChunkState(
            GridChunkKey key,
            GridCalculator gridCalculator)
        {
            int cellCount =
                gridCalculator.ChunkColumnCount * gridCalculator.ChunkRowCount;
            if (_chunkStates.TryGetValue(key, out InfiniteGridChunkState chunkState) &&
                chunkState.BlockedCells.Length == cellCount &&
                chunkState.TerritoryCells.Length == cellCount)
            {
                return chunkState;
            }

            chunkState = new InfiniteGridChunkState(key, cellCount);
            _chunkStates[key] = chunkState;
            return chunkState;
        }

        private static void RebuildBlockedState(
            InfiniteGridChunkState chunkState,
            GridCalculator gridCalculator,
            InfiniteGridOccupancyIndex occupancyIndex)
        {
            int chunkColumnCount = gridCalculator.ChunkColumnCount;
            int chunkRowCount = gridCalculator.ChunkRowCount;
            Vector2Int chunkMin = gridCalculator.GetChunkMinCellIndex(chunkState.Key);

            for (int localRow = 0; localRow < chunkRowCount; localRow++)
            {
                int row = chunkMin.y + localRow;
                for (int localCol = 0; localCol < chunkColumnCount; localCol++)
                {
                    int col = chunkMin.x + localCol;
                    Vector2Int index = new Vector2Int(col, row);
                    int localIndex = localRow * chunkColumnCount + localCol;
                    bool isBlocked =
                        chunkState.TerritoryCells[localIndex] == 0 ||
                        occupancyIndex.Contains(index);
                    chunkState.BlockedCells[localIndex] =
                        isBlocked ? byte.MaxValue : (byte)0;
                }
            }

            chunkState.BaseStateRevision = 0;
        }

        private void CopyChunkStateToAtlas(
            InfiniteGridChunkState chunkState,
            GridCalculator gridCalculator,
            Color32[] targetPixels,
            int targetWidth,
            Vector2Int visibleCellMin)
        {
            int chunkColumnCount = gridCalculator.ChunkColumnCount;
            int chunkRowCount = gridCalculator.ChunkRowCount;
            Vector2Int chunkMin = gridCalculator.GetChunkMinCellIndex(chunkState.Key);
            int atlasStartColumn = chunkMin.x - visibleCellMin.x;
            int atlasStartRow = chunkMin.y - visibleCellMin.y;

            for (int localRow = 0; localRow < chunkRowCount; localRow++)
            {
                int sourceStart = localRow * chunkColumnCount;
                int targetStart =
                    (atlasStartRow + localRow) * targetWidth + atlasStartColumn;
                for (int localCol = 0; localCol < chunkColumnCount; localCol++)
                {
                    int targetIndex = targetStart + localCol;
                    Color32 pixel = targetPixels[targetIndex];
                    pixel.r = chunkState.BlockedCells[sourceStart + localCol];
                    pixel.a = byte.MaxValue;
                    targetPixels[targetIndex] = pixel;
                }
            }
        }

        private long GetNextChunkUseSequence()
        {
            if (_chunkUseSequence == long.MaxValue)
            {
                _chunkUseSequence = 0;
                foreach (InfiniteGridChunkState chunkState in _chunkStates.Values)
                {
                    chunkState.LastUseSequence = 0;
                }
            }

            _chunkUseSequence++;
            return _chunkUseSequence;
        }

        private void EvictUnusedChunkStates(int maxCachedChunkCount)
        {
            int cacheLimit = Mathf.Max(maxCachedChunkCount, _visibleChunkKeys.Count);
            while (_chunkStates.Count > cacheLimit)
            {
                bool foundCandidate = false;
                GridChunkKey oldestKey = default;
                long oldestUseSequence = long.MaxValue;

                foreach (KeyValuePair<GridChunkKey, InfiniteGridChunkState> pair in _chunkStates)
                {
                    if (_visibleChunkKeys.Contains(pair.Key) ||
                        pair.Value.LastUseSequence >= oldestUseSequence)
                    {
                        continue;
                    }

                    foundCandidate = true;
                    oldestKey = pair.Key;
                    oldestUseSequence = pair.Value.LastUseSequence;
                }

                if (!foundCandidate)
                {
                    return;
                }

                _chunkStates.Remove(oldestKey);
            }
        }
    }
}
