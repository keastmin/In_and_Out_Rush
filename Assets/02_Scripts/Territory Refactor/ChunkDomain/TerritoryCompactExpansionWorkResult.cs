namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactExpansionWorkResult
    {
        private TerritoryCompactExpansionWorkResult(
            ulong sessionId,
            ulong sourceRevision,
            TerritoryCompactSnapshot candidate,
            TerritoryCompactApplySession applySession,
            TerritoryCompactExpansionWorkerMetrics metrics,
            string failureReason)
        {
            SessionId = sessionId;
            SourceRevision = sourceRevision;
            Candidate = candidate;
            ApplySession = applySession;
            Metrics = metrics;
            FailureReason = failureReason;
        }

        public ulong SessionId { get; }
        public ulong SourceRevision { get; }
        public TerritoryCompactSnapshot Candidate { get; }
        public TerritoryCompactApplySession ApplySession { get; }
        public TerritoryCompactExpansionWorkerMetrics Metrics { get; }
        public string FailureReason { get; }
        public bool IsSuccess => Candidate != null && ApplySession != null && string.IsNullOrEmpty(FailureReason);

        internal static TerritoryCompactExpansionWorkResult Success(
            TerritoryCompactExpansionWorkItem item,
            TerritoryCompactSnapshot candidate,
            TerritoryCompactApplySession applySession,
            TerritoryCompactExpansionWorkerMetrics metrics)
            => new(
                item.SessionId,
                item.ExpectedSourceRevision,
                candidate,
                applySession,
                metrics,
                null);

        internal static TerritoryCompactExpansionWorkResult Failure(
            TerritoryCompactExpansionWorkItem item,
            string reason)
            => new(
                item.SessionId,
                item.ExpectedSourceRevision,
                null,
                null,
                default,
                reason);
    }
}
