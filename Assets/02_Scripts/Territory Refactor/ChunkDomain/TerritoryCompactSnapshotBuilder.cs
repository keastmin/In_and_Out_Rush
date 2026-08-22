using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactSnapshotBuilder
    {
        public long LastSourceChunkScanCount { get; private set; }
        public long LastSourceBoundarySegmentScanCount { get; private set; }

        public bool TryBuild(
            TerritoryChunkSnapshot source,
            out TerritoryCompactSnapshot snapshot,
            out string reason)
        {
            snapshot = null;
            reason = string.Empty;
            LastSourceChunkScanCount = 0L;
            LastSourceBoundarySegmentScanCount = 0L;
            if (source == null)
            {
                reason = "Initial compact conversion requires a C006 snapshot.";
                return false;
            }
            if (source.Revision == 0)
            {
                reason = "An uninitialized C006 snapshot cannot produce compact Territory state.";
                return false;
            }
            if (!TerritoryBoundaryLoopIndex.TryCreate(source, out TerritoryBoundaryLoopIndex legacyIndex, out reason))
                return false;

            var sourceSegments = new Dictionary<int, SourceSegment>();
            var fullXsByRow = new SortedDictionary<int, List<int>>();
            var centerInsideByChunk = new Dictionary<TerritoryChunkCoordinate, bool>();
            try
            {
                foreach (KeyValuePair<TerritoryChunkCoordinate, TerritoryChunkCoverage> pair in source.Chunks)
                {
                    LastSourceChunkScanCount++;
                    TerritoryChunkCoverage coverage = pair.Value;
                    if (coverage.Fill == TerritoryChunkFill.Full)
                    {
                        if (!fullXsByRow.TryGetValue(pair.Key.Y, out List<int> xs))
                        {
                            xs = new List<int>();
                            fullXsByRow.Add(pair.Key.Y, xs);
                        }
                        xs.Add(pair.Key.X);
                        continue;
                    }
                    if (coverage.Fill != TerritoryChunkFill.Boundary)
                        continue;

                    centerInsideByChunk[pair.Key] = coverage.CenterInside;
                    for (int i = 0; i < coverage.Segments.Count; i++)
                    {
                        TerritoryChunkBoundarySegment segment = coverage.Segments[i];
                        LastSourceBoundarySegmentScanCount++;
                        sourceSegments.Add(segment.Sequence, new SourceSegment(
                            pair.Key,
                            segment.Start,
                            segment.End));
                    }
                }
            }
            catch (ArgumentException)
            {
                reason = "C006 Boundary sequences are duplicated during compact conversion.";
                return false;
            }

            int boundaryCount = legacyIndex.SegmentCount;
            if (sourceSegments.Count != boundaryCount)
            {
                reason = "C006 Boundary index and source segment counts disagree.";
                return false;
            }

            var ordered = new List<TerritoryCompactBoundarySegment>(boundaryCount);
            bool reverse = legacyIndex.SignedTwiceArea < 0m;
            for (int ordinal = 0; ordinal < boundaryCount; ordinal++)
            {
                int sequence = reverse ? boundaryCount - 1 - ordinal : ordinal;
                SourceSegment sourceSegment = sourceSegments[sequence];
                TerritoryChunkLocalPoint start = reverse ? sourceSegment.End : sourceSegment.Start;
                TerritoryChunkLocalPoint end = reverse ? sourceSegment.Start : sourceSegment.End;
                ordered.Add(new TerritoryCompactBoundarySegment(
                    new TerritoryBoundarySegmentId((ulong)ordinal + 1UL),
                    sourceSegment.Chunk,
                    start,
                    end));
            }

            if (!TerritoryPersistentBoundaryTree.TryCreate(ordered, out TerritoryPersistentBoundaryTree boundary, out reason))
                return false;

            var rows = new TerritoryPersistentAvlMap<int, TerritoryCompactRowCoverage>();
            foreach (KeyValuePair<int, List<int>> pair in fullXsByRow)
            {
                pair.Value.Sort();
                var runs = new List<TerritoryChunkFillRun>();
                int minimum = pair.Value[0];
                int maximum = minimum;
                for (int i = 1; i < pair.Value.Count; i++)
                {
                    int x = pair.Value[i];
                    if (maximum != int.MaxValue && x == maximum + 1)
                        maximum = x;
                    else if (x != maximum)
                    {
                        runs.Add(new TerritoryChunkFillRun(pair.Key, minimum, maximum));
                        minimum = maximum = x;
                    }
                }
                runs.Add(new TerritoryChunkFillRun(pair.Key, minimum, maximum));
                rows = rows.Set(pair.Key, new TerritoryCompactRowCoverage(pair.Key, runs), out _, out _);
            }

            var actualMutable = new Dictionary<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>>();
            var candidateMutable = new Dictionary<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>>();
            for (int i = 0; i < ordered.Count; i++)
            {
                TerritoryCompactBoundarySegment segment = ordered[i];
                AddMutable(actualMutable, segment.Chunk, segment.Id);
                AddCandidateNeighborhood(candidateMutable, segment.Chunk, segment.Id);
            }

            var actual = new TerritoryPersistentAvlMap<TerritoryChunkCoordinate, TerritoryCompactSnapshot.BoundaryBucket>(
                ChunkComparer.Instance);
            foreach (KeyValuePair<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>> pair in actualMutable)
            {
                bool centerInside = centerInsideByChunk.TryGetValue(pair.Key, out bool value) && value;
                actual = actual.Set(
                    pair.Key,
                    new TerritoryCompactSnapshot.BoundaryBucket(centerInside, pair.Value.ToArray()),
                    out _,
                    out _);
            }

            var candidates = new TerritoryPersistentAvlMap<TerritoryChunkCoordinate, TerritoryCompactSnapshot.CandidateBucket>(
                ChunkComparer.Instance);
            foreach (KeyValuePair<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>> pair in candidateMutable)
            {
                candidates = candidates.Set(
                    pair.Key,
                    new TerritoryCompactSnapshot.CandidateBucket(pair.Value.ToArray()),
                    out _,
                    out _);
            }

            if ((ulong)boundaryCount == ulong.MaxValue)
            {
                reason = "Boundary identity range is exhausted.";
                return false;
            }

            snapshot = new TerritoryCompactSnapshot(
                source.Revision,
                boundary,
                rows,
                actual,
                candidates,
                (ulong)boundaryCount + 1UL);
            return true;
        }

        private static void AddMutable(
            Dictionary<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>> map,
            TerritoryChunkCoordinate chunk,
            TerritoryBoundarySegmentId id)
        {
            if (!map.TryGetValue(chunk, out List<TerritoryBoundarySegmentId> ids))
            {
                ids = new List<TerritoryBoundarySegmentId>();
                map.Add(chunk, ids);
            }
            ids.Add(id);
        }

        private static void AddCandidateNeighborhood(
            Dictionary<TerritoryChunkCoordinate, List<TerritoryBoundarySegmentId>> map,
            TerritoryChunkCoordinate source,
            TerritoryBoundarySegmentId id)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int x = -1; x <= 1; x++)
                {
                    long candidateX = (long)source.X + x;
                    long candidateY = (long)source.Y + y;
                    if (candidateX < int.MinValue || candidateX > int.MaxValue ||
                        candidateY < int.MinValue || candidateY > int.MaxValue)
                    {
                        continue;
                    }
                    AddMutable(map, new TerritoryChunkCoordinate((int)candidateX, (int)candidateY), id);
                }
            }
        }

        private readonly struct SourceSegment
        {
            public SourceSegment(
                TerritoryChunkCoordinate chunk,
                TerritoryChunkLocalPoint start,
                TerritoryChunkLocalPoint end)
            {
                Chunk = chunk;
                Start = start;
                End = end;
            }

            public TerritoryChunkCoordinate Chunk { get; }
            public TerritoryChunkLocalPoint Start { get; }
            public TerritoryChunkLocalPoint End { get; }
        }

        internal sealed class ChunkComparer : IComparer<TerritoryChunkCoordinate>
        {
            public static readonly ChunkComparer Instance = new();

            public int Compare(TerritoryChunkCoordinate left, TerritoryChunkCoordinate right)
            {
                int y = left.Y.CompareTo(right.Y);
                return y != 0 ? y : left.X.CompareTo(right.X);
            }
        }
    }
}
