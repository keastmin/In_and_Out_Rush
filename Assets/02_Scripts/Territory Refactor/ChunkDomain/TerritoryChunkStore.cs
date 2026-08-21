using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkStore
    {
        private readonly TerritoryChunkStateBuilder _builder = new();
        private readonly HashSet<TerritoryChunkCoordinate> _changedCoordinateSet = new();
        private readonly List<TerritoryChunkCoordinate> _changedCoordinates = new();
        private readonly List<TerritoryChunkCoverage> _changedCoverage = new();

        public TerritoryChunkStore()
            : this(new TerritoryChunkSnapshot(
                0,
                new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>()))
        {
        }

        public TerritoryChunkStore(TerritoryChunkSnapshot initialSnapshot)
        {
            Current = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
        }

        public TerritoryChunkSnapshot Current { get; private set; }

        public bool TryCommit(
            ulong expectedBaseRevision,
            IReadOnlyList<FixedTerritoryPoint> polygon,
            out TerritoryChunkCommitResult result,
            out string reason)
        {
            result = null;
            if (expectedBaseRevision != Current.Revision)
            {
                reason = $"Expected base revision {Current.Revision}, received {expectedBaseRevision}.";
                return false;
            }
            if (Current.Revision == ulong.MaxValue)
            {
                reason = "Territory Chunk revision exhausted UInt64 range.";
                return false;
            }

            ulong nextRevision = Current.Revision + 1UL;
            if (!_builder.TryBuild(polygon, nextRevision, out TerritoryChunkSnapshot candidate, out reason))
                return false;

            BuildDelta(Current, candidate);
            var commitResult = new TerritoryChunkCommitResult(
                Current.Revision,
                nextRevision,
                _changedCoverage);

            Current = candidate;
            result = commitResult;
            reason = null;
            return true;
        }

        private void BuildDelta(
            TerritoryChunkSnapshot previous,
            TerritoryChunkSnapshot current)
        {
            _changedCoordinateSet.Clear();
            _changedCoordinates.Clear();
            _changedCoverage.Clear();

            foreach (TerritoryChunkCoordinate chunk in previous.Chunks.Keys)
                _changedCoordinateSet.Add(chunk);
            foreach (TerritoryChunkCoordinate chunk in current.Chunks.Keys)
                _changedCoordinateSet.Add(chunk);

            _changedCoordinates.AddRange(_changedCoordinateSet);
            _changedCoordinates.Sort(CompareCoordinates);
            for (int i = 0; i < _changedCoordinates.Count; i++)
            {
                TerritoryChunkCoordinate chunk = _changedCoordinates[i];
                bool hadPrevious = previous.TryGetCoverage(chunk, out TerritoryChunkCoverage oldCoverage);
                bool hasCurrent = current.TryGetCoverage(chunk, out TerritoryChunkCoverage newCoverage);
                if (hadPrevious && hasCurrent && oldCoverage.Equals(newCoverage))
                    continue;
                if (!hadPrevious && !hasCurrent)
                    continue;

                _changedCoverage.Add(
                    hasCurrent ? newCoverage : TerritoryChunkCoverage.Empty(chunk));
            }
        }

        private static int CompareCoordinates(
            TerritoryChunkCoordinate left,
            TerritoryChunkCoordinate right)
        {
            int y = left.Y.CompareTo(right.Y);
            return y != 0 ? y : left.X.CompareTo(right.X);
        }
    }
}
