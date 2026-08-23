using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBackgroundExpansionWorkItem
    {
        private TerritoryBackgroundExpansionWorkItem(
            ulong sessionId,
            ulong sourceRevision,
            Vector2[] sourceVertices,
            Vector2[] trailPoints)
        {
            SessionId = sessionId;
            SourceRevision = sourceRevision;
            SourceVertices = sourceVertices;
            TrailPoints = trailPoints;
        }

        public ulong SessionId { get; }
        public ulong SourceRevision { get; }
        public IReadOnlyList<Vector2> SourceVertices { get; }
        public IReadOnlyList<Vector2> TrailPoints { get; }

        public static bool TryCreate(
            ulong sessionId,
            ulong sourceRevision,
            IReadOnlyList<Vector2> sourceVertices,
            IReadOnlyList<Vector2> trailPoints,
            out TerritoryBackgroundExpansionWorkItem item,
            out string reason)
        {
            item = null;
            if (sessionId == 0UL || sourceRevision == 0UL)
            {
                reason = "Background expansion requires non-zero session and source revisions.";
                return false;
            }
            if (sourceVertices == null || sourceVertices.Count < 3 ||
                trailPoints == null || trailPoints.Count < 2)
            {
                reason = "Background expansion requires a Territory polygon and Trail.";
                return false;
            }

            try
            {
                var sourceCopy = new Vector2[sourceVertices.Count];
                var trailCopy = new Vector2[trailPoints.Count];
                for (int i = 0; i < sourceCopy.Length; i++)
                    sourceCopy[i] = sourceVertices[i];
                for (int i = 0; i < trailCopy.Length; i++)
                    trailCopy[i] = trailPoints[i];

                item = new TerritoryBackgroundExpansionWorkItem(
                    sessionId,
                    sourceRevision,
                    sourceCopy,
                    trailCopy);
                reason = null;
                return true;
            }
            catch (Exception exception)
            {
                reason = $"Background expansion snapshot failed: {exception.Message}";
                return false;
            }
        }
    }
}
