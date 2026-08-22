using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactApplySession
    {
        private TerritoryCompactSnapshot _source;
        private TerritoryChunkExpansionMaterialization _materialization;
        private TerritoryBoundarySplice _splice;
        private TerritoryPersistentBoundaryTree _boundary;
        private TerritoryPersistentAvlMap<int, TerritoryCompactRowCoverage> _rows;
        private TerritoryPersistentAvlMap<TerritoryChunkCoordinate, TerritoryCompactSnapshot.BoundaryBucket> _boundaryByChunk;
        private TerritoryPersistentAvlMap<TerritoryChunkCoordinate, TerritoryCompactSnapshot.CandidateBucket> _candidatesByChunk;
        private readonly HashSet<TerritoryChunkCoordinate> _changedBoundarySet = new();
        private readonly HashSet<int> _changedRowSet = new();
        private readonly Dictionary<TerritoryChunkCoordinate, bool> _addedCenterInside = new();
        private TerritoryCompactSnapshot _candidate;
        private TerritoryCompactCommitResult _commitResult;
        private TerritoryBoundarySegmentId _predecessorId;
        private TerritoryBoundarySegmentId _successorId;
        private bool _wraps;
        private ulong _nextIdentity;
        private int _removeIndex;
        private int _insertIndex;
        private int _fullRunIndex;
        private int _editIndex;
        private long _persistentNodesCreated;
        private long _totalWorkUnits;
        private Phase _phase;

        public ulong SessionId { get; private set; }
        public TerritoryTrailSessionStatus Status { get; private set; }
        public int LastStepWorkUnits { get; private set; }
        public TerritoryCompactSnapshot Source => _source;

        public bool TryBegin(
            TerritoryCompactSnapshot source,
            TerritoryChunkExpansionMaterialization materialization,
            out string reason)
        {
            if (source == null || materialization == null)
            {
                reason = "A compact apply requires source state and C009 materialization.";
                return false;
            }
            if (Status == TerritoryTrailSessionStatus.Active)
            {
                reason = "The current compact apply is still active.";
                return false;
            }
            if (materialization.SessionId == 0 || materialization.SessionId <= SessionId)
            {
                reason = $"SessionId {materialization.SessionId} is stale; the last SessionId is {SessionId}.";
                return false;
            }
            if (source.Revision != materialization.SourceRevision)
            {
                reason = "Compact source revision does not match C009 materialization.";
                return false;
            }
            if (source.Revision == ulong.MaxValue)
            {
                reason = "Compact Territory revision exhausted UInt64 range.";
                return false;
            }
            if (materialization.BoundarySplice == null)
            {
                reason = "C009 materialization does not contain a canonical stable Boundary splice.";
                return false;
            }
            if ((ulong)materialization.BoundarySplice.InsertedSegments.Count >
                ulong.MaxValue - source.NextBoundaryIdentity)
            {
                reason = "Stable Boundary identity range is exhausted.";
                return false;
            }

            ResetCandidate();
            _source = source;
            _materialization = materialization;
            _splice = materialization.BoundarySplice;
            _boundary = source.Boundary;
            _rows = source.Rows;
            _boundaryByChunk = source.BoundaryByChunk;
            _candidatesByChunk = source.CandidatesByChunk;
            _nextIdentity = source.NextBoundaryIdentity;
            for (int i = 0; i < materialization.BoundaryEdits.Count; i++)
            {
                TerritoryChunkBoundaryEdit edit = materialization.BoundaryEdits[i];
                if (_addedCenterInside.TryGetValue(edit.Chunk, out bool existing))
                    _addedCenterInside[edit.Chunk] = existing || edit.CenterInsideAddedRegion;
                else
                    _addedCenterInside.Add(edit.Chunk, edit.CenterInsideAddedRegion);
            }

            SessionId = materialization.SessionId;
            Status = TerritoryTrailSessionStatus.Active;
            _phase = Phase.Validate;
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
                reason = completed ? "The compact apply is already complete." : "No compact apply is active.";
                return false;
            }

            try
            {
                while (workUnitsUsed < maxWorkUnits && Status == TerritoryTrailSessionStatus.Active)
                {
                    if (!TryProcessOne(out string stepReason))
                    {
                        BurnCandidate();
                        reason = stepReason;
                        return false;
                    }
                    workUnitsUsed++;
                    _totalWorkUnits++;
                }
            }
            catch (Exception exception) when (
                exception is OverflowException ||
                exception is ArgumentException ||
                exception is InvalidOperationException)
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
            out TerritoryCompactSnapshot snapshot,
            out TerritoryCompactCommitResult result,
            out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Committed || _candidate == null || _commitResult == null)
            {
                snapshot = null;
                result = null;
                reason = "Compact apply has not published a terminal candidate.";
                return false;
            }

            snapshot = _candidate;
            result = _commitResult;
            reason = null;
            return true;
        }

        public bool TryAbort(out string reason)
        {
            if (Status != TerritoryTrailSessionStatus.Active)
            {
                reason = "No compact apply is active.";
                return false;
            }
            BurnCandidate();
            reason = null;
            return true;
        }

        private bool TryProcessOne(out string reason)
        {
            switch (_phase)
            {
                case Phase.Validate:
                    if (!_boundary.TryValidateForwardArc(
                            _splice.RemovedIds,
                            out _predecessorId,
                            out _successorId,
                            out _wraps,
                            out reason))
                    {
                        return false;
                    }
                    if (!_boundary.TryGetSegment(_predecessorId, out TerritoryCompactBoundarySegment predecessor) ||
                        !_boundary.TryGetSegment(_successorId, out TerritoryCompactBoundarySegment successor) ||
                        predecessor.GlobalEnd != _splice.InsertedSegments[0].GlobalStart ||
                        _splice.InsertedSegments[_splice.InsertedSegments.Count - 1].GlobalEnd != successor.GlobalStart)
                    {
                        reason = "Boundary splice does not connect exactly to its retained neighbors.";
                        return false;
                    }
                    _phase = Phase.RemoveBoundary;
                    reason = null;
                    return true;

                case Phase.RemoveBoundary:
                    if (_removeIndex < _splice.RemovedIds.Count)
                    {
                        TerritoryBoundarySegmentId id = _splice.RemovedIds[_removeIndex++];
                        if (!_boundary.TryGetSegment(id, out TerritoryCompactBoundarySegment removed))
                        {
                            reason = $"Boundary identity {id} became stale during candidate apply.";
                            return false;
                        }
                        _boundary = _boundary.Remove(id, out bool didRemove, out int boundaryNodes);
                        if (!didRemove)
                        {
                            reason = $"Boundary identity {id} could not be removed.";
                            return false;
                        }
                        _persistentNodesCreated += boundaryNodes;
                        RemoveIndexes(removed);
                        reason = null;
                        return true;
                    }
                    _phase = Phase.InsertBoundary;
                    reason = null;
                    return true;

                case Phase.InsertBoundary:
                    if (_insertIndex < _splice.InsertedSegments.Count)
                    {
                        TerritoryBoundarySplice.Segment part = _splice.InsertedSegments[_insertIndex++];
                        var id = new TerritoryBoundarySegmentId(_nextIdentity++);
                        var segment = new TerritoryCompactBoundarySegment(id, part.Chunk, part.Start, part.End);
                        if (!_boundary.TryInsertBetween(
                                _predecessorId,
                                _successorId,
                                _wraps,
                                segment,
                                out TerritoryPersistentBoundaryTree nextBoundary,
                                out int boundaryNodes,
                                out reason))
                        {
                            return false;
                        }
                        _boundary = nextBoundary;
                        _persistentNodesCreated += boundaryNodes;
                        _predecessorId = id;
                        bool centerInside = _addedCenterInside.TryGetValue(part.Chunk, out bool inside) && inside;
                        AddIndexes(segment, centerInside);
                        reason = null;
                        return true;
                    }
                    _phase = Phase.UnionFullRuns;
                    reason = null;
                    return true;

                case Phase.UnionFullRuns:
                    if (_fullRunIndex < _materialization.FullRuns.Count)
                    {
                        UnionRow(_materialization.FullRuns[_fullRunIndex++]);
                        reason = null;
                        return true;
                    }
                    _phase = Phase.FinalizeBoundaryChunks;
                    reason = null;
                    return true;

                case Phase.FinalizeBoundaryChunks:
                    if (_editIndex < _materialization.BoundaryEdits.Count)
                    {
                        TerritoryChunkBoundaryEdit edit = _materialization.BoundaryEdits[_editIndex++];
                        bool wasInside = _source.TryGetBoundaryCenterInside(edit.Chunk, out bool sourceInside) && sourceInside;
                        bool centerInside = wasInside || edit.CenterInsideAddedRegion;
                        if (_boundaryByChunk.TryGetValue(edit.Chunk, out TerritoryCompactSnapshot.BoundaryBucket bucket))
                        {
                            TerritoryCompactSnapshot.BoundaryBucket changed = bucket.WithCenterInside(centerInside);
                            _boundaryByChunk = _boundaryByChunk.Set(edit.Chunk, changed, out _, out int nodes);
                            _persistentNodesCreated += nodes;
                            _changedBoundarySet.Add(edit.Chunk);
                        }
                        else if (centerInside)
                        {
                            UnionRow(new TerritoryChunkFillRun(edit.Chunk.Y, edit.Chunk.X, edit.Chunk.X));
                        }
                        reason = null;
                        return true;
                    }
                    _phase = Phase.Publish;
                    reason = null;
                    return true;

                case Phase.Publish:
                    decimal expectedArea = _source.Boundary.AbsoluteTwiceArea + _materialization.AddedAbsoluteTwiceArea;
                    if (_boundary.Count < 3 || _boundary.SignedTwiceArea != expectedArea)
                    {
                        reason = "Persistent Boundary splice area does not match the exact C009 result.";
                        return false;
                    }

                    var changedChunks = new List<TerritoryChunkCoordinate>(_changedBoundarySet);
                    changedChunks.Sort(TerritoryCompactSnapshotBuilder.ChunkComparer.Instance.Compare);
                    var changedRows = new List<int>(_changedRowSet);
                    changedRows.Sort();
                    var metrics = new TerritoryCompactApplyMetrics(
                        _splice.RemovedIds.Count,
                        _splice.InsertedSegments.Count,
                        changedChunks.Count,
                        changedRows.Count,
                        _persistentNodesCreated,
                        _totalWorkUnits + 1L);
                    _candidate = new TerritoryCompactSnapshot(
                        _source.Revision + 1UL,
                        _boundary,
                        _rows,
                        _boundaryByChunk,
                        _candidatesByChunk,
                        _nextIdentity);
                    _commitResult = new TerritoryCompactCommitResult(
                        _source.Revision,
                        _candidate.Revision,
                        changedChunks,
                        changedRows,
                        metrics);
                    Status = TerritoryTrailSessionStatus.Committed;
                    _phase = Phase.Done;
                    reason = null;
                    return true;

                default:
                    reason = "Compact apply entered an invalid work phase.";
                    return false;
            }
        }

        private void RemoveIndexes(TerritoryCompactBoundarySegment segment)
        {
            if (!_boundaryByChunk.TryGetValue(segment.Chunk, out TerritoryCompactSnapshot.BoundaryBucket bucket))
                throw new InvalidOperationException("Boundary Chunk index is missing a removed identity.");
            TerritoryCompactSnapshot.BoundaryBucket changed = bucket.Remove(segment.Id);
            int actualNodes;
            if (changed == null)
                _boundaryByChunk = _boundaryByChunk.Remove(segment.Chunk, out _, out actualNodes);
            else
                _boundaryByChunk = _boundaryByChunk.Set(segment.Chunk, changed, out _, out actualNodes);
            _persistentNodesCreated += actualNodes;
            _changedBoundarySet.Add(segment.Chunk);

            ForNeighborhood(segment.Chunk, chunk =>
            {
                if (!_candidatesByChunk.TryGetValue(chunk, out TerritoryCompactSnapshot.CandidateBucket candidate))
                    throw new InvalidOperationException("Boundary candidate index is missing a removed identity.");
                TerritoryCompactSnapshot.CandidateBucket next = candidate.Remove(segment.Id);
                int nodes;
                if (next == null)
                    _candidatesByChunk = _candidatesByChunk.Remove(chunk, out _, out nodes);
                else
                    _candidatesByChunk = _candidatesByChunk.Set(chunk, next, out _, out nodes);
                _persistentNodesCreated += nodes;
            });
        }

        private void AddIndexes(TerritoryCompactBoundarySegment segment, bool centerInside)
        {
            TerritoryCompactSnapshot.BoundaryBucket bucket;
            if (_boundaryByChunk.TryGetValue(segment.Chunk, out TerritoryCompactSnapshot.BoundaryBucket existing))
                bucket = existing.Add(segment.Id, existing.CenterInside || centerInside);
            else
                bucket = new TerritoryCompactSnapshot.BoundaryBucket(centerInside, new[] { segment.Id });
            _boundaryByChunk = _boundaryByChunk.Set(segment.Chunk, bucket, out _, out int actualNodes);
            _persistentNodesCreated += actualNodes;
            _changedBoundarySet.Add(segment.Chunk);

            ForNeighborhood(segment.Chunk, chunk =>
            {
                TerritoryCompactSnapshot.CandidateBucket candidate;
                if (_candidatesByChunk.TryGetValue(chunk, out TerritoryCompactSnapshot.CandidateBucket existingCandidate))
                    candidate = existingCandidate.Add(segment.Id);
                else
                    candidate = new TerritoryCompactSnapshot.CandidateBucket(new[] { segment.Id });
                _candidatesByChunk = _candidatesByChunk.Set(chunk, candidate, out _, out int nodes);
                _persistentNodesCreated += nodes;
            });
        }

        private void UnionRow(TerritoryChunkFillRun addition)
        {
            TerritoryCompactRowCoverage row;
            if (_rows.TryGetValue(addition.Y, out TerritoryCompactRowCoverage existing))
                row = existing.Union(addition);
            else
                row = new TerritoryCompactRowCoverage(addition.Y, new[] { addition });
            _rows = _rows.Set(addition.Y, row, out _, out int nodes);
            _persistentNodesCreated += nodes;
            _changedRowSet.Add(addition.Y);
        }

        private static void ForNeighborhood(
            TerritoryChunkCoordinate source,
            Action<TerritoryChunkCoordinate> action)
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
                    action(new TerritoryChunkCoordinate((int)candidateX, (int)candidateY));
                }
            }
        }

        private void BurnCandidate()
        {
            _boundary = null;
            _rows = null;
            _boundaryByChunk = null;
            _candidatesByChunk = null;
            _candidate = null;
            _commitResult = null;
            _phase = Phase.None;
            Status = TerritoryTrailSessionStatus.Aborted;
        }

        private void ResetCandidate()
        {
            _source = null;
            _materialization = null;
            _splice = null;
            _boundary = null;
            _rows = null;
            _boundaryByChunk = null;
            _candidatesByChunk = null;
            _changedBoundarySet.Clear();
            _changedRowSet.Clear();
            _addedCenterInside.Clear();
            _candidate = null;
            _commitResult = null;
            _predecessorId = default;
            _successorId = default;
            _wraps = false;
            _nextIdentity = 0UL;
            _removeIndex = 0;
            _insertIndex = 0;
            _fullRunIndex = 0;
            _editIndex = 0;
            _persistentNodesCreated = 0L;
            _totalWorkUnits = 0L;
            LastStepWorkUnits = 0;
            _phase = Phase.None;
        }

        private enum Phase
        {
            None,
            Validate,
            RemoveBoundary,
            InsertBoundary,
            UnionFullRuns,
            FinalizeBoundaryChunks,
            Publish,
            Done
        }
    }
}
