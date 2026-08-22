using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkBoundaryEdit
    {
        private readonly ReadOnlyCollection<TerritoryChunkBoundarySegment> _addedTrailSegments;
        private readonly ReadOnlyCollection<TerritoryChunkBoundarySegment> _replacedBoundarySegments;
        private readonly ReadOnlyCollection<int> _removedSourceSequences;

        internal TerritoryChunkBoundaryEdit(
            TerritoryChunkCoordinate chunk,
            bool centerInsideAddedRegion,
            List<TerritoryChunkBoundarySegment> addedTrailSegments,
            List<TerritoryChunkBoundarySegment> replacedBoundarySegments,
            List<int> removedSourceSequences)
        {
            Chunk = chunk;
            CenterInsideAddedRegion = centerInsideAddedRegion;
            _addedTrailSegments = (addedTrailSegments ?? throw new ArgumentNullException(nameof(addedTrailSegments))).AsReadOnly();
            _replacedBoundarySegments = (replacedBoundarySegments ?? throw new ArgumentNullException(nameof(replacedBoundarySegments))).AsReadOnly();
            _removedSourceSequences = (removedSourceSequences ?? throw new ArgumentNullException(nameof(removedSourceSequences))).AsReadOnly();

            if (_addedTrailSegments.Count + _replacedBoundarySegments.Count == 0)
                throw new ArgumentException("A Boundary edit requires at least one exact local segment.");
        }

        public TerritoryChunkCoordinate Chunk { get; }
        public bool CenterInsideAddedRegion { get; }
        public IReadOnlyList<TerritoryChunkBoundarySegment> AddedTrailSegments => _addedTrailSegments;
        public IReadOnlyList<TerritoryChunkBoundarySegment> ReplacedBoundarySegments => _replacedBoundarySegments;
        public IReadOnlyList<int> RemovedSourceSequences => _removedSourceSequences;
        public int CapturedRegionSegmentCount =>
            _addedTrailSegments.Count + _replacedBoundarySegments.Count;
    }
}
