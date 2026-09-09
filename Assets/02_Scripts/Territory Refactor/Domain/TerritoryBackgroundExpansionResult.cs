using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBackgroundExpansionResult
    {
        private TerritoryBackgroundExpansionResult(
            ulong sessionId,
            ulong sourceRevision,
            TerritoryExpansionPresentationData presentation,
            IReadOnlyList<TerritoryExpansionResultPacket> packets,
            TimeSpan elapsed,
            int workerThreadId,
            string failureReason)
        {
            SessionId = sessionId;
            SourceRevision = sourceRevision;
            Presentation = presentation;
            Packets = packets;
            Elapsed = elapsed;
            WorkerThreadId = workerThreadId;
            FailureReason = failureReason;
        }

        public ulong SessionId { get; }
        public ulong SourceRevision { get; }
        public TerritoryExpansionPresentationData Presentation { get; }
        public IReadOnlyList<TerritoryExpansionResultPacket> Packets { get; }
        public TimeSpan Elapsed { get; }
        public int WorkerThreadId { get; }
        public string FailureReason { get; }
        public bool IsSuccess => Presentation != null && string.IsNullOrEmpty(FailureReason);

        internal static TerritoryBackgroundExpansionResult Success(
            TerritoryBackgroundExpansionWorkItem item,
            TerritoryExpansionPresentationData presentation,
            IReadOnlyList<TerritoryExpansionResultPacket> packets,
            TimeSpan elapsed,
            int workerThreadId)
            => new(
                item.SessionId,
                item.SourceRevision,
                presentation,
                packets,
                elapsed,
                workerThreadId,
                null);

        internal static TerritoryBackgroundExpansionResult Failure(
            TerritoryBackgroundExpansionWorkItem item,
            string reason,
            TimeSpan elapsed,
            int workerThreadId)
            => Failure(
                item.SessionId,
                item.SourceRevision,
                reason,
                elapsed,
                workerThreadId);

        internal static TerritoryBackgroundExpansionResult Failure(
            ulong sessionId,
            ulong sourceRevision,
            string reason,
            TimeSpan elapsed,
            int workerThreadId)
            => new(
                sessionId,
                sourceRevision,
                null,
                Array.Empty<TerritoryExpansionResultPacket>(),
                elapsed,
                workerThreadId,
                string.IsNullOrEmpty(reason) ? "Background Territory expansion failed." : reason);
    }
}
