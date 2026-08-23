using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailFragment
    {
        public const int MaximumPointCount = 256;

        private readonly ReadOnlyCollection<FixedTerritoryPoint> _points;

        public TerritoryTrailFragment(
            ulong sessionId,
            uint sequence,
            TerritoryChunkCoordinate chunk,
            uint firstSampleSequence,
            uint lastSampleSequence,
            IReadOnlyList<FixedTerritoryPoint> points)
        {
            if (sessionId == 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            if (firstSampleSequence > lastSampleSequence)
                throw new ArgumentOutOfRangeException(nameof(firstSampleSequence));
            if (points == null)
                throw new ArgumentNullException(nameof(points));
            if (points.Count < 2)
                throw new ArgumentException("A trail fragment requires at least two points.", nameof(points));
            if (points.Count > MaximumPointCount)
            {
                throw new ArgumentException(
                    $"A Trail fragment cannot contain more than {MaximumPointCount} points.",
                    nameof(points));
            }

            var copy = new FixedTerritoryPoint[points.Count];
            for (int i = 0; i < points.Count; i++)
            {
                FixedTerritoryPoint point = points[i];
                if (!chunk.ContainsClosed(point))
                    throw new ArgumentException($"Point {point} is outside closed bounds of Chunk {chunk}.", nameof(points));
                if (i > 0 && copy[i - 1] == point)
                    throw new ArgumentException("A Trail fragment cannot contain consecutive duplicate points.", nameof(points));

                copy[i] = point;
            }

            SessionId = sessionId;
            Sequence = sequence;
            Chunk = chunk;
            FirstSampleSequence = firstSampleSequence;
            LastSampleSequence = lastSampleSequence;
            _points = Array.AsReadOnly(copy);
        }

        public ulong SessionId { get; }
        public uint Sequence { get; }
        public TerritoryChunkCoordinate Chunk { get; }
        public uint FirstSampleSequence { get; }
        public uint LastSampleSequence { get; }
        public IReadOnlyList<FixedTerritoryPoint> Points => _points;
    }
}
