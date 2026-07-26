using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class InfiniteGridOccupancyIndex
    {
        private readonly HashSet<Vector2Int> _occupiedCells = new();
        private readonly Dictionary<Vector2Int, int> _occupiedCellRefCounts = new();
        private readonly Dictionary<Vector2Int, int> _networkCellRanges = new();
        private readonly Dictionary<Vector2Int, int> _incomingNetworkCellRanges = new();
        private readonly HashSet<Vector2Int> _additionalOccupiedCells = new();
        private readonly HashSet<Vector2Int> _incomingAdditionalOccupiedCells = new();
        private readonly HashSet<GridChunkKey> _changedChunks = new();

        public IEnumerable<GridChunkKey> ChangedChunks => _changedChunks;

        public bool Contains(Vector2Int index)
        {
            return _occupiedCells.Contains(index);
        }

        public bool Rebuild(
            GridCalculator gridCalculator,
            IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid,
            IEnumerable<Vector2Int> additionalOccupiedIndices)
        {
            _changedChunks.Clear();
            if (gridCalculator == null)
            {
                bool hadOccupiedCells = _occupiedCells.Count > 0;
                Clear();
                return hadOccupiedCells;
            }

            _incomingNetworkCellRanges.Clear();
            if (networkGrid != null)
            {
                foreach (KeyValuePair<Vector2Int, CellData> pair in networkGrid)
                {
                    _incomingNetworkCellRanges[pair.Key] = Mathf.Max(0, pair.Value.ActiveRange);
                }
            }

            foreach (KeyValuePair<Vector2Int, int> pair in _networkCellRanges)
            {
                if (!_incomingNetworkCellRanges.TryGetValue(pair.Key, out int incomingRange) ||
                    incomingRange != pair.Value)
                {
                    AdjustOccupiedRange(gridCalculator, pair.Key, pair.Value, -1);
                }
            }

            foreach (KeyValuePair<Vector2Int, int> pair in _incomingNetworkCellRanges)
            {
                if (!_networkCellRanges.TryGetValue(pair.Key, out int previousRange) ||
                    previousRange != pair.Value)
                {
                    AdjustOccupiedRange(gridCalculator, pair.Key, pair.Value, 1);
                }
            }

            _networkCellRanges.Clear();
            foreach (KeyValuePair<Vector2Int, int> pair in _incomingNetworkCellRanges)
            {
                _networkCellRanges.Add(pair.Key, pair.Value);
            }

            _incomingAdditionalOccupiedCells.Clear();
            if (additionalOccupiedIndices != null)
            {
                foreach (Vector2Int occupiedIndex in additionalOccupiedIndices)
                {
                    _incomingAdditionalOccupiedCells.Add(occupiedIndex);
                }
            }

            foreach (Vector2Int occupiedIndex in _additionalOccupiedCells)
            {
                if (!_incomingAdditionalOccupiedCells.Contains(occupiedIndex))
                {
                    AdjustOccupiedCell(gridCalculator, occupiedIndex, -1);
                }
            }

            foreach (Vector2Int occupiedIndex in _incomingAdditionalOccupiedCells)
            {
                if (!_additionalOccupiedCells.Contains(occupiedIndex))
                {
                    AdjustOccupiedCell(gridCalculator, occupiedIndex, 1);
                }
            }

            _additionalOccupiedCells.Clear();
            _additionalOccupiedCells.UnionWith(_incomingAdditionalOccupiedCells);
            return _changedChunks.Count > 0;
        }

        public void Clear()
        {
            _occupiedCells.Clear();
            _occupiedCellRefCounts.Clear();
            _networkCellRanges.Clear();
            _incomingNetworkCellRanges.Clear();
            _additionalOccupiedCells.Clear();
            _incomingAdditionalOccupiedCells.Clear();
            _changedChunks.Clear();
        }

        private void AdjustOccupiedRange(
            GridCalculator gridCalculator,
            Vector2Int centerIndex,
            int range,
            int delta)
        {
            List<Vector2Int> indicesInRange =
                gridCalculator.GetInRangeIndices(centerIndex, range);
            for (int i = 0; i < indicesInRange.Count; i++)
            {
                AdjustOccupiedCell(gridCalculator, indicesInRange[i], delta);
            }
        }

        private void AdjustOccupiedCell(
            GridCalculator gridCalculator,
            Vector2Int index,
            int delta)
        {
            _occupiedCellRefCounts.TryGetValue(index, out int referenceCount);
            int nextReferenceCount = referenceCount + delta;
            bool wasOccupied = referenceCount > 0;
            bool isOccupied = nextReferenceCount > 0;
            if (wasOccupied != isOccupied)
            {
                _changedChunks.Add(gridCalculator.GetChunkKeyFromCellIndex(index));
            }

            if (nextReferenceCount <= 0)
            {
                _occupiedCellRefCounts.Remove(index);
                _occupiedCells.Remove(index);
                return;
            }

            _occupiedCellRefCounts[index] = nextReferenceCount;
            _occupiedCells.Add(index);
        }
    }
}
