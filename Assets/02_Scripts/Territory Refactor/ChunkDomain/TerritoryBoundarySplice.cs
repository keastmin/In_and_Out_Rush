using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBoundarySplice
    {
        private readonly ReadOnlyCollection<TerritoryBoundarySegmentId> _removedIds;
        private readonly ReadOnlyCollection<Segment> _insertedSegments;

        private TerritoryBoundarySplice(
            List<TerritoryBoundarySegmentId> removedIds,
            List<Segment> insertedSegments)
        {
            _removedIds = removedIds.AsReadOnly();
            _insertedSegments = insertedSegments.AsReadOnly();
        }

        public IReadOnlyList<TerritoryBoundarySegmentId> RemovedIds => _removedIds;
        public IReadOnlyList<Segment> InsertedSegments => _insertedSegments;

        internal static bool TryCreate(
            TerritoryBoundaryLoopIndex boundaryIndex,
            TerritoryChunkExpansionPlan plan,
            IReadOnlyList<TerritoryChunkBoundaryEdit> edits,
            out TerritoryBoundarySplice splice,
            out string reason)
        {
            splice = null;
            reason = string.Empty;
            if (!boundaryIndex.IsPersistent || !plan.BoundaryForwardFromEntryToExit)
                return true;

            var removedSet = new HashSet<TerritoryBoundarySegmentId>();
            var trailParts = new List<TerritoryChunkBoundarySegment>();
            var trailChunks = new Dictionary<int, TerritoryChunkCoordinate>();
            for (int i = 0; i < edits.Count; i++)
            {
                TerritoryChunkBoundaryEdit edit = edits[i];
                for (int j = 0; j < edit.RemovedSourceIds.Count; j++)
                    removedSet.Add(edit.RemovedSourceIds[j]);
                for (int j = 0; j < edit.AddedTrailSegments.Count; j++)
                {
                    TerritoryChunkBoundarySegment part = edit.AddedTrailSegments[j];
                    if (!trailChunks.TryAdd(part.Sequence, edit.Chunk))
                    {
                        reason = $"Trail part sequence {part.Sequence} is duplicated.";
                        return false;
                    }
                    trailParts.Add(part);
                }
            }

            if (removedSet.Count == 0 || trailParts.Count == 0)
            {
                reason = "A persistent Boundary splice requires removed source identities and exact Trail parts.";
                return false;
            }

            TerritoryBoundarySegmentId firstRemovedId = plan.ExitBoundaryId;
            TerritoryBoundarySegmentId lastRemovedId = plan.EntryBoundaryId;
            if (!boundaryIndex.TryGetSegment(
                    firstRemovedId,
                    out _,
                    out FixedTerritoryPoint declaredExitEnd) ||
                !boundaryIndex.TryGetSegment(
                    lastRemovedId,
                    out FixedTerritoryPoint declaredEntryStart,
                    out _))
            {
                reason = "Boundary splice contact identities are stale.";
                return false;
            }
            if (plan.ExitPoint == declaredExitEnd &&
                !boundaryIndex.TryGetNextSegmentId(firstRemovedId, 1, out firstRemovedId))
            {
                reason = "Boundary splice cannot advance from an endpoint exit contact.";
                return false;
            }
            if (plan.EntryPoint == declaredEntryStart &&
                !boundaryIndex.TryGetNextSegmentId(lastRemovedId, -1, out lastRemovedId))
            {
                reason = "Boundary splice cannot retreat from an endpoint entry contact.";
                return false;
            }

            var removed = new List<TerritoryBoundarySegmentId>(removedSet.Count);
            TerritoryBoundarySegmentId current = firstRemovedId;
            for (int guard = 0; guard <= boundaryIndex.SegmentCount; guard++)
            {
                if (!removedSet.Contains(current))
                {
                    reason = "Removed Boundary identities do not form the expected exit-to-entry arc.";
                    return false;
                }
                removed.Add(current);
                if (current == lastRemovedId)
                    break;
                if (!boundaryIndex.TryGetNextSegmentId(current, 1, out current))
                {
                    reason = "Boundary splice could not advance to the entry identity.";
                    return false;
                }
            }
            if (removed.Count != removedSet.Count || removed[removed.Count - 1] != lastRemovedId)
            {
                reason = "Removed Boundary identities are non-contiguous or include an unrelated segment.";
                return false;
            }

            if (!boundaryIndex.TryGetSegmentPart(
                    firstRemovedId,
                    out TerritoryChunkCoordinate exitChunk,
                    out TerritoryChunkLocalPoint exitLocalStart,
                    out _) ||
                !boundaryIndex.TryGetSegmentPart(
                    lastRemovedId,
                    out TerritoryChunkCoordinate entryChunk,
                    out _,
                    out TerritoryChunkLocalPoint entryLocalEnd))
            {
                reason = "Boundary splice contact identities are stale.";
                return false;
            }

            trailParts.Sort((left, right) => left.Sequence.CompareTo(right.Sequence));
            var inserted = new List<Segment>(trailParts.Count + 2);
            FixedTerritoryPoint exitStart = exitLocalStart.ToGlobal(exitChunk);
            FixedTerritoryPoint entryEnd = entryLocalEnd.ToGlobal(entryChunk);
            if (exitStart != plan.ExitPoint)
                inserted.Add(new Segment(exitChunk, exitLocalStart, ToLocal(exitChunk, plan.ExitPoint)));
            for (int i = 0; i < trailParts.Count; i++)
            {
                TerritoryChunkBoundarySegment part = trailParts[i];
                TerritoryChunkCoordinate chunk = trailChunks[part.Sequence];
                inserted.Add(new Segment(chunk, part.Start, part.End));
            }
            if (plan.EntryPoint != entryEnd)
                inserted.Add(new Segment(entryChunk, ToLocal(entryChunk, plan.EntryPoint), entryLocalEnd));

            if (inserted.Count == 0)
            {
                reason = "Boundary splice cannot insert an empty replacement chain.";
                return false;
            }
            for (int i = 1; i < inserted.Count; i++)
            {
                if (inserted[i - 1].GlobalEnd != inserted[i].GlobalStart)
                {
                    reason = "Boundary splice replacement parts do not form one exact chain.";
                    return false;
                }
            }

            splice = new TerritoryBoundarySplice(removed, inserted);
            return true;
        }

        private static TerritoryChunkLocalPoint ToLocal(
            TerritoryChunkCoordinate chunk,
            FixedTerritoryPoint point)
        {
            long x = point.X - chunk.MinimumX;
            long y = point.Y - chunk.MinimumY;
            return new TerritoryChunkLocalPoint((int)x, (int)y);
        }

        public readonly struct Segment
        {
            public Segment(
                TerritoryChunkCoordinate chunk,
                TerritoryChunkLocalPoint start,
                TerritoryChunkLocalPoint end)
            {
                if (start == end)
                    throw new ArgumentException("A Boundary splice part cannot have zero length.");
                Chunk = chunk;
                Start = start;
                End = end;
            }

            public TerritoryChunkCoordinate Chunk { get; }
            public TerritoryChunkLocalPoint Start { get; }
            public TerritoryChunkLocalPoint End { get; }
            public FixedTerritoryPoint GlobalStart => Start.ToGlobal(Chunk);
            public FixedTerritoryPoint GlobalEnd => End.ToGlobal(Chunk);
        }
    }
}
