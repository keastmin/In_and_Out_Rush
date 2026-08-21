using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkSnapshot
    {
        private readonly ReadOnlyDictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage> _chunks;

        public TerritoryChunkSnapshot(
            ulong revision,
            IReadOnlyDictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage> chunks)
        {
            if (chunks == null)
                throw new ArgumentNullException(nameof(chunks));
            if (revision == 0 && chunks.Count != 0)
                throw new ArgumentException("Revision zero is reserved for an empty uninitialized snapshot.", nameof(revision));

            var copy = new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>(chunks.Count);
            foreach (KeyValuePair<TerritoryChunkCoordinate, TerritoryChunkCoverage> pair in chunks)
            {
                if (pair.Value == null)
                    throw new ArgumentException("Snapshot coverage cannot be null.", nameof(chunks));
                if (pair.Key != pair.Value.Chunk)
                    throw new ArgumentException("Snapshot key and coverage Chunk must match.", nameof(chunks));
                if (pair.Value.Fill == TerritoryChunkFill.Empty)
                    throw new ArgumentException("Empty Chunks must be omitted from the sparse snapshot.", nameof(chunks));

                copy.Add(pair.Key, pair.Value);
            }

            Revision = revision;
            _chunks = new ReadOnlyDictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>(copy);
        }

        public ulong Revision { get; }
        public IReadOnlyDictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage> Chunks => _chunks;

        public TerritoryChunkFill GetFill(TerritoryChunkCoordinate chunk)
            => _chunks.TryGetValue(chunk, out TerritoryChunkCoverage coverage)
                ? coverage.Fill
                : TerritoryChunkFill.Empty;

        public bool TryGetCoverage(
            TerritoryChunkCoordinate chunk,
            out TerritoryChunkCoverage coverage)
            => _chunks.TryGetValue(chunk, out coverage);
    }
}
