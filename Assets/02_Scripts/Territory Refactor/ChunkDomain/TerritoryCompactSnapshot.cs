using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactSnapshot
    {
        private readonly TerritoryPersistentAvlMap<int, TerritoryCompactRowCoverage> _rows;
        private readonly TerritoryPersistentAvlMap<TerritoryChunkCoordinate, BoundaryBucket> _boundaryByChunk;
        private readonly TerritoryPersistentAvlMap<TerritoryChunkCoordinate, CandidateBucket> _candidatesByChunk;

        internal TerritoryCompactSnapshot(
            ulong revision,
            TerritoryPersistentBoundaryTree boundary,
            TerritoryPersistentAvlMap<int, TerritoryCompactRowCoverage> rows,
            TerritoryPersistentAvlMap<TerritoryChunkCoordinate, BoundaryBucket> boundaryByChunk,
            TerritoryPersistentAvlMap<TerritoryChunkCoordinate, CandidateBucket> candidatesByChunk,
            ulong nextBoundaryIdentity)
        {
            if (revision == 0)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (nextBoundaryIdentity == 0UL)
                throw new ArgumentOutOfRangeException(nameof(nextBoundaryIdentity));

            Revision = revision;
            Boundary = boundary ?? throw new ArgumentNullException(nameof(boundary));
            _rows = rows ?? throw new ArgumentNullException(nameof(rows));
            _boundaryByChunk = boundaryByChunk ?? throw new ArgumentNullException(nameof(boundaryByChunk));
            _candidatesByChunk = candidatesByChunk ?? throw new ArgumentNullException(nameof(candidatesByChunk));
            NextBoundaryIdentity = nextBoundaryIdentity;
        }

        public ulong Revision { get; }
        public TerritoryPersistentBoundaryTree Boundary { get; }
        public int FullRowCount => _rows.Count;
        public int BoundaryChunkCount => _boundaryByChunk.Count;
        public ulong NextBoundaryIdentity { get; }
        public object FullRowRootIdentity => _rows.RootIdentity;
        public object BoundaryChunkRootIdentity => _boundaryByChunk.RootIdentity;
        public object BoundaryCandidateRootIdentity => _candidatesByChunk.RootIdentity;

        public TerritoryChunkFill GetFill(TerritoryChunkCoordinate chunk)
        {
            if (_boundaryByChunk.TryGetValue(chunk, out _))
                return TerritoryChunkFill.Boundary;
            return _rows.TryGetValue(chunk.Y, out TerritoryCompactRowCoverage row) && row.Contains(chunk.X)
                ? TerritoryChunkFill.Full
                : TerritoryChunkFill.Empty;
        }

        public bool TryGetFullRow(int y, out TerritoryCompactRowCoverage row)
            => _rows.TryGetValue(y, out row);

        public bool TryGetBoundarySegments(
            TerritoryChunkCoordinate chunk,
            out IReadOnlyList<TerritoryCompactBoundarySegment> segments)
        {
            if (!_boundaryByChunk.TryGetValue(chunk, out BoundaryBucket bucket))
            {
                segments = Array.Empty<TerritoryCompactBoundarySegment>();
                return false;
            }

            var resolved = new TerritoryCompactBoundarySegment[bucket.Ids.Length];
            for (int i = 0; i < bucket.Ids.Length; i++)
            {
                if (!Boundary.TryGetSegment(bucket.Ids[i], out resolved[i]))
                    throw new InvalidOperationException("Boundary Chunk index contains a stale identity.");
            }

            segments = resolved;
            return true;
        }

        public bool TryGetBoundaryCenterInside(TerritoryChunkCoordinate chunk, out bool centerInside)
        {
            if (_boundaryByChunk.TryGetValue(chunk, out BoundaryBucket bucket))
            {
                centerInside = bucket.CenterInside;
                return true;
            }

            centerInside = false;
            return false;
        }

        internal bool TryGetCandidateIds(
            TerritoryChunkCoordinate chunk,
            out IReadOnlyList<TerritoryBoundarySegmentId> ids)
        {
            if (_candidatesByChunk.TryGetValue(chunk, out CandidateBucket bucket))
            {
                ids = bucket.Ids;
                return true;
            }

            ids = Array.Empty<TerritoryBoundarySegmentId>();
            return false;
        }

        internal TerritoryPersistentAvlMap<int, TerritoryCompactRowCoverage> Rows => _rows;
        internal TerritoryPersistentAvlMap<TerritoryChunkCoordinate, BoundaryBucket> BoundaryByChunk => _boundaryByChunk;
        internal TerritoryPersistentAvlMap<TerritoryChunkCoordinate, CandidateBucket> CandidatesByChunk => _candidatesByChunk;

        internal sealed class BoundaryBucket : IEquatable<BoundaryBucket>
        {
            public BoundaryBucket(bool centerInside, TerritoryBoundarySegmentId[] ids)
            {
                CenterInside = centerInside;
                Ids = ids ?? throw new ArgumentNullException(nameof(ids));
                if (ids.Length == 0)
                    throw new ArgumentException("A Boundary bucket requires at least one identity.", nameof(ids));
            }

            public bool CenterInside { get; }
            public TerritoryBoundarySegmentId[] Ids { get; }

            public BoundaryBucket Add(TerritoryBoundarySegmentId id, bool centerInside)
            {
                for (int i = 0; i < Ids.Length; i++)
                {
                    if (Ids[i] == id)
                        return CenterInside == centerInside ? this : new BoundaryBucket(centerInside, Ids);
                }

                var copy = new TerritoryBoundarySegmentId[Ids.Length + 1];
                Array.Copy(Ids, copy, Ids.Length);
                copy[Ids.Length] = id;
                return new BoundaryBucket(centerInside, copy);
            }

            public BoundaryBucket WithCenterInside(bool centerInside)
                => CenterInside == centerInside ? this : new BoundaryBucket(centerInside, Ids);

            public BoundaryBucket Remove(TerritoryBoundarySegmentId id)
            {
                int index = Array.IndexOf(Ids, id);
                if (index < 0)
                    return this;
                if (Ids.Length == 1)
                    return null;

                var copy = new TerritoryBoundarySegmentId[Ids.Length - 1];
                if (index > 0)
                    Array.Copy(Ids, 0, copy, 0, index);
                if (index < Ids.Length - 1)
                    Array.Copy(Ids, index + 1, copy, index, Ids.Length - index - 1);
                return new BoundaryBucket(CenterInside, copy);
            }

            public bool Equals(BoundaryBucket other)
            {
                if (ReferenceEquals(null, other) || CenterInside != other.CenterInside || Ids.Length != other.Ids.Length)
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                for (int i = 0; i < Ids.Length; i++)
                    if (Ids[i] != other.Ids[i])
                        return false;
                return true;
            }

            public override bool Equals(object obj) => Equals(obj as BoundaryBucket);
            public override int GetHashCode() => Ids.Length;
        }

        internal sealed class CandidateBucket : IEquatable<CandidateBucket>
        {
            public CandidateBucket(TerritoryBoundarySegmentId[] ids)
            {
                Ids = ids ?? throw new ArgumentNullException(nameof(ids));
                if (ids.Length == 0)
                    throw new ArgumentException("A candidate bucket requires at least one identity.", nameof(ids));
            }

            public TerritoryBoundarySegmentId[] Ids { get; }

            public CandidateBucket Add(TerritoryBoundarySegmentId id)
            {
                for (int i = 0; i < Ids.Length; i++)
                    if (Ids[i] == id)
                        return this;
                var copy = new TerritoryBoundarySegmentId[Ids.Length + 1];
                Array.Copy(Ids, copy, Ids.Length);
                copy[Ids.Length] = id;
                return new CandidateBucket(copy);
            }

            public CandidateBucket Remove(TerritoryBoundarySegmentId id)
            {
                int index = Array.IndexOf(Ids, id);
                if (index < 0)
                    return this;
                if (Ids.Length == 1)
                    return null;
                var copy = new TerritoryBoundarySegmentId[Ids.Length - 1];
                if (index > 0)
                    Array.Copy(Ids, 0, copy, 0, index);
                if (index < Ids.Length - 1)
                    Array.Copy(Ids, index + 1, copy, index, Ids.Length - index - 1);
                return new CandidateBucket(copy);
            }

            public bool Equals(CandidateBucket other)
            {
                if (ReferenceEquals(null, other) || Ids.Length != other.Ids.Length)
                    return false;
                if (ReferenceEquals(this, other))
                    return true;
                for (int i = 0; i < Ids.Length; i++)
                    if (Ids[i] != other.Ids[i])
                        return false;
                return true;
            }

            public override bool Equals(object obj) => Equals(obj as CandidateBucket);
            public override int GetHashCode() => Ids.Length;
        }
    }
}
