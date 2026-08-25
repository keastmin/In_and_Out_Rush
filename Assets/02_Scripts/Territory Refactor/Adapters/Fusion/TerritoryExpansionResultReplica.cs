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
            if (sourceRevision == 0UL || revision != sourceRevision + 1UL)
            {
                reason = "Expansion result revision is invalid.";
                return false;
            }
            if (revision <= CurrentRevision)
            {
                reason = null;
                return true;
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

            if (IsReceiving)
            {
                if (sourceRevision == _sourceRevision && revision == _revision &&
                    vertexCount == _vertexCount && triangleCount == _triangleCount &&
                    packetCount == _packetCount)
                {
                    reason = null;
                    return true;
                }

                if (revision <= _revision)
                {
                    reason = "Expansion result conflicts with the current inbound transfer.";
                    return false;
                }

                ResetInbound();
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
            if (!IsReceiving)
            {
                if (revision <= CurrentRevision)
                {
                    reason = null;
                    return true;
                }

                reason = "Expansion result packet arrived without an inbound transfer.";
                return false;
            }
            if (sourceRevision != _sourceRevision || revision != _revision || words == null)
            {
                if (revision <= CurrentRevision)
                {
                    reason = null;
                    return true;
                }

                reason = "Expansion result packet does not match the inbound transfer.";
                return false;
            }

            if (sequence >= (uint)_packetCount)
            {
                reason = "Expansion result packet sequence exceeds the transfer length.";
                return false;
            }

            int packetOffset = checked((int)sequence *
                TerritoryExpansionResultPacketizer.MaximumWordsPerPacket);
            int expectedLength = Math.Min(
                TerritoryExpansionResultPacketizer.MaximumWordsPerPacket,
                _words.Length - packetOffset);
            if (words.Length != expectedLength)
            {
                reason = "Expansion result packet payload length is invalid.";
                return false;
            }

            if (sequence < _nextPacketSequence)
            {
                for (int i = 0; i < words.Length; i++)
                {
                    if (_words[packetOffset + i] != words[i])
                    {
                        reason = "Expansion result replay packet conflicts with received data.";
                        return false;
                    }
                }

                reason = null;
                return true;
            }
            if (sequence != _nextPacketSequence)
            {
                reason = "Expansion result packet sequence is out of order.";
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
            if (!IsReceiving)
            {
                if (revision <= CurrentRevision)
                {
                    reason = null;
                    return true;
                }

                reason = "Expansion result terminal arrived without an inbound transfer.";
                return false;
            }
            if (sourceRevision != _sourceRevision || revision != _revision ||
                _nextPacketSequence != (uint)_packetCount || _wordOffset != _words.Length)
            {
                reason = "Expansion result terminal arrived before the complete payload.";
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
