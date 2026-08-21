using System;
using System.Collections.Generic;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkReplica
    {
        private const int RunRecord = 1;
        private const int BoundaryRecord = 2;
        private const int MaximumTransactionWords = 4 * 1024 * 1024;

        private readonly List<int> _receivedWords = new();
        private readonly HashSet<TerritoryChunkCoordinate> _receivedChunks = new();

        private TerritoryChunkTransferKind _kind;
        private ulong _baseRevision;
        private ulong _revision;
        private uint _expectedPacketSequence;
        private int _expectedPacketCount;
        private bool _isReceiving;

        public TerritoryChunkReplica()
        {
            Current = EmptySnapshot();
        }

        public TerritoryChunkSnapshot Current { get; private set; }
        public bool IsReceiving => _isReceiving;
        public ulong IncomingRevision => _isReceiving ? _revision : 0;
        public ulong IncomingBaseRevision => _isReceiving ? _baseRevision : 0;
        public TerritoryChunkTransferKind IncomingKind => _isReceiving ? _kind : default;

        public bool TryBegin(
            TerritoryChunkTransferKind kind,
            ulong baseRevision,
            ulong revision,
            int packetCount,
            out string reason)
        {
            if (_isReceiving)
            {
                reason = "A Chunk transfer is already active.";
                return false;
            }
            if (packetCount <= 0)
            {
                reason = "Chunk transfer requires at least one packet.";
                return false;
            }
            if (kind == TerritoryChunkTransferKind.Delta)
            {
                if (baseRevision == ulong.MaxValue || revision != baseRevision + 1UL)
                {
                    reason = "Delta revisions must be consecutive.";
                    return false;
                }
                if (Current.Revision != baseRevision)
                {
                    reason = $"Delta base revision {baseRevision} does not match replica {Current.Revision}.";
                    return false;
                }
            }
            else if (kind == TerritoryChunkTransferKind.Snapshot)
            {
                if (baseRevision != 0 || revision == 0)
                {
                    reason = "Snapshot transfer revisions are invalid.";
                    return false;
                }
                if (revision < Current.Revision)
                {
                    reason = $"Snapshot revision {revision} is older than replica {Current.Revision}.";
                    return false;
                }
            }
            else
            {
                reason = "Unknown Chunk transfer kind.";
                return false;
            }

            _kind = kind;
            _baseRevision = baseRevision;
            _revision = revision;
            _expectedPacketCount = packetCount;
            _expectedPacketSequence = 0;
            _receivedWords.Clear();
            _isReceiving = true;
            reason = null;
            return true;
        }

        public bool TryAppend(TerritoryChunkTransferPacket packet, out string reason)
        {
            if (!_isReceiving)
            {
                reason = "No Chunk transfer is active.";
                return false;
            }
            if (packet == null)
            {
                reason = "Chunk transfer packet cannot be null.";
                return false;
            }
            if (packet.Kind != _kind || packet.BaseRevision != _baseRevision ||
                packet.Revision != _revision)
            {
                reason = "Chunk transfer packet metadata does not match the active transaction.";
                return false;
            }
            if (packet.Sequence != _expectedPacketSequence)
            {
                reason = $"Expected Chunk packet sequence {_expectedPacketSequence}, received {packet.Sequence}.";
                return false;
            }
            if (_expectedPacketSequence >= _expectedPacketCount)
            {
                reason = "Chunk transfer received more packets than declared.";
                return false;
            }
            if (_receivedWords.Count > MaximumTransactionWords - packet.Words.Count)
            {
                reason = "Chunk transfer exceeds the transaction word limit.";
                return false;
            }

            for (int i = 0; i < packet.Words.Count; i++)
                _receivedWords.Add(packet.Words[i]);
            _expectedPacketSequence++;
            reason = null;
            return true;
        }

        public bool TryComplete(out TerritoryChunkSnapshot snapshot, out string reason)
        {
            snapshot = null;
            if (!_isReceiving)
            {
                reason = "No Chunk transfer is active.";
                return false;
            }
            if (_expectedPacketSequence != _expectedPacketCount)
            {
                reason = $"Chunk transfer expected {_expectedPacketCount} packets and received {_expectedPacketSequence}.";
                Abort();
                return false;
            }

            bool built = TryBuildSnapshot(out TerritoryChunkSnapshot candidate, out reason);
            Abort();
            if (!built)
                return false;

            Current = candidate;
            snapshot = candidate;
            return true;
        }

        public void Abort()
        {
            _kind = default;
            _baseRevision = 0;
            _revision = 0;
            _expectedPacketSequence = 0;
            _expectedPacketCount = 0;
            _receivedWords.Clear();
            _receivedChunks.Clear();
            _isReceiving = false;
        }

        public void Reset()
        {
            Abort();
            Current = EmptySnapshot();
        }

        private bool TryBuildSnapshot(out TerritoryChunkSnapshot snapshot, out string reason)
        {
            snapshot = null;
            if (_receivedWords.Count == 0)
            {
                reason = "Chunk transfer payload is empty.";
                return false;
            }

            int recordCount = _receivedWords[0];
            if (recordCount < 0)
            {
                reason = "Chunk transfer record count is invalid.";
                return false;
            }

            var chunks = new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>();
            if (_kind == TerritoryChunkTransferKind.Delta)
            {
                foreach (KeyValuePair<TerritoryChunkCoordinate, TerritoryChunkCoverage> pair in Current.Chunks)
                    chunks.Add(pair.Key, pair.Value);
            }

            _receivedChunks.Clear();
            int offset = 1;
            for (int recordIndex = 0; recordIndex < recordCount; recordIndex++)
            {
                if (!TryReadWord(ref offset, out int recordType))
                {
                    reason = "Chunk transfer ended before its declared records.";
                    return false;
                }

                if (recordType == RunRecord)
                {
                    if (!TryReadRun(ref offset, chunks, out reason))
                        return false;
                }
                else if (recordType == BoundaryRecord)
                {
                    if (!TryReadBoundary(ref offset, chunks, out reason))
                        return false;
                }
                else
                {
                    reason = $"Unknown Chunk transfer record type {recordType}.";
                    return false;
                }
            }

            if (offset != _receivedWords.Count)
            {
                reason = "Chunk transfer contains trailing words.";
                return false;
            }

            try
            {
                snapshot = new TerritoryChunkSnapshot(_revision, chunks);
                reason = null;
                return true;
            }
            catch (Exception exception) when (
                exception is ArgumentException || exception is OverflowException)
            {
                reason = exception.Message;
                return false;
            }
        }

        private bool TryReadRun(
            ref int offset,
            Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage> chunks,
            out string reason)
        {
            if (!TryReadWord(ref offset, out int fillValue) ||
                !TryReadWord(ref offset, out int y) ||
                !TryReadWord(ref offset, out int startX) ||
                !TryReadWord(ref offset, out int runLength))
            {
                reason = "Chunk run record is incomplete.";
                return false;
            }
            if ((fillValue != (int)TerritoryChunkFill.Full &&
                 fillValue != (int)TerritoryChunkFill.Empty) || runLength <= 0)
            {
                reason = "Chunk run record has an invalid fill or length.";
                return false;
            }
            if (_kind == TerritoryChunkTransferKind.Snapshot &&
                fillValue == (int)TerritoryChunkFill.Empty)
            {
                reason = "Sparse snapshot transfer cannot contain Empty tombstones.";
                return false;
            }

            long lastX = (long)startX + runLength - 1L;
            if (lastX > int.MaxValue)
            {
                reason = "Chunk run exceeds the coordinate range.";
                return false;
            }

            for (int runIndex = 0; runIndex < runLength; runIndex++)
            {
                var chunk = new TerritoryChunkCoordinate(startX + runIndex, y);
                if (!_receivedChunks.Add(chunk))
                {
                    reason = $"Chunk transfer contains duplicate coverage for {chunk}.";
                    return false;
                }

                if (fillValue == (int)TerritoryChunkFill.Full)
                    chunks[chunk] = TerritoryChunkCoverage.Full(chunk);
                else
                    chunks.Remove(chunk);
            }

            reason = null;
            return true;
        }

        private bool TryReadBoundary(
            ref int offset,
            Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage> chunks,
            out string reason)
        {
            if (!TryReadWord(ref offset, out int y) ||
                !TryReadWord(ref offset, out int x) ||
                !TryReadWord(ref offset, out int centerInsideValue) ||
                !TryReadWord(ref offset, out int segmentCount))
            {
                reason = "Boundary record header is incomplete.";
                return false;
            }
            if ((centerInsideValue != 0 && centerInsideValue != 1) || segmentCount <= 0)
            {
                reason = "Boundary record header is invalid.";
                return false;
            }
            if (segmentCount > (_receivedWords.Count - offset) / 5)
            {
                reason = "Boundary record segment count exceeds the remaining payload.";
                return false;
            }

            var chunk = new TerritoryChunkCoordinate(x, y);
            if (!_receivedChunks.Add(chunk))
            {
                reason = $"Chunk transfer contains duplicate coverage for {chunk}.";
                return false;
            }

            var segments = new TerritoryChunkBoundarySegment[segmentCount];
            int previousSequence = -1;
            try
            {
                for (int segmentIndex = 0; segmentIndex < segmentCount; segmentIndex++)
                {
                    if (!TryReadWord(ref offset, out int sequence) ||
                        !TryReadWord(ref offset, out int startX) ||
                        !TryReadWord(ref offset, out int startY) ||
                        !TryReadWord(ref offset, out int endX) ||
                        !TryReadWord(ref offset, out int endY))
                    {
                        reason = "Boundary segment payload is incomplete.";
                        return false;
                    }
                    if (sequence <= previousSequence)
                    {
                        reason = "Boundary segment sequence must be strictly increasing.";
                        return false;
                    }

                    segments[segmentIndex] = new TerritoryChunkBoundarySegment(
                        sequence,
                        new TerritoryChunkLocalPoint(startX, startY),
                        new TerritoryChunkLocalPoint(endX, endY));
                    previousSequence = sequence;
                }

                chunks[chunk] = TerritoryChunkCoverage.Boundary(
                    chunk,
                    centerInsideValue == 1,
                    segments);
            }
            catch (Exception exception) when (exception is ArgumentException)
            {
                reason = exception.Message;
                return false;
            }

            reason = null;
            return true;
        }

        private bool TryReadWord(ref int offset, out int word)
        {
            if (offset >= _receivedWords.Count)
            {
                word = 0;
                return false;
            }

            word = _receivedWords[offset++];
            return true;
        }

        private static TerritoryChunkSnapshot EmptySnapshot()
            => new(0, new Dictionary<TerritoryChunkCoordinate, TerritoryChunkCoverage>());
    }
}
