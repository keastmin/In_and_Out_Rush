using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBoundaryLoopIndex
    {
        private readonly SegmentRecord[] _segments;
        private readonly decimal[] _prefixTwiceArea;
        private readonly Dictionary<TerritoryChunkCoordinate, int[]> _sequencesByChunk;

        private TerritoryBoundaryLoopIndex(
            ulong revision,
            SegmentRecord[] segments,
            decimal[] prefixTwiceArea,
            Dictionary<TerritoryChunkCoordinate, int[]> sequencesByChunk)
        {
            Revision = revision;
            _segments = segments;
            _prefixTwiceArea = prefixTwiceArea;
            _sequencesByChunk = sequencesByChunk;
        }

        public ulong Revision { get; }
        public int SegmentCount => _segments.Length;
        public decimal SignedTwiceArea => _prefixTwiceArea[_prefixTwiceArea.Length - 1];
        public decimal AbsoluteTwiceArea => Math.Abs(SignedTwiceArea);

        public static bool TryCreate(
            TerritoryChunkSnapshot snapshot,
            out TerritoryBoundaryLoopIndex index,
            out string reason)
        {
            index = null;
            reason = string.Empty;

            if (snapshot == null)
            {
                reason = "A Boundary index requires a snapshot.";
                return false;
            }

            if (snapshot.Revision == 0)
            {
                reason = "Revision zero cannot produce a Boundary index.";
                return false;
            }

            var recordsBySequence = new Dictionary<int, SegmentRecord>();
            try
            {
                foreach (KeyValuePair<TerritoryChunkCoordinate, TerritoryChunkCoverage> pair in snapshot.Chunks)
                {
                    TerritoryChunkCoverage coverage = pair.Value;
                    if (coverage.Fill != TerritoryChunkFill.Boundary)
                        continue;

                    for (int i = 0; i < coverage.Segments.Count; i++)
                    {
                        TerritoryChunkBoundarySegment segment = coverage.Segments[i];
                        var record = new SegmentRecord(
                            pair.Key,
                            segment.Start.ToGlobal(pair.Key),
                            segment.End.ToGlobal(pair.Key));

                        if (!recordsBySequence.TryAdd(segment.Sequence, record))
                        {
                            reason = $"Boundary sequence {segment.Sequence} is duplicated.";
                            return false;
                        }
                    }
                }
            }
            catch (OverflowException)
            {
                reason = "A Boundary endpoint exceeds the fixed coordinate range.";
                return false;
            }

            if (recordsBySequence.Count < 3)
            {
                reason = "A Boundary loop requires at least three segments.";
                return false;
            }

            int count = recordsBySequence.Count;
            var ordered = new SegmentRecord[count];
            for (int sequence = 0; sequence < count; sequence++)
            {
                if (!recordsBySequence.TryGetValue(sequence, out SegmentRecord record))
                {
                    reason = $"Boundary sequence {sequence} is missing.";
                    return false;
                }

                ordered[sequence] = record;
            }

            foreach (int sequence in recordsBySequence.Keys)
            {
                if (sequence >= count)
                {
                    reason = $"Boundary sequence {sequence} is outside the required 0..{count - 1} range.";
                    return false;
                }
            }

            for (int i = 0; i < ordered.Length; i++)
            {
                SegmentRecord current = ordered[i];
                SegmentRecord next = ordered[(i + 1) % ordered.Length];
                if (current.End != next.Start)
                {
                    reason = $"Boundary sequence {i} does not connect to sequence {(i + 1) % ordered.Length}.";
                    return false;
                }
            }

            var prefix = new decimal[count + 1];
            try
            {
                for (int i = 0; i < count; i++)
                    prefix[i + 1] = prefix[i] + Cross(ordered[i].Start, ordered[i].End);
            }
            catch (OverflowException)
            {
                reason = "Boundary area accumulation overflowed.";
                return false;
            }

            if (prefix[count] == 0m)
            {
                reason = "Boundary loop area must be non-zero.";
                return false;
            }

            var mutableLookup = new Dictionary<TerritoryChunkCoordinate, List<int>>();
            for (int sequence = 0; sequence < count; sequence++)
            {
                TerritoryChunkCoordinate source = ordered[sequence].SourceChunk;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        long lookupX = (long)source.X + x;
                        long lookupY = (long)source.Y + y;
                        if (lookupX < int.MinValue || lookupX > int.MaxValue ||
                            lookupY < int.MinValue || lookupY > int.MaxValue)
                        {
                            continue;
                        }

                        var chunk = new TerritoryChunkCoordinate((int)lookupX, (int)lookupY);
                        if (!mutableLookup.TryGetValue(chunk, out List<int> sequences))
                        {
                            sequences = new List<int>();
                            mutableLookup.Add(chunk, sequences);
                        }

                        sequences.Add(sequence);
                    }
                }
            }

            var lookup = new Dictionary<TerritoryChunkCoordinate, int[]>(mutableLookup.Count);
            foreach (KeyValuePair<TerritoryChunkCoordinate, List<int>> pair in mutableLookup)
                lookup.Add(pair.Key, pair.Value.ToArray());

            index = new TerritoryBoundaryLoopIndex(snapshot.Revision, ordered, prefix, lookup);
            return true;
        }

        public bool TryGetOrderedSegment(
            int sequence,
            out FixedTerritoryPoint start,
            out FixedTerritoryPoint end)
        {
            if (sequence < 0 || sequence >= _segments.Length)
            {
                start = default;
                end = default;
                return false;
            }

            start = _segments[sequence].Start;
            end = _segments[sequence].End;
            return true;
        }

        internal bool TryCollectContacts(
            TerritoryChunkCoordinate chunk,
            FixedTerritoryPoint trailStart,
            FixedTerritoryPoint trailEnd,
            List<Contact> contacts,
            out int candidateChecks,
            out bool overlapsBoundary)
        {
            contacts.Clear();
            candidateChecks = 0;
            overlapsBoundary = false;

            if (!_sequencesByChunk.TryGetValue(chunk, out int[] sequences))
                return true;

            for (int i = 0; i < sequences.Length; i++)
            {
                int sequence = sequences[i];
                SegmentRecord boundary = _segments[sequence];
                candidateChecks++;

                IntersectionKind kind = TryIntersect(
                    trailStart,
                    trailEnd,
                    boundary.Start,
                    boundary.End,
                    out decimal trailT,
                    out decimal boundaryT,
                    out decimal x,
                    out decimal y);

                if (kind == IntersectionKind.Overlap)
                {
                    overlapsBoundary = true;
                    return false;
                }

                if (kind != IntersectionKind.Point)
                    continue;

                decimal position = sequence + boundaryT;
                if (position == _segments.Length)
                    position = 0m;

                contacts.Add(new Contact(
                    x,
                    y,
                    trailT,
                    position,
                    sequence,
                    boundary.Start,
                    boundary.End));
            }

            contacts.Sort(Contact.CompareByTrailPosition);
            return true;
        }

        internal decimal GetForwardArcTwiceArea(Contact from, Contact to)
        {
            decimal fromPosition = NormalizePosition(from.BoundaryPosition);
            decimal toPosition = NormalizePosition(to.BoundaryPosition);
            int fromSequence = (int)decimal.Floor(fromPosition);
            int toSequence = (int)decimal.Floor(toPosition);

            if (fromPosition == toPosition && from.Point == to.Point)
                return 0m;

            if (fromSequence == toSequence && fromPosition < toPosition)
                return Cross(from.Point, to.Point);

            decimal area = Cross(from.Point, _segments[fromSequence].End);
            int fullStart = fromSequence + 1;

            if (fromPosition < toPosition)
            {
                area += RangeArea(fullStart, toSequence);
            }
            else
            {
                area += RangeArea(fullStart, _segments.Length);
                area += RangeArea(0, toSequence);
            }

            area += Cross(_segments[toSequence].Start, to.Point);
            return area;
        }

        internal static decimal Cross(FixedTerritoryPoint a, FixedTerritoryPoint b)
            => (decimal)a.X * b.Y - (decimal)a.Y * b.X;

        internal static decimal Orientation(
            FixedTerritoryPoint a,
            FixedTerritoryPoint b,
            FixedTerritoryPoint c)
            => ((decimal)b.X - a.X) * ((decimal)c.Y - a.Y) -
               ((decimal)b.Y - a.Y) * ((decimal)c.X - a.X);

        internal static FixedTerritoryPoint RoundPoint(decimal x, decimal y)
        {
            decimal roundedX = decimal.Round(x, 0, MidpointRounding.AwayFromZero);
            decimal roundedY = decimal.Round(y, 0, MidpointRounding.AwayFromZero);
            if (roundedX < int.MinValue || roundedX > int.MaxValue ||
                roundedY < int.MinValue || roundedY > int.MaxValue)
            {
                throw new OverflowException("An intersection exceeds the fixed coordinate range.");
            }

            return new FixedTerritoryPoint((int)roundedX, (int)roundedY);
        }

        internal static bool PointOnSegment(
            FixedTerritoryPoint point,
            FixedTerritoryPoint start,
            FixedTerritoryPoint end)
            => Orientation(start, end, point) == 0m &&
               point.X >= Math.Min(start.X, end.X) && point.X <= Math.Max(start.X, end.X) &&
               point.Y >= Math.Min(start.Y, end.Y) && point.Y <= Math.Max(start.Y, end.Y);

        internal static IntersectionKind TryIntersect(
            FixedTerritoryPoint firstStart,
            FixedTerritoryPoint firstEnd,
            FixedTerritoryPoint secondStart,
            FixedTerritoryPoint secondEnd,
            out decimal firstT,
            out decimal secondT,
            out decimal x,
            out decimal y)
        {
            decimal rx = (decimal)firstEnd.X - firstStart.X;
            decimal ry = (decimal)firstEnd.Y - firstStart.Y;
            decimal sx = (decimal)secondEnd.X - secondStart.X;
            decimal sy = (decimal)secondEnd.Y - secondStart.Y;
            decimal qpx = (decimal)secondStart.X - firstStart.X;
            decimal qpy = (decimal)secondStart.Y - firstStart.Y;
            decimal denominator = rx * sy - ry * sx;

            firstT = 0m;
            secondT = 0m;
            x = 0m;
            y = 0m;

            if (denominator == 0m)
            {
                if (qpx * ry - qpy * rx != 0m)
                    return IntersectionKind.None;

                decimal firstLengthSquared = rx * rx + ry * ry;
                if (firstLengthSquared == 0m)
                    return IntersectionKind.None;

                decimal t0 = (qpx * rx + qpy * ry) / firstLengthSquared;
                decimal t1 = t0 + (sx * rx + sy * ry) / firstLengthSquared;
                decimal minimum = Math.Max(0m, Math.Min(t0, t1));
                decimal maximum = Math.Min(1m, Math.Max(t0, t1));
                if (minimum > maximum)
                    return IntersectionKind.None;
                if (minimum < maximum)
                    return IntersectionKind.Overlap;

                firstT = minimum;
                x = firstStart.X + firstT * rx;
                y = firstStart.Y + firstT * ry;
                decimal secondLengthSquared = sx * sx + sy * sy;
                if (secondLengthSquared == 0m)
                    return IntersectionKind.None;
                secondT = ((x - secondStart.X) * sx + (y - secondStart.Y) * sy) /
                          secondLengthSquared;
                return IntersectionKind.Point;
            }

            firstT = (qpx * sy - qpy * sx) / denominator;
            secondT = (qpx * ry - qpy * rx) / denominator;
            if (firstT < 0m || firstT > 1m || secondT < 0m || secondT > 1m)
                return IntersectionKind.None;

            x = firstStart.X + firstT * rx;
            y = firstStart.Y + firstT * ry;
            return IntersectionKind.Point;
        }

        private decimal NormalizePosition(decimal position)
        {
            if (position < 0m)
                position += _segments.Length;
            if (position >= _segments.Length)
                position -= _segments.Length;
            return position;
        }

        private decimal RangeArea(int startInclusive, int endExclusive)
        {
            if (startInclusive >= endExclusive)
                return 0m;
            return _prefixTwiceArea[endExclusive] - _prefixTwiceArea[startInclusive];
        }

        internal enum IntersectionKind
        {
            None,
            Point,
            Overlap
        }

        internal readonly struct Contact
        {
            public Contact(
                decimal x,
                decimal y,
                decimal trailPosition,
                decimal boundaryPosition,
                int sequence,
                FixedTerritoryPoint boundaryStart,
                FixedTerritoryPoint boundaryEnd)
            {
                X = x;
                Y = y;
                Point = RoundPoint(x, y);
                TrailPosition = trailPosition;
                BoundaryPosition = boundaryPosition;
                Sequence = sequence;
                BoundaryStart = boundaryStart;
                BoundaryEnd = boundaryEnd;
            }

            public decimal X { get; }
            public decimal Y { get; }
            public FixedTerritoryPoint Point { get; }
            public decimal TrailPosition { get; }
            public decimal BoundaryPosition { get; }
            public int Sequence { get; }
            public FixedTerritoryPoint BoundaryStart { get; }
            public FixedTerritoryPoint BoundaryEnd { get; }

            public static int CompareByTrailPosition(Contact left, Contact right)
                => left.TrailPosition.CompareTo(right.TrailPosition);
        }

        private readonly struct SegmentRecord
        {
            public SegmentRecord(
                TerritoryChunkCoordinate sourceChunk,
                FixedTerritoryPoint start,
                FixedTerritoryPoint end)
            {
                SourceChunk = sourceChunk;
                Start = start;
                End = end;
            }

            public TerritoryChunkCoordinate SourceChunk { get; }
            public FixedTerritoryPoint Start { get; }
            public FixedTerritoryPoint End { get; }
        }
    }
}
