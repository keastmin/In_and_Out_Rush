using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactExpansionWorkItem
    {
        private readonly IReadOnlyList<TerritoryTrailFragment> _fragments;

        private TerritoryCompactExpansionWorkItem(
            ulong sessionId,
            ulong expectedSourceRevision,
            List<TerritoryTrailFragment> ownedFragments)
        {
            SessionId = sessionId;
            ExpectedSourceRevision = expectedSourceRevision;
            _fragments = ownedFragments.AsReadOnly();
        }

        public ulong SessionId { get; }
        public ulong ExpectedSourceRevision { get; }
        public IReadOnlyList<TerritoryTrailFragment> Fragments => _fragments;

        public static bool TryTakeOwnership(
            ulong sessionId,
            ulong expectedSourceRevision,
            List<TerritoryTrailFragment> ownedFragments,
            out TerritoryCompactExpansionWorkItem item,
            out string reason)
        {
            item = null;
            if (sessionId == 0)
            {
                reason = "A compact expansion work item requires a non-zero SessionId.";
                return false;
            }
            if (expectedSourceRevision == 0 || expectedSourceRevision == ulong.MaxValue)
            {
                reason = "A compact expansion work item requires an incrementable source revision.";
                return false;
            }
            if (ownedFragments == null || ownedFragments.Count == 0)
            {
                reason = "A compact expansion work item requires confirmed Trail fragments.";
                return false;
            }

            for (int i = 0; i < ownedFragments.Count; i++)
            {
                TerritoryTrailFragment fragment = ownedFragments[i];
                if (fragment == null || fragment.SessionId != sessionId || fragment.Sequence != (uint)i)
                {
                    reason = "Owned Trail fragments must be non-null and ordered for the work SessionId.";
                    return false;
                }
                if (i > 0)
                {
                    IReadOnlyList<FixedTerritoryPoint> previous = ownedFragments[i - 1].Points;
                    if (previous[previous.Count - 1] != fragment.Points[0])
                    {
                        reason = "Owned Trail fragments must form one continuous path.";
                        return false;
                    }
                }
            }

            item = new TerritoryCompactExpansionWorkItem(
                sessionId,
                expectedSourceRevision,
                ownedFragments);
            reason = null;
            return true;
        }
    }
}
