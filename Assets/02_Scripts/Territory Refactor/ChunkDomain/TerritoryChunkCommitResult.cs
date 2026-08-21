using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkCommitResult
    {
        private readonly ReadOnlyCollection<TerritoryChunkCoverage> _changedChunks;

        public TerritoryChunkCommitResult(
            ulong baseRevision,
            ulong revision,
            IReadOnlyList<TerritoryChunkCoverage> changedChunks)
        {
            if (revision == 0 || revision != baseRevision + 1)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (changedChunks == null)
                throw new ArgumentNullException(nameof(changedChunks));

            var copy = new TerritoryChunkCoverage[changedChunks.Count];
            for (int i = 0; i < changedChunks.Count; i++)
                copy[i] = changedChunks[i] ?? throw new ArgumentException("Changed coverage cannot be null.");

            BaseRevision = baseRevision;
            Revision = revision;
            _changedChunks = Array.AsReadOnly(copy);
        }

        public ulong BaseRevision { get; }
        public ulong Revision { get; }
        public IReadOnlyList<TerritoryChunkCoverage> ChangedChunks => _changedChunks;
    }
}
