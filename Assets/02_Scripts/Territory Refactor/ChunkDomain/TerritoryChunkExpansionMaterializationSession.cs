using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkExpansionMaterializationSession
    {
        private readonly SortedDictionary<TerritoryChunkCoordinate, MutableBoundaryEdit> _mutableEdits =
            new(new ChunkCoordinateComparer());
        private readonly Dictionary<int, SortedSet<int>> _boundaryXsByRow = new();
        private readonly List<GlobalEdge> _globalEdges = new();
        private readonly Dictionary<int, List<int>> _edgesStartingByRow = new();
        private readonly Dictionary<int, List<int>> _edgesEndingByRow = new();
        private readonly SortedSet<int> _activeEdgeIndices = new();
        private readonly SortedDictionary<decimal, int> _scanlineIntersections = new();
        private List<TerritoryChunkFillRun> _fullRuns = new();
        private List<TerritoryChunkBoundaryEdit> _boundaryEdits = new();

        private TerritoryBoundaryLoopIndex _boundaryIndex;
        private TerritoryChunkExpansionPlan _plan;
        private TerritoryChunkExpansionMaterialization _result;
        private SegmentSplitCursor _splitter;
        private IEnumerator<int> _activeEdgeEnumerator;
        private IEnumerator<KeyValuePair<decimal, int>> _intersectionEnumerator;
        private IEnumerator<int> _intervalBoundaryEnumerator;
        private IEnumerator<KeyValuePair<TerritoryChunkCoordinate, MutableBoundaryEdit>> _editEnumerator;
        private Phase _phase;
        private int _trailEdgeIndex;
        private int _arcDirection;
        private int _arcSequence;
        private int _arcTraversedSequences;
        private FixedTerritoryPoint _arcPoint;
        private bool _arcStarted;
        private bool _arcComplete;
        private bool _splitIsTrail;
        private int _splitSourceSequence;
        private int _capturedPartSequence;
        private int _minimumBoundaryRow;
        private int _maximumBoundaryRow;
        private int _currentRow;
        private List<int> _rowEndingEdges;
        private List<int> _rowStartingEdges;
        private int _rowUpdateIndex;
        private int _pendingIntersectionCount;
        private decimal _pendingIntersectionX;
        private bool _hasLeftIntersection;
        private decimal _leftIntersection;
        private int _intervalCursorX;
        private int _intervalMaximumX;
        private long _trailEdgesRead;
        private long _replacedBoundarySegmentsRead;
        private long _boundaryPartsProduced;
        private long _scanlineEdgeChecks;
        private long _scanlineIntersectionsConsumed;
        private long _boundaryChunkExclusions;
        private long _fullChunkCount;
        private long _totalWorkUnits;

        public ulong SessionId { get; private set; }
        public TerritoryTrailSessionStatus Status { get; private set; }
        public int LastStepWorkUnits { get; private set; }

        public bool TryBegin(
            TerritoryChunkSnapshot sourceSnapshot,
            TerritoryBoundaryLoopIndex boundaryIndex,
            TerritoryChunkExpansionPlan plan,
            out string reason)
        {
            if (sourceSnapshot == null)
            {
                reason = "A compact materialization requires a source snapshot.";
                return false;
            }
            if (boundaryIndex == null)
            {
                reason = "A compact materialization requires a Boundary index.";
                return false;
            }
            if (plan == null)
            {
                reason = "A compact materialization requires an expansion plan.";
                return false;
            }
            if (Status == TerritoryTrailSessionStatus.Active)
            {
                reason = "The current materialization session is still active.";
                return false;
            }
            if (plan.SessionId == 0 || plan.SessionId <= SessionId)
            {
                reason = $"SessionId {plan.SessionId} is stale; the last SessionId is {SessionId}.";
                return false;
            }
            if (sourceSnapshot.Revision == 0 ||
                sourceSnapshot.Revision != boundaryIndex.Revision ||
                sourceSnapshot.Revision != plan.SourceRevision)
            {
                reason = "Snapshot, Boundary index and expansion plan revisions must match.";
                return false;
            }
            if (plan.TrailPoints.Count < 2 ||
                plan.TrailPoints[0] != plan.ExitPoint ||
                plan.TrailPoints[plan.TrailPoints.Count - 1] != plan.EntryPoint)
            {
                reason = "Expansion plan Trail endpoints do not match its exit and entry contacts.";
                return false;
            }
            if (plan.ExitPoint == plan.EntryPoint)
            {
                reason = "Exit and entry contacts must be distinct.";
                return false;
            }
            if (!TryValidateContact(boundaryIndex, plan.ExitBoundarySequence, plan.ExitPoint) ||
                !TryValidateContact(boundaryIndex, plan.EntryBoundarySequence, plan.EntryPoint))
            {
                reason = "Expansion plan contacts are not on their declared Boundary sequences.";
                return false;
            }

            decimal addedArea = plan.ResultAbsoluteTwiceArea - boundaryIndex.AbsoluteTwiceArea;
            if (addedArea <= 0m)
            {
                reason = "Expansion plan must add positive Territory area.";
                return false;
            }

            ResetCandidate();
            _boundaryIndex = boundaryIndex;
            _plan = plan;
            SessionId = plan.SessionId;
            Status = TerritoryTrailSessionStatus.Active;
            _phase = Phase.BuildTrail;
            _arcDirection = plan.BoundaryForwardFromEntryToExit ? -1 : 1;
            _arcSequence = plan.EntryBoundarySequence;
            _arcPoint = plan.EntryPoint;
            reason = null;
            return true;
        }

        public bool TryStep(
            int maxWorkUnits,
            out int workUnitsUsed,
            out bool completed,
            out string reason)
        {
            workUnitsUsed = 0;
            completed = Status == TerritoryTrailSessionStatus.Committed;
            LastStepWorkUnits = 0;

            if (maxWorkUnits <= 0)
            {
                reason = "maxWorkUnits must be greater than zero.";
                return false;
            }
            if (Status != TerritoryTrailSessionStatus.Active)
            {
                reason = completed
                    ? "The materialization session is already complete."
                    : "No materialization session is active.";
                return false;
            }

            try
            {
                while (workUnitsUsed < maxWorkUnits && Status == TerritoryTrailSessionStatus.Active)
                {
                    if (!TryProcessOneWorkUnit(out string stepReason))
                    {
                        BurnCandidate();
                        reason = stepReason;
                        return false;
                    }

                    workUnitsUsed++;
                    _totalWorkUnits++;
                }
            }
            catch (OverflowException)
            {
                BurnCandidate();
                reason = "Compact materialization fixed geometry overflowed.";
                return false;
            }
            catch (InvalidOperationException exception)
            {
                BurnCandidate();
                reason = exception.Message;
                return false;
            }

            LastStepWorkUnits = workUnitsUsed;
            completed = Status == TerritoryTrailSessionStatus.Committed;
            reason = null;
            return true;
        }

        public bool TryGetResult(
            out TerritoryChunkExpansionMaterialization materialization,
            out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Committed || _result == null)
            {
                materialization = null;
                reason = "Compact materialization has not completed.";
                return false;
            }

            materialization = _result;
            reason = null;
            return true;
        }

        public bool TryAbort(ulong sessionId, out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Active || sessionId != SessionId)
            {
                reason = "The requested materialization session is not active.";
                return false;
            }

            BurnCandidate();
            reason = null;
            return true;
        }

        private bool TryProcessOneWorkUnit(out string reason)
        {
            while (true)
            {
                switch (_phase)
                {
                    case Phase.BuildTrail:
                        if (_splitter != null)
                            return TryEmitBoundaryPart(out reason);

                        if (_trailEdgeIndex < _plan.TrailPoints.Count - 1)
                        {
                            FixedTerritoryPoint start = _plan.TrailPoints[_trailEdgeIndex];
                            FixedTerritoryPoint end = _plan.TrailPoints[_trailEdgeIndex + 1];
                            _trailEdgeIndex++;
                            if (start == end)
                            {
                                reason = "Expansion Trail contains a zero-length edge.";
                                return false;
                            }

                            StartGlobalEdge(start, end, true, -1);
                            _trailEdgesRead++;
                            reason = null;
                            return true;
                        }

                        _phase = Phase.BuildReplacedArc;
                        continue;

                    case Phase.BuildReplacedArc:
                        if (_splitter != null)
                            return TryEmitBoundaryPart(out reason);

                        if (!_arcComplete)
                        {
                            if (!TryTakeNextArcEdge(out FixedTerritoryPoint start, out FixedTerritoryPoint end, out int sourceSequence, out reason))
                                return false;
                            if (start == end)
                                continue;

                            StartGlobalEdge(start, end, false, sourceSequence);
                            _replacedBoundarySegmentsRead++;
                            return true;
                        }

                        _phase = Phase.PrepareRows;
                        continue;

                    case Phase.PrepareRows:
                        if (_mutableEdits.Count == 0 || _globalEdges.Count < 3)
                        {
                            reason = "Added-region Boundary is incomplete.";
                            return false;
                        }

                        _currentRow = _minimumBoundaryRow;
                        _phase = Phase.BeginRow;
                        reason = null;
                        return true;

                    case Phase.BeginRow:
                        _scanlineIntersections.Clear();
                        _hasLeftIntersection = false;
                        _pendingIntersectionCount = 0;
                        _rowEndingEdges = _edgesEndingByRow.TryGetValue(_currentRow, out List<int> ending)
                            ? ending
                            : null;
                        _rowStartingEdges = _edgesStartingByRow.TryGetValue(_currentRow, out List<int> starting)
                            ? starting
                            : null;
                        _rowUpdateIndex = 0;
                        _phase = Phase.RemoveRowEdges;
                        reason = null;
                        return true;

                    case Phase.RemoveRowEdges:
                        if (_rowEndingEdges != null && _rowUpdateIndex < _rowEndingEdges.Count)
                        {
                            _activeEdgeIndices.Remove(_rowEndingEdges[_rowUpdateIndex++]);
                            reason = null;
                            return true;
                        }

                        _rowUpdateIndex = 0;
                        _phase = Phase.AddRowEdges;
                        continue;

                    case Phase.AddRowEdges:
                        if (_rowStartingEdges != null && _rowUpdateIndex < _rowStartingEdges.Count)
                        {
                            _activeEdgeIndices.Add(_rowStartingEdges[_rowUpdateIndex++]);
                            reason = null;
                            return true;
                        }

                        _activeEdgeEnumerator = _activeEdgeIndices.GetEnumerator();
                        _phase = Phase.ScanRowEdges;
                        continue;

                    case Phase.ScanRowEdges:
                        if (_activeEdgeEnumerator.MoveNext())
                        {
                            AddScanlineIntersection(_globalEdges[_activeEdgeEnumerator.Current]);
                            _scanlineEdgeChecks++;
                            reason = null;
                            return true;
                        }

                        _activeEdgeEnumerator.Dispose();
                        _activeEdgeEnumerator = null;
                        _intersectionEnumerator = _scanlineIntersections.GetEnumerator();
                        _phase = Phase.ConsumeIntersections;
                        continue;

                    case Phase.ConsumeIntersections:
                        if (_pendingIntersectionCount == 0)
                        {
                            if (!_intersectionEnumerator.MoveNext())
                            {
                                _intersectionEnumerator.Dispose();
                                _intersectionEnumerator = null;
                                if (_hasLeftIntersection)
                                {
                                    reason = $"Scanline row {_currentRow} has an odd intersection count.";
                                    return false;
                                }

                                _phase = Phase.FinalizeRow;
                                continue;
                            }

                            _pendingIntersectionX = _intersectionEnumerator.Current.Key;
                            _pendingIntersectionCount = _intersectionEnumerator.Current.Value;
                        }

                        decimal intersection = _pendingIntersectionX;
                        _pendingIntersectionCount--;
                        _scanlineIntersectionsConsumed++;
                        if (!_hasLeftIntersection)
                        {
                            _leftIntersection = intersection;
                            _hasLeftIntersection = true;
                        }
                        else
                        {
                            _hasLeftIntersection = false;
                            PrepareInteriorInterval(_leftIntersection, intersection);
                        }

                        reason = null;
                        return true;

                    case Phase.ConsumeIntervalBoundary:
                        if (_intervalBoundaryEnumerator.MoveNext())
                        {
                            int boundaryX = _intervalBoundaryEnumerator.Current;
                            if (_intervalCursorX < boundaryX)
                                AddFullRun(_currentRow, _intervalCursorX, boundaryX - 1);

                            MarkBoundaryCenterInside(boundaryX, _currentRow);
                            _intervalCursorX = boundaryX == int.MaxValue ? int.MaxValue : boundaryX + 1;
                            _boundaryChunkExclusions++;
                            reason = null;
                            return true;
                        }

                        _intervalBoundaryEnumerator.Dispose();
                        _intervalBoundaryEnumerator = null;
                        if (_intervalCursorX <= _intervalMaximumX)
                            AddFullRun(_currentRow, _intervalCursorX, _intervalMaximumX);
                        _phase = Phase.ConsumeIntersections;
                        reason = null;
                        return true;

                    case Phase.FinalizeRow:
                        if (_currentRow == _maximumBoundaryRow)
                        {
                            _editEnumerator = _mutableEdits.GetEnumerator();
                            _phase = Phase.FinalizeEdits;
                        }
                        else
                        {
                            _currentRow++;
                            _phase = Phase.BeginRow;
                        }

                        reason = null;
                        return true;

                    case Phase.FinalizeEdits:
                        if (_editEnumerator.MoveNext())
                        {
                            KeyValuePair<TerritoryChunkCoordinate, MutableBoundaryEdit> pair = _editEnumerator.Current;
                            MutableBoundaryEdit edit = pair.Value;
                            _boundaryEdits.Add(new TerritoryChunkBoundaryEdit(
                                pair.Key,
                                edit.CenterInsideAddedRegion,
                                edit.AddedTrailSegments,
                                edit.ReplacedBoundarySegments,
                                edit.RemovedSourceSequences));
                            reason = null;
                            return true;
                        }

                        _editEnumerator.Dispose();
                        _editEnumerator = null;
                        _phase = Phase.Publish;
                        continue;

                    case Phase.Publish:
                        var metrics = new TerritoryChunkExpansionMaterializationMetrics(
                            _trailEdgesRead,
                            _replacedBoundarySegmentsRead,
                            _boundaryPartsProduced,
                            _scanlineEdgeChecks,
                            _scanlineIntersectionsConsumed,
                            _boundaryChunkExclusions,
                            _fullChunkCount,
                            _fullRuns.Count,
                            _totalWorkUnits + 1L,
                            0L,
                            0L,
                            0L);
                        _result = new TerritoryChunkExpansionMaterialization(
                            _plan.SourceRevision,
                            _plan.SessionId,
                            _plan.ResultAbsoluteTwiceArea - _boundaryIndex.AbsoluteTwiceArea,
                            _boundaryEdits,
                            _fullRuns,
                            metrics);
                        Status = TerritoryTrailSessionStatus.Committed;
                        _phase = Phase.Done;
                        reason = null;
                        return true;

                    default:
                        reason = "Compact materialization entered an invalid work phase.";
                        return false;
                }
            }
        }

        private bool TryEmitBoundaryPart(out string reason)
        {
            if (!_splitter.TryTakeNext(out SegmentPart part, out bool completed))
            {
                reason = "Boundary edge splitting failed.";
                return false;
            }

            if (part.Start != part.End)
            {
                MutableBoundaryEdit edit = GetOrCreateEdit(part.Chunk);
                var localSegment = new TerritoryChunkBoundarySegment(
                    _capturedPartSequence++,
                    ToLocal(part.Chunk, part.Start),
                    ToLocal(part.Chunk, part.End));
                if (_splitIsTrail)
                {
                    edit.AddedTrailSegments.Add(localSegment);
                }
                else
                {
                    edit.ReplacedBoundarySegments.Add(localSegment);
                    edit.AddRemovedSequence(_splitSourceSequence);
                }

                if (!_boundaryXsByRow.TryGetValue(part.Chunk.Y, out SortedSet<int> boundaryXs))
                {
                    boundaryXs = new SortedSet<int>();
                    _boundaryXsByRow.Add(part.Chunk.Y, boundaryXs);
                }
                boundaryXs.Add(part.Chunk.X);
                _boundaryPartsProduced++;
            }

            if (completed)
                _splitter = null;

            reason = null;
            return true;
        }

        private void StartGlobalEdge(
            FixedTerritoryPoint start,
            FixedTerritoryPoint end,
            bool isTrail,
            int sourceSequence)
        {
            int edgeIndex = _globalEdges.Count;
            _globalEdges.Add(new GlobalEdge(start, end));
            _splitter = new SegmentSplitCursor(start, end);
            _splitIsTrail = isTrail;
            _splitSourceSequence = sourceSequence;

            if (start.Y == end.Y)
                return;

            long minimumY = Math.Min(start.Y, end.Y);
            long maximumY = Math.Max(start.Y, end.Y);
            int firstRow = CheckedChunkIndex(CeilingDivide(
                minimumY - TerritoryChunkCoordinate.SizeInFixedUnits / 2L,
                TerritoryChunkCoordinate.SizeInFixedUnits));
            int lastRow = CheckedChunkIndex(CeilingDivide(
                maximumY - TerritoryChunkCoordinate.SizeInFixedUnits / 2L,
                TerritoryChunkCoordinate.SizeInFixedUnits) - 1L);
            if (firstRow > lastRow)
                return;

            AddRowEdge(_edgesStartingByRow, firstRow, edgeIndex);
            if (lastRow < int.MaxValue)
                AddRowEdge(_edgesEndingByRow, lastRow + 1, edgeIndex);
        }

        private bool TryTakeNextArcEdge(
            out FixedTerritoryPoint start,
            out FixedTerritoryPoint end,
            out int sourceSequence,
            out string reason)
        {
            start = default;
            end = default;
            sourceSequence = -1;

            while (!_arcComplete)
            {
                if (_arcTraversedSequences > _boundaryIndex.SegmentCount + 1)
                {
                    reason = "Replaced Boundary arc exceeded its deterministic sequence bound.";
                    return false;
                }
                if (!_boundaryIndex.TryGetOrderedSegment(
                        _arcSequence,
                        out FixedTerritoryPoint segmentStart,
                        out FixedTerritoryPoint segmentEnd))
                {
                    reason = $"Boundary sequence {_arcSequence} is unavailable.";
                    return false;
                }

                bool onExitSequence = _arcSequence == _plan.ExitBoundarySequence;
                if (onExitSequence && (_arcStarted || IsAheadOnSegment(
                        segmentStart,
                        segmentEnd,
                        _arcPoint,
                        _plan.ExitPoint,
                        _arcDirection)))
                {
                    start = _arcPoint;
                    end = _plan.ExitPoint;
                    sourceSequence = _arcSequence;
                    _arcComplete = true;
                    reason = null;
                    return true;
                }

                FixedTerritoryPoint endpoint = _arcDirection > 0 ? segmentEnd : segmentStart;
                sourceSequence = _arcSequence;
                start = _arcPoint;
                end = endpoint;
                AdvanceArc(endpoint);
                if (start != end)
                {
                    reason = null;
                    return true;
                }
            }

            reason = null;
            return true;
        }

        private void AdvanceArc(FixedTerritoryPoint endpoint)
        {
            _arcPoint = endpoint;
            _arcSequence += _arcDirection;
            if (_arcSequence < 0)
                _arcSequence = _boundaryIndex.SegmentCount - 1;
            else if (_arcSequence >= _boundaryIndex.SegmentCount)
                _arcSequence = 0;
            _arcPoint = GetArcSequenceEntryPoint(_arcSequence);
            _arcStarted = true;
            _arcTraversedSequences++;
        }

        private FixedTerritoryPoint GetArcSequenceEntryPoint(int sequence)
        {
            if (!_boundaryIndex.TryGetOrderedSegment(
                    sequence,
                    out FixedTerritoryPoint start,
                    out FixedTerritoryPoint end))
            {
                throw new InvalidOperationException($"Boundary sequence {sequence} is unavailable.");
            }

            return _arcDirection > 0 ? start : end;
        }

        private void AddScanlineIntersection(GlobalEdge edge)
        {
            long scanY = (long)_currentRow * TerritoryChunkCoordinate.SizeInFixedUnits +
                         TerritoryChunkCoordinate.SizeInFixedUnits / 2L;
            decimal x = edge.Start.X +
                        (decimal)(scanY - edge.Start.Y) * (edge.End.X - (long)edge.Start.X) /
                        (edge.End.Y - (long)edge.Start.Y);
            if (_scanlineIntersections.TryGetValue(x, out int count))
                _scanlineIntersections[x] = checked(count + 1);
            else
                _scanlineIntersections.Add(x, 1);
        }

        private void PrepareInteriorInterval(decimal left, decimal right)
        {
            if (left >= right)
                return;

            decimal size = TerritoryChunkCoordinate.SizeInFixedUnits;
            decimal half = TerritoryChunkCoordinate.SizeInFixedUnits / 2m;
            decimal minimum = decimal.Floor((left - half) / size) + 1m;
            decimal maximum = decimal.Ceiling((right - half) / size) - 1m;
            if (minimum > maximum || maximum < int.MinValue || minimum > int.MaxValue)
                return;

            int minimumX = minimum < int.MinValue ? int.MinValue : (int)minimum;
            int maximumX = maximum > int.MaxValue ? int.MaxValue : (int)maximum;
            if (minimumX > maximumX)
                return;

            _intervalCursorX = minimumX;
            _intervalMaximumX = maximumX;
            if (_boundaryXsByRow.TryGetValue(_currentRow, out SortedSet<int> boundaryXs))
                _intervalBoundaryEnumerator = boundaryXs.GetViewBetween(minimumX, maximumX).GetEnumerator();
            else
                _intervalBoundaryEnumerator = EmptyIntEnumerator.Instance;
            _phase = Phase.ConsumeIntervalBoundary;
        }

        private void AddFullRun(int y, int minimumX, int maximumX)
        {
            if (minimumX > maximumX)
                return;

            if (_fullRuns.Count > 0)
            {
                TerritoryChunkFillRun previous = _fullRuns[_fullRuns.Count - 1];
                if (previous.Y == y && previous.MaximumX != int.MaxValue &&
                    previous.MaximumX + 1 >= minimumX)
                {
                    _fullChunkCount -= previous.ChunkCount;
                    _fullRuns[_fullRuns.Count - 1] = new TerritoryChunkFillRun(
                        y,
                        previous.MinimumX,
                        Math.Max(previous.MaximumX, maximumX));
                    _fullChunkCount += _fullRuns[_fullRuns.Count - 1].ChunkCount;
                    return;
                }
            }

            var run = new TerritoryChunkFillRun(y, minimumX, maximumX);
            _fullRuns.Add(run);
            _fullChunkCount += run.ChunkCount;
        }

        private MutableBoundaryEdit GetOrCreateEdit(TerritoryChunkCoordinate chunk)
        {
            if (_mutableEdits.TryGetValue(chunk, out MutableBoundaryEdit edit))
                return edit;

            edit = new MutableBoundaryEdit();
            _mutableEdits.Add(chunk, edit);
            if (_mutableEdits.Count == 1)
            {
                _minimumBoundaryRow = chunk.Y;
                _maximumBoundaryRow = chunk.Y;
            }
            else
            {
                _minimumBoundaryRow = Math.Min(_minimumBoundaryRow, chunk.Y);
                _maximumBoundaryRow = Math.Max(_maximumBoundaryRow, chunk.Y);
            }
            return edit;
        }

        private void MarkBoundaryCenterInside(int x, int y)
        {
            if (_mutableEdits.TryGetValue(new TerritoryChunkCoordinate(x, y), out MutableBoundaryEdit edit))
                edit.CenterInsideAddedRegion = true;
        }

        private static bool TryValidateContact(
            TerritoryBoundaryLoopIndex index,
            int sequence,
            FixedTerritoryPoint point)
            => index.TryGetOrderedSegment(sequence, out FixedTerritoryPoint start, out FixedTerritoryPoint end) &&
               TerritoryBoundaryLoopIndex.PointOnSegment(point, start, end);

        private static bool IsAheadOnSegment(
            FixedTerritoryPoint segmentStart,
            FixedTerritoryPoint segmentEnd,
            FixedTerritoryPoint from,
            FixedTerritoryPoint to,
            int direction)
        {
            long edgeX = (long)segmentEnd.X - segmentStart.X;
            long edgeY = (long)segmentEnd.Y - segmentStart.Y;
            decimal progress = (decimal)(to.X - (long)from.X) * edgeX +
                               (decimal)(to.Y - (long)from.Y) * edgeY;
            return direction > 0 ? progress > 0m : progress < 0m;
        }

        private static TerritoryChunkLocalPoint ToLocal(
            TerritoryChunkCoordinate chunk,
            FixedTerritoryPoint point)
        {
            long localX = point.X - chunk.MinimumX;
            long localY = point.Y - chunk.MinimumY;
            if (localX < 0L || localX > TerritoryChunkCoordinate.SizeInFixedUnits ||
                localY < 0L || localY > TerritoryChunkCoordinate.SizeInFixedUnits)
            {
                throw new InvalidOperationException("Split Boundary part is outside its declared Chunk.");
            }

            return new TerritoryChunkLocalPoint((int)localX, (int)localY);
        }

        private static void AddRowEdge(
            Dictionary<int, List<int>> rows,
            int row,
            int edgeIndex)
        {
            if (!rows.TryGetValue(row, out List<int> edges))
            {
                edges = new List<int>();
                rows.Add(row, edges);
            }
            edges.Add(edgeIndex);
        }

        private static long CeilingDivide(long value, int divisor)
            => -FloorDivide(-value, divisor);

        private static long FloorDivide(long value, int divisor)
        {
            long quotient = value / divisor;
            long remainder = value % divisor;
            return remainder < 0L ? quotient - 1L : quotient;
        }

        private static int CheckedChunkIndex(long value)
        {
            if (value < int.MinValue || value > int.MaxValue)
                throw new OverflowException("Chunk row exceeds Int32 range.");
            return (int)value;
        }

        private void BurnCandidate()
        {
            ResetCandidate();
            Status = TerritoryTrailSessionStatus.Aborted;
        }

        private void ResetCandidate()
        {
            _mutableEdits.Clear();
            _boundaryXsByRow.Clear();
            _globalEdges.Clear();
            _edgesStartingByRow.Clear();
            _edgesEndingByRow.Clear();
            _activeEdgeIndices.Clear();
            _scanlineIntersections.Clear();
            _fullRuns = new List<TerritoryChunkFillRun>();
            _boundaryEdits = new List<TerritoryChunkBoundaryEdit>();
            _boundaryIndex = null;
            _plan = null;
            _result = null;
            _splitter = null;
            _activeEdgeEnumerator?.Dispose();
            _activeEdgeEnumerator = null;
            _intersectionEnumerator?.Dispose();
            _intersectionEnumerator = null;
            _intervalBoundaryEnumerator?.Dispose();
            _intervalBoundaryEnumerator = null;
            _editEnumerator?.Dispose();
            _editEnumerator = null;
            _phase = Phase.None;
            _trailEdgeIndex = 0;
            _arcDirection = 0;
            _arcSequence = 0;
            _arcTraversedSequences = 0;
            _arcPoint = default;
            _arcStarted = false;
            _arcComplete = false;
            _capturedPartSequence = 0;
            _trailEdgesRead = 0L;
            _replacedBoundarySegmentsRead = 0L;
            _boundaryPartsProduced = 0L;
            _scanlineEdgeChecks = 0L;
            _scanlineIntersectionsConsumed = 0L;
            _boundaryChunkExclusions = 0L;
            _fullChunkCount = 0L;
            _totalWorkUnits = 0L;
            LastStepWorkUnits = 0;
        }

        private enum Phase
        {
            None,
            BuildTrail,
            BuildReplacedArc,
            PrepareRows,
            BeginRow,
            RemoveRowEdges,
            AddRowEdges,
            ScanRowEdges,
            ConsumeIntersections,
            ConsumeIntervalBoundary,
            FinalizeRow,
            FinalizeEdits,
            Publish,
            Done
        }

        private sealed class MutableBoundaryEdit
        {
            private readonly HashSet<int> _removedSet = new();

            public bool CenterInsideAddedRegion { get; set; }
            public List<TerritoryChunkBoundarySegment> AddedTrailSegments { get; } = new();
            public List<TerritoryChunkBoundarySegment> ReplacedBoundarySegments { get; } = new();
            public List<int> RemovedSourceSequences { get; } = new();

            public void AddRemovedSequence(int sequence)
            {
                if (_removedSet.Add(sequence))
                    RemovedSourceSequences.Add(sequence);
            }
        }

        private readonly struct GlobalEdge
        {
            public GlobalEdge(FixedTerritoryPoint start, FixedTerritoryPoint end)
            {
                Start = start;
                End = end;
            }

            public FixedTerritoryPoint Start { get; }
            public FixedTerritoryPoint End { get; }
        }

        private readonly struct SegmentPart
        {
            public SegmentPart(
                TerritoryChunkCoordinate chunk,
                FixedTerritoryPoint start,
                FixedTerritoryPoint end)
            {
                Chunk = chunk;
                Start = start;
                End = end;
            }

            public TerritoryChunkCoordinate Chunk { get; }
            public FixedTerritoryPoint Start { get; }
            public FixedTerritoryPoint End { get; }
        }

        private sealed class SegmentSplitCursor
        {
            private readonly FixedTerritoryPoint _start;
            private readonly long _deltaX;
            private readonly long _deltaY;
            private readonly int _stepX;
            private readonly int _stepY;
            private readonly decimal _stepTimeX;
            private readonly decimal _stepTimeY;
            private readonly long _maximumTransitions;
            private TerritoryChunkCoordinate _currentChunk;
            private decimal _nextX;
            private decimal _nextY;
            private decimal _currentTime;
            private long _transitions;
            private bool _completed;

            public SegmentSplitCursor(FixedTerritoryPoint start, FixedTerritoryPoint end)
            {
                if (start == end)
                    throw new ArgumentException("A split edge cannot have zero length.");

                _start = start;
                _deltaX = (long)end.X - start.X;
                _deltaY = (long)end.Y - start.Y;
                _stepX = Math.Sign(_deltaX);
                _stepY = Math.Sign(_deltaY);
                _currentChunk = TerritoryChunkCoordinate.FromPoint(start);
                _nextX = GetFirstBoundaryTime(start.X, _deltaX, _currentChunk.MinimumX, _currentChunk.MaximumX);
                _nextY = GetFirstBoundaryTime(start.Y, _deltaY, _currentChunk.MinimumY, _currentChunk.MaximumY);
                _stepTimeX = GetBoundaryStepTime(_deltaX);
                _stepTimeY = GetBoundaryStepTime(_deltaY);
                TerritoryChunkCoordinate destination = TerritoryChunkCoordinate.FromPoint(end);
                _maximumTransitions =
                    Math.Abs((long)destination.X - _currentChunk.X) +
                    Math.Abs((long)destination.Y - _currentChunk.Y) + 2L;
            }

            public bool TryTakeNext(out SegmentPart part, out bool completed)
            {
                part = default;
                completed = _completed;
                if (_completed)
                    return false;

                while (true)
                {
                    decimal nextTime = Math.Min(1m, Math.Min(_nextX, _nextY));
                    bool crossesX = _nextX == nextTime && _nextX <= 1m;
                    bool crossesY = _nextY == nextTime && _nextY <= 1m;
                    TerritoryChunkCoordinate partChunk = _currentChunk;
                    FixedTerritoryPoint partStart = Interpolate(_currentTime);
                    FixedTerritoryPoint partEnd = Interpolate(nextTime);

                    if (nextTime >= 1m)
                    {
                        _completed = true;
                        completed = true;
                    }
                    else
                    {
                        if (crossesX)
                        {
                            _currentChunk = new TerritoryChunkCoordinate(_currentChunk.X + _stepX, _currentChunk.Y);
                            _nextX += _stepTimeX;
                        }
                        if (crossesY)
                        {
                            _currentChunk = new TerritoryChunkCoordinate(_currentChunk.X, _currentChunk.Y + _stepY);
                            _nextY += _stepTimeY;
                        }

                        _currentTime = nextTime;
                        _transitions++;
                        if (_transitions > _maximumTransitions)
                            throw new InvalidOperationException("Chunk traversal exceeded its deterministic transition bound.");
                    }

                    if (partStart != partEnd)
                    {
                        part = new SegmentPart(partChunk, partStart, partEnd);
                        return true;
                    }

                    if (_completed)
                    {
                        completed = true;
                        return true;
                    }
                }
            }

            private FixedTerritoryPoint Interpolate(decimal time)
            {
                decimal x = _start.X + _deltaX * time;
                decimal y = _start.Y + _deltaY * time;
                return new FixedTerritoryPoint(RoundToInt(x), RoundToInt(y));
            }

            private static decimal GetFirstBoundaryTime(
                int start,
                long delta,
                long minimum,
                long maximum)
            {
                if (delta > 0L)
                    return (decimal)(maximum - start) / delta;
                if (delta < 0L)
                    return (decimal)(start - minimum) / -delta;
                return decimal.MaxValue;
            }

            private static decimal GetBoundaryStepTime(long delta)
                => delta == 0L
                    ? decimal.MaxValue
                    : (decimal)TerritoryChunkCoordinate.SizeInFixedUnits / Math.Abs(delta);

            private static int RoundToInt(decimal value)
            {
                decimal rounded = decimal.Round(value, 0, MidpointRounding.AwayFromZero);
                if (rounded < int.MinValue || rounded > int.MaxValue)
                    throw new OverflowException("Split fixed point exceeds Int32 range.");
                return (int)rounded;
            }
        }

        private sealed class ChunkCoordinateComparer : IComparer<TerritoryChunkCoordinate>
        {
            public int Compare(TerritoryChunkCoordinate left, TerritoryChunkCoordinate right)
            {
                int y = left.Y.CompareTo(right.Y);
                return y != 0 ? y : left.X.CompareTo(right.X);
            }
        }

        private sealed class EmptyIntEnumerator : IEnumerator<int>
        {
            public static readonly EmptyIntEnumerator Instance = new();

            public int Current => default;
            object System.Collections.IEnumerator.Current => Current;
            public bool MoveNext() => false;
            public void Reset() { }
            public void Dispose() { }
        }
    }
}
