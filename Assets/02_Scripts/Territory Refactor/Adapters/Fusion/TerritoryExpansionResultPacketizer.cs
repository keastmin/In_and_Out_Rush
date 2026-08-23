using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionResultPacketizer
    {
        public const int MaximumWordsPerPacket = 48;

        public bool TryCreate(
            TerritoryExpansionPresentationData presentation,
            out IReadOnlyList<TerritoryExpansionResultPacket> packets,
            out string reason)
        {
            packets = Array.Empty<TerritoryExpansionResultPacket>();
            if (presentation == null)
            {
                reason = "Expansion packetizer requires a completed result.";
                return false;
            }

            long wordCountLong = (long)presentation.Vertices.Count * 2L + presentation.Triangles.Count;
            if (wordCountLong <= 0L || wordCountLong > int.MaxValue)
            {
                reason = "Expansion result exceeds the supported packet size.";
                return false;
            }

            int wordCount = (int)wordCountLong;
            int packetCount = (wordCount + MaximumWordsPerPacket - 1) / MaximumWordsPerPacket;
            var result = new TerritoryExpansionResultPacket[packetCount];
            int vertexWordCount = presentation.Vertices.Count * 2;
            int wordOffset = 0;
            for (int packetIndex = 0; packetIndex < packetCount; packetIndex++)
            {
                int length = Math.Min(MaximumWordsPerPacket, wordCount - wordOffset);
                var words = new int[length];
                for (int local = 0; local < length; local++, wordOffset++)
                {
                    if (wordOffset < vertexWordCount)
                    {
                        var bits = new FloatBits
                        {
                            Float = (wordOffset & 1) == 0
                                ? presentation.Vertices[wordOffset / 2].x
                                : presentation.Vertices[wordOffset / 2].y
                        };
                        words[local] = bits.Integer;
                    }
                    else
                    {
                        words[local] = presentation.Triangles[wordOffset - vertexWordCount];
                    }
                }

                result[packetIndex] = new TerritoryExpansionResultPacket(
                    presentation.SourceRevision,
                    presentation.Revision,
                    (uint)packetIndex,
                    words);
            }

            packets = result;
            reason = null;
            return true;
        }

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatBits
        {
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public int Integer;
        }
    }
}
