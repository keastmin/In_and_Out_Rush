using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkExpansionSession
    {
        private const int TrailIndexCellSize = FixedTerritoryPoint.UnitsPerWorldUnit;

        private TerritoryBoundaryLoopIndex _boundaryIndex;
        private List<FixedTerritoryPoint> _planTrailPoints = new();
        private List<TerritoryChunkCoordinate> _affectedChunks = new();
        private HashSet<TerritoryChunkCoordinate> _affectedChunkSet = new();
        private List<TrailSegmentRecord> _trailSegments = new();
        private Dictionary<TrailCellCoordinate, List<int>> _trailSegmentsByCell = new();
        private readonly List<TerritoryBoundaryLoopIndex.Contact> _contacts = new();
        private readonly HashSet<int> _candidateTrailSegments = new();

        private TerritoryBoundaryLoopIndex.Contact _exitContact;
        private TerritoryBoundaryLoopIndex.Contact _entryContact;
        private FixedTerritoryPoint _lastPathPoint;
        private FixedTerritoryPoint _lastBoundaryContactPoint;
        private uint _nextFragmentSequence;
        private int _crossingCount;
        private int _fragmentCount;
        private int _trailPointCount;
        private long _boundaryCandidateChecks;
        private long _trailSelfIntersectionChecks;
        private decimal _trailTwiceArea;
        private bool _hasLastPathPoint;
        private bool _outside;
        private bool _suppressMatchingStartContact;

        public ulong SessionId { get; private set; }
        public TerritoryTrailSessionStatus Status { get; private set; }

        public TerritoryChunkExpansionMetrics Metrics => new(
            _boundaryIndex?.SegmentCount ?? 0,
            _fragmentCount,
            _trailPointCount,
            _boundaryCandidateChecks,
            _trailSelfIntersectionChecks,
            0,
            0);

        public bool TryBegin(
            TerritoryBoundaryLoopIndex boundaryIndex,
            ulong sessionId,
            out string reason)
        {
            if (boundaryIndex == null)
            {
                reason = "An expansion session requires a Boundary index.";
                return false;
            }
            if (sessionId == 0)
            {
                reason = "SessionId must be greater than zero.";
                return false;
            }
            if (Status == TerritoryTrailSessionStatus.Active)
            {
                reason = "The current expansion session is still active.";
                return false;
            }
            if (sessionId <= SessionId)
            {
                reason = $"SessionId {sessionId} is stale; the last SessionId is {SessionId}.";
                return false;
            }

            ResetPayload();
            _boundaryIndex = boundaryIndex;
            SessionId = sessionId;
            Status = TerritoryTrailSessionStatus.Active;
            reason = null;
            return true;
        }

        public bool TryAppendFragment(TerritoryTrailFragment fragment, out string reason)
        {
            if (fragment == null)
            {
                reason = "Trail fragment cannot be null.";
                return false;
            }
            if (!ValidateActiveSession(fragment.SessionId, out reason))
                return false;
            if (fragment.Sequence != _nextFragmentSequence)
            {
                reason = $"Expected fragment sequence {_nextFragmentSequence}, received {fragment.Sequence}.";
                return false;
            }
            if (fragment.Sequence == uint.MaxValue)
            {
                reason = "Fragment sequence exhausted UInt32 range.";
                return false;
            }

            IReadOnlyList<FixedTerritoryPoint> points = fragment.Points;
            int firstNewPoint = 0;
            if (_hasLastPathPoint)
            {
                if (_lastPathPoint != points[0])
                {
                    reason = "Adjacent Trail fragments must share one boundary point.";
                    return false;
                }

                firstNewPoint = 1;
            }
            else
            {
                _lastPathPoint = points[0];
                _hasLastPathPoint = true;
                _trailPointCount = 1;
                firstNewPoint = 1;
            }

            if (_affectedChunkSet.Add(fragment.Chunk))
                _affectedChunks.Add(fragment.Chunk);

            for (int i = firstNewPoint; i < points.Count; i++)
            {
                FixedTerritoryPoint next = points[i];
                if (!TryAppendSegment(fragment.Chunk, _lastPathPoint, next, out reason))
                    return false;

                _lastPathPoint = next;
                _trailPointCount++;
            }

            _fragmentCount++;
            _nextFragmentSequence++;
            reason = null;
            return true;
        }

        public bool TryComplete(
            ulong sessionId,
            out TerritoryChunkExpansionPlan plan,
            out string reason)
        {
            plan = null;
            if (!ValidateActiveSession(sessionId, out reason))
                return false;
            if (_crossingCount != 2 || _outside)
            {
                return FailGeometry("Expansion requires exactly one exit and one re-entry.", out reason);
            }
            if (_planTrailPoints.Count < 2)
            {
                return FailGeometry("Expansion Trail does not contain an external path.", out reason);
            }

            decimal forwardArc = _boundaryIndex.GetForwardArcTwiceArea(_entryContact, _exitContact);
            decimal reverseArc = -_boundaryIndex.GetForwardArcTwiceArea(_exitContact, _entryContact);
            decimal forwardCandidate = _trailTwiceArea + forwardArc;
            decimal reverseCandidate = _trailTwiceArea + reverseArc;
            decimal oldArea = _boundaryIndex.AbsoluteTwiceArea;
            decimal forwardSize = Math.Abs(forwardCandidate);
            decimal reverseSize = Math.Abs(reverseCandidate);

            bool forwardValid = forwardSize > oldArea;
            bool reverseValid = reverseSize > oldArea;
            if (!forwardValid && !reverseValid)
            {
                return FailGeometry("Neither expansion candidate increases Territory area.", out reason);
            }

            bool boundaryForward = forwardValid && (!reverseValid || forwardSize >= reverseSize);
            decimal selectedArea = boundaryForward ? forwardCandidate : reverseCandidate;
            TerritoryChunkExpansionMetrics metrics = Metrics;
            plan = new TerritoryChunkExpansionPlan(
                _boundaryIndex.Revision,
                SessionId,
                _exitContact.Point,
                _entryContact.Point,
                _exitContact.Sequence,
                _entryContact.Sequence,
                _exitContact.SegmentId,
                _entryContact.SegmentId,
                boundaryForward,
                selectedArea,
                _planTrailPoints.AsReadOnly(),
                _affectedChunks.AsReadOnly(),
                metrics);

            Status = TerritoryTrailSessionStatus.Committed;
            reason = null;
            return true;
        }

        public bool TryAbort(ulong sessionId, out string reason)
        {
            if (!ValidateActiveSession(sessionId, out reason))
                return false;

            ResetPayload();
            Status = TerritoryTrailSessionStatus.Aborted;
            reason = null;
            return true;
        }

        private bool TryAppendSegment(
            TerritoryChunkCoordinate chunk,
            FixedTerritoryPoint start,
            FixedTerritoryPoint end,
            out string reason)
        {
            if (start == end)
            {
                reason = null;
                return true;
            }

            try
            {
                if (!_boundaryIndex.TryCollectContacts(
                        chunk,
                        start,
                        end,
                        _contacts,
                        out int candidateChecks,
                        out bool overlapsBoundary))
                {
                    _boundaryCandidateChecks += candidateChecks;
                    if (overlapsBoundary)
                        return FailGeometry("Trail overlaps the existing Territory Boundary.", out reason);

                    return FailGeometry("Boundary contact collection failed.", out reason);
                }

                _boundaryCandidateChecks += candidateChecks;
                bool segmentEndedWithCrossing = false;
                int contactIndex = 0;
                while (contactIndex < _contacts.Count)
                {
                    int groupEnd = contactIndex + 1;
                    TerritoryBoundaryLoopIndex.Contact representative = _contacts[contactIndex];
                    while (groupEnd < _contacts.Count &&
                           SameContact(representative, _contacts[groupEnd]))
                    {
                        groupEnd++;
                    }

                    bool suppressedDuplicate = representative.TrailPosition == 0m &&
                                               _suppressMatchingStartContact &&
                                               representative.Point == _lastBoundaryContactPoint;
                    if (!suppressedDuplicate && IsBoundaryCrossing(start, end, contactIndex, groupEnd))
                    {
                        if (!ProcessCrossing(representative, out reason))
                            return false;

                        segmentEndedWithCrossing = representative.TrailPosition == 1m;
                        _lastBoundaryContactPoint = representative.Point;
                    }

                    contactIndex = groupEnd;
                }

                if (_outside && !TryAddPlanPoint(end, out reason))
                    return false;

                _suppressMatchingStartContact = segmentEndedWithCrossing;
            }
            catch (OverflowException)
            {
                return FailGeometry("Expansion fixed geometry overflowed.", out reason);
            }

            reason = null;
            return true;
        }

        private bool ProcessCrossing(
            TerritoryBoundaryLoopIndex.Contact contact,
            out string reason)
        {
            if (_crossingCount == 0)
            {
                _exitContact = contact;
                _crossingCount = 1;
                _outside = true;
                return TryAddPlanPoint(contact.Point, out reason);
            }

            if (_crossingCount == 1 && _outside)
            {
                if (!TryAddPlanPoint(contact.Point, out reason))
                    return false;
                _entryContact = contact;
                _crossingCount = 2;
                _outside = false;
                reason = null;
                return true;
            }

            return FailGeometry("Trail crosses the existing Boundary more than twice.", out reason);
        }

        private bool IsBoundaryCrossing(
            FixedTerritoryPoint trailStart,
            FixedTerritoryPoint trailEnd,
            int groupStart,
            int groupEnd)
        {
            if (groupEnd - groupStart == 1)
            {
                TerritoryBoundaryLoopIndex.Contact contact = _contacts[groupStart];
                bool atStart = contact.X == contact.BoundaryStart.X && contact.Y == contact.BoundaryStart.Y;
                bool atEnd = contact.X == contact.BoundaryEnd.X && contact.Y == contact.BoundaryEnd.Y;
                if (!atStart && !atEnd)
                    return true;
            }

            decimal trailX = (decimal)trailEnd.X - trailStart.X;
            decimal trailY = (decimal)trailEnd.Y - trailStart.Y;
            bool positive = false;
            bool negative = false;

            for (int i = groupStart; i < groupEnd; i++)
            {
                TerritoryBoundaryLoopIndex.Contact contact = _contacts[i];
                FixedTerritoryPoint other;
                if (contact.X == contact.BoundaryStart.X && contact.Y == contact.BoundaryStart.Y)
                    other = contact.BoundaryEnd;
                else if (contact.X == contact.BoundaryEnd.X && contact.Y == contact.BoundaryEnd.Y)
                    other = contact.BoundaryStart;
                else
                    return true;

                decimal side = trailX * (other.Y - contact.Y) -
                               trailY * (other.X - contact.X);
                positive |= side > 0m;
                negative |= side < 0m;
            }

            if (positive && negative)
                return true;

            return groupEnd - groupStart == 1 && (positive || negative);
        }

        private bool TryValidateNoTrailIntersection(
            FixedTerritoryPoint start,
            FixedTerritoryPoint end,
            out string reason)
        {
            _candidateTrailSegments.Clear();
            int minimumCellX = ToTrailCell(Math.Min(start.X, end.X));
            int maximumCellX = ToTrailCell(Math.Max(start.X, end.X));
            int minimumCellY = ToTrailCell(Math.Min(start.Y, end.Y));
            int maximumCellY = ToTrailCell(Math.Max(start.Y, end.Y));
            for (int y = minimumCellY; y <= maximumCellY; y++)
            {
                for (int x = minimumCellX; x <= maximumCellX; x++)
                {
                    var cell = new TrailCellCoordinate(x, y);
                    if (!_trailSegmentsByCell.TryGetValue(cell, out List<int> candidates))
                        continue;

                    for (int i = 0; i < candidates.Count; i++)
                        _candidateTrailSegments.Add(candidates[i]);
                }
            }

            foreach (int candidateIndex in _candidateTrailSegments)
            {
                TrailSegmentRecord candidate = _trailSegments[candidateIndex];
                _trailSelfIntersectionChecks++;
                TerritoryBoundaryLoopIndex.IntersectionKind kind = TerritoryBoundaryLoopIndex.TryIntersect(
                    start,
                    end,
                    candidate.Start,
                    candidate.End,
                    out decimal newT,
                    out _,
                    out decimal x,
                    out decimal y);

                if (kind == TerritoryBoundaryLoopIndex.IntersectionKind.None)
                    continue;
                if (kind == TerritoryBoundaryLoopIndex.IntersectionKind.Overlap)
                    return FailGeometry("Trail overlaps its previous path.", out reason);

                bool adjacentSharedStart = candidateIndex == _trailSegments.Count - 1 &&
                                           newT == 0m &&
                                           x == start.X &&
                                           y == start.Y &&
                                           candidate.End == start;
                if (!adjacentSharedStart)
                    return FailGeometry("Trail intersects its previous path.", out reason);
            }

            reason = null;
            return true;
        }

        private void AddTrailSegment(
            FixedTerritoryPoint start,
            FixedTerritoryPoint end)
        {
            int index = _trailSegments.Count;
            _trailSegments.Add(new TrailSegmentRecord(start, end));
            int minimumCellX = ToTrailCell(Math.Min(start.X, end.X));
            int maximumCellX = ToTrailCell(Math.Max(start.X, end.X));
            int minimumCellY = ToTrailCell(Math.Min(start.Y, end.Y));
            int maximumCellY = ToTrailCell(Math.Max(start.Y, end.Y));
            for (int y = minimumCellY; y <= maximumCellY; y++)
            {
                for (int x = minimumCellX; x <= maximumCellX; x++)
                {
                    var cell = new TrailCellCoordinate(x, y);
                    if (!_trailSegmentsByCell.TryGetValue(cell, out List<int> entries))
                    {
                        entries = new List<int>();
                        _trailSegmentsByCell.Add(cell, entries);
                    }

                    entries.Add(index);
                }
            }
        }

        private bool TryAddPlanPoint(
            FixedTerritoryPoint point,
            out string reason)
        {
            if (_planTrailPoints.Count > 0 && _planTrailPoints[_planTrailPoints.Count - 1] == point)
            {
                reason = null;
                return true;
            }

            if (_planTrailPoints.Count > 0)
            {
                FixedTerritoryPoint previous = _planTrailPoints[_planTrailPoints.Count - 1];
                if (!TryValidateNoTrailIntersection(previous, point, out reason))
                    return false;

                AddTrailSegment(previous, point);
            }

            if (_planTrailPoints.Count > 0)
                _trailTwiceArea += TerritoryBoundaryLoopIndex.Cross(_planTrailPoints[_planTrailPoints.Count - 1], point);

            if (_planTrailPoints.Count >= 2)
            {
                FixedTerritoryPoint before = _planTrailPoints[_planTrailPoints.Count - 2];
                FixedTerritoryPoint middle = _planTrailPoints[_planTrailPoints.Count - 1];
                if (TerritoryBoundaryLoopIndex.Orientation(before, middle, point) == 0m &&
                    IsBetween(middle, before, point))
                {
                    _planTrailPoints.RemoveAt(_planTrailPoints.Count - 1);
                }
            }

            _planTrailPoints.Add(point);
            reason = null;
            return true;
        }

        private bool FailGeometry(string failureReason, out string reason)
        {
            ResetPayload();
            Status = TerritoryTrailSessionStatus.Aborted;
            reason = failureReason;
            return false;
        }

        private bool ValidateActiveSession(ulong sessionId, out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Active)
            {
                reason = "The expansion session is not active.";
                return false;
            }
            if (sessionId != SessionId)
            {
                reason = $"Payload SessionId {sessionId} does not match active SessionId {SessionId}.";
                return false;
            }

            reason = null;
            return true;
        }

        private void ResetPayload()
        {
            _planTrailPoints = new List<FixedTerritoryPoint>();
            _affectedChunks = new List<TerritoryChunkCoordinate>();
            _affectedChunkSet = new HashSet<TerritoryChunkCoordinate>();
            _trailSegments = new List<TrailSegmentRecord>();
            _trailSegmentsByCell = new Dictionary<TrailCellCoordinate, List<int>>();
            _contacts.Clear();
            _candidateTrailSegments.Clear();
            _nextFragmentSequence = 0;
            _crossingCount = 0;
            _fragmentCount = 0;
            _trailPointCount = 0;
            _boundaryCandidateChecks = 0;
            _trailSelfIntersectionChecks = 0;
            _trailTwiceArea = 0m;
            _hasLastPathPoint = false;
            _outside = false;
            _suppressMatchingStartContact = false;
            _lastPathPoint = default;
            _lastBoundaryContactPoint = default;
            _exitContact = default;
            _entryContact = default;
        }

        private static bool SameContact(
            TerritoryBoundaryLoopIndex.Contact left,
            TerritoryBoundaryLoopIndex.Contact right)
            => left.TrailPosition == right.TrailPosition && left.X == right.X && left.Y == right.Y;

        private static bool IsBetween(
            FixedTerritoryPoint point,
            FixedTerritoryPoint start,
            FixedTerritoryPoint end)
            => point.X >= Math.Min(start.X, end.X) && point.X <= Math.Max(start.X, end.X) &&
               point.Y >= Math.Min(start.Y, end.Y) && point.Y <= Math.Max(start.Y, end.Y);

        private static int ToTrailCell(int coordinate)
        {
            int quotient = coordinate / TrailIndexCellSize;
            int remainder = coordinate % TrailIndexCellSize;
            return remainder < 0 ? quotient - 1 : quotient;
        }

        private readonly struct TrailCellCoordinate : IEquatable<TrailCellCoordinate>
        {
            public TrailCellCoordinate(int x, int y)
            {
                X = x;
                Y = y;
            }

            public int X { get; }
            public int Y { get; }

            public bool Equals(TrailCellCoordinate other)
                => X == other.X && Y == other.Y;

            public override bool Equals(object obj)
                => obj is TrailCellCoordinate other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    return (X * 397) ^ Y;
                }
            }
        }

        private readonly struct TrailSegmentRecord
        {
            public TrailSegmentRecord(FixedTerritoryPoint start, FixedTerritoryPoint end)
            {
                Start = start;
                End = end;
            }

            public FixedTerritoryPoint Start { get; }
            public FixedTerritoryPoint End { get; }
        }
    }
}
