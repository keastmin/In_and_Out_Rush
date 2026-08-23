using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionResultPacket
    {
        private readonly int[] _words;

        public TerritoryExpansionResultPacket(
            ulong sourceRevision,
            ulong revision,
            uint sequence,
            int[] words)
        {
            if (sourceRevision == 0UL || revision != sourceRevision + 1UL)
                throw new ArgumentOutOfRangeException(nameof(revision));
            if (words == null || words.Length == 0 ||
                words.Length > TerritoryExpansionResultPacketizer.MaximumWordsPerPacket)
            {
                throw new ArgumentOutOfRangeException(nameof(words));
            }

            SourceRevision = sourceRevision;
            Revision = revision;
            Sequence = sequence;
            _words = (int[])words.Clone();
        }

        public ulong SourceRevision { get; }
        public ulong Revision { get; }
        public uint Sequence { get; }
        public IReadOnlyList<int> Words => _words;
        public int[] CopyWords() => (int[])_words.Clone();
    }
}
