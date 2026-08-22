using System;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactStore
    {
        public TerritoryCompactStore(TerritoryCompactSnapshot initialSnapshot)
        {
            Current = initialSnapshot ?? throw new ArgumentNullException(nameof(initialSnapshot));
        }

        public TerritoryCompactSnapshot Current { get; private set; }

        public bool TryBeginApply(
            TerritoryChunkExpansionMaterialization materialization,
            out TerritoryCompactApplySession session,
            out string reason)
        {
            session = new TerritoryCompactApplySession();
            if (session.TryBegin(Current, materialization, out reason))
                return true;

            session = null;
            return false;
        }

        public bool TryPublish(
            TerritoryCompactApplySession session,
            out TerritoryCompactCommitResult result,
            out string reason)
        {
            result = null;
            if (session == null)
            {
                reason = "A compact Store publish requires an apply session.";
                return false;
            }
            if (!ReferenceEquals(session.Source, Current))
            {
                reason = "Compact apply source is stale or belongs to a different Store.";
                return false;
            }
            if (!session.TryGetResult(out TerritoryCompactSnapshot candidate, out result, out reason))
                return false;
            if (candidate.Revision != Current.Revision + 1UL || result.PreviousRevision != Current.Revision)
            {
                result = null;
                reason = "Compact apply revision contract is invalid.";
                return false;
            }

            Current = candidate;
            reason = null;
            return true;
        }
    }
}
