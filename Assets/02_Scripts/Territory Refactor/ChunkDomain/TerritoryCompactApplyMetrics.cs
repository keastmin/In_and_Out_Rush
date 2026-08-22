namespace ProjectIO.Territory
{
    public readonly struct TerritoryCompactApplyMetrics
    {
        internal TerritoryCompactApplyMetrics(
            long removedBoundarySegments,
            long insertedBoundarySegments,
            long touchedBoundaryChunks,
            long touchedFullRows,
            long persistentNodesCreated,
            long totalWorkUnits)
        {
            RemovedBoundarySegments = removedBoundarySegments;
            InsertedBoundarySegments = insertedBoundarySegments;
            TouchedBoundaryChunks = touchedBoundaryChunks;
            TouchedFullRows = touchedFullRows;
            PersistentNodesCreated = persistentNodesCreated;
            TotalWorkUnits = totalWorkUnits;
        }

        public long RemovedBoundarySegments { get; }
        public long InsertedBoundarySegments { get; }
        public long TouchedBoundaryChunks { get; }
        public long TouchedFullRows { get; }
        public long PersistentNodesCreated { get; }
        public long TotalWorkUnits { get; }
        public long SourceWideBoundaryScans => 0L;
        public long SourceWideFullChunkScans => 0L;
        public long GlobalBoundaryRenumbers => 0L;
        public long ExpandedFullChunkObjects => 0L;
        public long UnchangedNodeCopies => 0L;
    }
}
