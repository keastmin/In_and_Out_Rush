using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkCoverage : IEquatable<TerritoryChunkCoverage>
    {
        private static readonly ReadOnlyCollection<TerritoryChunkBoundarySegment> NoSegments =
            Array.AsReadOnly(Array.Empty<TerritoryChunkBoundarySegment>());

        private readonly ReadOnlyCollection<TerritoryChunkBoundarySegment> _segments;

        private TerritoryChunkCoverage(
            TerritoryChunkCoordinate chunk,
            TerritoryChunkFill fill,
            bool centerInside,
            IReadOnlyList<TerritoryChunkBoundarySegment> segments)
        {
            Chunk = chunk;
            Fill = fill;
            CenterInside = centerInside;

            if (fill == TerritoryChunkFill.Boundary)
            {
                if (segments == null || segments.Count == 0)
                    throw new ArgumentException("Boundary coverage requires at least one segment.", nameof(segments));

                var copy = new TerritoryChunkBoundarySegment[segments.Count];
                for (int i = 0; i < segments.Count; i++)
                    copy[i] = segments[i];
                _segments = Array.AsReadOnly(copy);
            }
            else
            {
                if (segments != null && segments.Count > 0)
                    throw new ArgumentException("Empty and Full coverage cannot contain boundary segments.", nameof(segments));
                _segments = NoSegments;
            }
        }

        public TerritoryChunkCoordinate Chunk { get; }
        public TerritoryChunkFill Fill { get; }
        public bool CenterInside { get; }
        public IReadOnlyList<TerritoryChunkBoundarySegment> Segments => _segments;

        public static TerritoryChunkCoverage Empty(TerritoryChunkCoordinate chunk)
            => new(chunk, TerritoryChunkFill.Empty, false, null);

        public static TerritoryChunkCoverage Full(TerritoryChunkCoordinate chunk)
            => new(chunk, TerritoryChunkFill.Full, true, null);

        public static TerritoryChunkCoverage Boundary(
            TerritoryChunkCoordinate chunk,
            bool centerInside,
            IReadOnlyList<TerritoryChunkBoundarySegment> segments)
            => new(chunk, TerritoryChunkFill.Boundary, centerInside, segments);

        public bool Equals(TerritoryChunkCoverage other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            if (Chunk != other.Chunk || Fill != other.Fill || CenterInside != other.CenterInside ||
                _segments.Count != other._segments.Count)
            {
                return false;
            }

            for (int i = 0; i < _segments.Count; i++)
            {
                if (_segments[i] != other._segments[i])
                    return false;
            }

            return true;
        }

        public override bool Equals(object obj)
            => Equals(obj as TerritoryChunkCoverage);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Chunk.GetHashCode();
                hash = (hash * 397) ^ (int)Fill;
                hash = (hash * 397) ^ CenterInside.GetHashCode();
                for (int i = 0; i < _segments.Count; i++)
                    hash = (hash * 397) ^ _segments[i].GetHashCode();
                return hash;
            }
        }
    }
}
