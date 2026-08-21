using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkTransferPacket
    {
        private readonly ReadOnlyCollection<int> _words;

        public TerritoryChunkTransferPacket(
            ulong baseRevision,
            ulong revision,
            uint sequence,
            IReadOnlyList<int> words)
        {
            if (baseRevision == ulong.MaxValue || revision != baseRevision + 1UL)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (words == null)
                throw new ArgumentNullException(nameof(words));
            if (words.Count == 0 || words.Count > TerritoryChunkTransferPacketizer.MaximumWordCount)
                throw new ArgumentOutOfRangeException(nameof(words));

            var copy = new int[words.Count];
            for (int i = 0; i < words.Count; i++)
                copy[i] = words[i];

            BaseRevision = baseRevision;
            Revision = revision;
            Sequence = sequence;
            _words = Array.AsReadOnly(copy);
        }

        public ulong BaseRevision { get; }
        public ulong Revision { get; }
        public uint Sequence { get; }
        public IReadOnlyList<int> Words => _words;

        public int[] CopyWords()
        {
            var copy = new int[_words.Count];
            for (int i = 0; i < _words.Count; i++)
                copy[i] = _words[i];
            return copy;
        }
    }
}
