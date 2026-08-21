using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryChunkTransferPacketizer
    {
        private const int RunRecord = 1;
        private const int BoundaryRecord = 2;

        private readonly List<TerritoryChunkCoverage> _orderedCoverage = new();
        private readonly List<int> _words = new();
        private readonly List<TerritoryChunkTransferPacket> _packets = new();

        public const int MaximumWordCount = 48;

        public bool TryCreateDelta(
            TerritoryChunkCommitResult result,
            out IReadOnlyList<TerritoryChunkTransferPacket> packets,
            out string reason)
        {
            if (result == null)
            {
                packets = null;
                reason = "Chunk delta cannot be null.";
                return false;
            }

            return TryCreate(
                result.BaseRevision,
                result.Revision,
                result.ChangedChunks,
                out packets,
                out reason);
        }

        private bool TryCreate(
            ulong baseRevision,
            ulong revision,
            IReadOnlyList<TerritoryChunkCoverage> coverage,
            out IReadOnlyList<TerritoryChunkTransferPacket> packets,
            out string reason)
        {
            packets = null;
            if (coverage == null)
            {
                reason = "Chunk coverage cannot be null.";
                return false;
            }
            if (baseRevision == ulong.MaxValue || revision != baseRevision + 1UL)
            {
                reason = "Delta revisions must be consecutive.";
                return false;
            }

            _orderedCoverage.Clear();
            for (int i = 0; i < coverage.Count; i++)
            {
                TerritoryChunkCoverage item = coverage[i];
                if (item == null)
                {
                    reason = "Chunk coverage cannot contain null.";
                    return false;
                }
                _orderedCoverage.Add(item);
            }

            _orderedCoverage.Sort(CompareCoverage);
            for (int i = 1; i < _orderedCoverage.Count; i++)
            {
                if (_orderedCoverage[i - 1].Chunk == _orderedCoverage[i].Chunk)
                {
                    reason = $"Duplicate Chunk coverage {_orderedCoverage[i].Chunk}.";
                    return false;
                }
            }

            _words.Clear();
            _words.Add(0);
            int recordCount = 0;
            for (int index = 0; index < _orderedCoverage.Count;)
            {
                TerritoryChunkCoverage item = _orderedCoverage[index];
                if (item.Fill == TerritoryChunkFill.Full || item.Fill == TerritoryChunkFill.Empty)
                {
                    int runLength = 1;
                    while (index + runLength < _orderedCoverage.Count &&
                           CanJoinRun(item, _orderedCoverage[index + runLength], runLength))
                    {
                        runLength++;
                    }

                    _words.Add(RunRecord);
                    _words.Add((int)item.Fill);
                    _words.Add(item.Chunk.Y);
                    _words.Add(item.Chunk.X);
                    _words.Add(runLength);
                    recordCount++;
                    index += runLength;
                    continue;
                }

                if (item.Fill != TerritoryChunkFill.Boundary || item.Segments.Count == 0)
                {
                    reason = $"Unsupported coverage for Chunk {item.Chunk}.";
                    return false;
                }

                _words.Add(BoundaryRecord);
                _words.Add(item.Chunk.Y);
                _words.Add(item.Chunk.X);
                _words.Add(item.CenterInside ? 1 : 0);
                _words.Add(item.Segments.Count);
                int previousSequence = -1;
                for (int segmentIndex = 0; segmentIndex < item.Segments.Count; segmentIndex++)
                {
                    TerritoryChunkBoundarySegment segment = item.Segments[segmentIndex];
                    if (segment.Sequence <= previousSequence)
                    {
                        reason = $"Boundary segment sequence for Chunk {item.Chunk} must increase.";
                        return false;
                    }

                    _words.Add(segment.Sequence);
                    _words.Add(segment.Start.X);
                    _words.Add(segment.Start.Y);
                    _words.Add(segment.End.X);
                    _words.Add(segment.End.Y);
                    previousSequence = segment.Sequence;
                }

                recordCount++;
                index++;
            }

            _words[0] = recordCount;
            _packets.Clear();
            int wordOffset = 0;
            uint packetSequence = 0;
            while (wordOffset < _words.Count)
            {
                int wordCount = Math.Min(MaximumWordCount, _words.Count - wordOffset);
                var packetWords = new int[wordCount];
                _words.CopyTo(wordOffset, packetWords, 0, wordCount);
                _packets.Add(new TerritoryChunkTransferPacket(
                    baseRevision,
                    revision,
                    packetSequence,
                    packetWords));
                wordOffset += wordCount;
                packetSequence++;
            }

            packets = new ReadOnlyCollection<TerritoryChunkTransferPacket>(_packets.ToArray());
            reason = null;
            return true;
        }

        private static bool CanJoinRun(
            TerritoryChunkCoverage first,
            TerritoryChunkCoverage candidate,
            int runLength)
        {
            if (candidate.Fill != first.Fill || candidate.Chunk.Y != first.Chunk.Y)
                return false;

            long expectedX = (long)first.Chunk.X + runLength;
            return expectedX <= int.MaxValue && candidate.Chunk.X == expectedX;
        }

        private static int CompareCoverage(
            TerritoryChunkCoverage left,
            TerritoryChunkCoverage right)
        {
            int y = left.Chunk.Y.CompareTo(right.Chunk.Y);
            return y != 0 ? y : left.Chunk.X.CompareTo(right.Chunk.X);
        }
    }
}
