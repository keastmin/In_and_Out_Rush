using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionResultReplica
    {
        private ulong _sourceRevision;
        private ulong _revision;
        private int _vertexCount;
        private int _triangleCount;
        private int _packetCount;
        private int[] _words;
        private int _wordOffset;
        private uint _nextPacketSequence;

        public ulong CurrentRevision { get; private set; }
        public bool IsReceiving => _words != null;

        public void Reset(ulong initialRevision)
        {
            CurrentRevision = initialRevision;
            ResetInbound();
        }

        public bool TryBegin(
            ulong sourceRevision,
            ulong revision,
            int vertexCount,
            int triangleCount,
            int packetCount,
            out string reason)
        {
            if (IsReceiving)
            {
                reason = "An expansion result is already being received.";
                return false;
            }
            if (sourceRevision != CurrentRevision || revision != sourceRevision + 1UL)
            {
                reason = "Expansion result revision is stale or out of order.";
                return false;
            }
            long triangleCountLong = ((long)vertexCount - 2L) * 3L;
            long wordCountLong = (long)vertexCount * 2L + triangleCount;
            if (vertexCount < 3 || triangleCountLong != triangleCount ||
                wordCountLong <= 0L || wordCountLong > int.MaxValue)
            {
                reason = "Expansion result dimensions are invalid.";
                return false;
            }
            int expectedPacketCount = ((int)wordCountLong +
                TerritoryExpansionResultPacketizer.MaximumWordsPerPacket - 1) /
                TerritoryExpansionResultPacketizer.MaximumWordsPerPacket;
            if (packetCount != expectedPacketCount)
            {
                reason = "Expansion result packet count is invalid.";
                return false;
            }

            _sourceRevision = sourceRevision;
            _revision = revision;
            _vertexCount = vertexCount;
            _triangleCount = triangleCount;
            _packetCount = packetCount;
            _words = new int[(int)wordCountLong];
            _wordOffset = 0;
            _nextPacketSequence = 0U;
            reason = null;
            return true;
        }

        public bool TryAppend(
            ulong sourceRevision,
            ulong revision,
            uint sequence,
            int[] words,
            out string reason)
        {
            if (!IsReceiving || sourceRevision != _sourceRevision || revision != _revision ||
                sequence != _nextPacketSequence || words == null)
            {
                reason = "Expansion result packet is stale, missing or out of order.";
                ResetInbound();
                return false;
            }

            int remaining = _words.Length - _wordOffset;
            int expectedLength = Math.Min(
                TerritoryExpansionResultPacketizer.MaximumWordsPerPacket,
                remaining);
            if (words.Length != expectedLength)
            {
                reason = "Expansion result packet payload length is invalid.";
                ResetInbound();
                return false;
            }

            Array.Copy(words, 0, _words, _wordOffset, words.Length);
            _wordOffset += words.Length;
            _nextPacketSequence++;
            reason = null;
            return true;
        }

        public bool TryComplete(
            ulong sourceRevision,
            ulong revision,
            out TerritoryExpansionPresentationData presentation,
            out string reason)
        {
            presentation = null;
            if (!IsReceiving || sourceRevision != _sourceRevision || revision != _revision ||
                _nextPacketSequence != (uint)_packetCount || _wordOffset != _words.Length)
            {
                reason = "Expansion result terminal arrived before the complete payload.";
                ResetInbound();
                return false;
            }

            var vertices = new Vector2[_vertexCount];
            var triangles = new int[_triangleCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                var x = new FloatBits { Integer = _words[i * 2] };
                var y = new FloatBits { Integer = _words[i * 2 + 1] };
                if (!IsFinite(x.Float) || !IsFinite(y.Float))
                {
                    reason = "Expansion result contains a non-finite vertex.";
                    ResetInbound();
                    return false;
                }
                vertices[i] = new Vector2(x.Float, y.Float);
            }

            int triangleOffset = vertices.Length * 2;
            for (int i = 0; i < triangles.Length; i++)
            {
                int index = _words[triangleOffset + i];
                if (index < 0 || index >= vertices.Length)
                {
                    reason = "Expansion result contains an invalid triangle index.";
                    ResetInbound();
                    return false;
                }
                triangles[i] = index;
            }

            presentation = TerritoryExpansionPresentationData.TakeOwnershipValidated(
                _sourceRevision,
                _revision,
                vertices,
                triangles);
            CurrentRevision = _revision;
            ResetInbound();
            reason = null;
            return true;
        }

        private void ResetInbound()
        {
            _sourceRevision = 0UL;
            _revision = 0UL;
            _vertexCount = 0;
            _triangleCount = 0;
            _packetCount = 0;
            _words = null;
            _wordOffset = 0;
            _nextPacketSequence = 0U;
        }

        private static bool IsFinite(float value)
            => !float.IsNaN(value) && !float.IsInfinity(value);

        [StructLayout(LayoutKind.Explicit)]
        private struct FloatBits
        {
            [FieldOffset(0)] public float Float;
            [FieldOffset(0)] public int Integer;
        }
    }
}
