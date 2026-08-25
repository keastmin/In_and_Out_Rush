using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using NUnit.Framework;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class TerritoryBackgroundExpansionWorkerTests
    {
        [Test]
        public void MoreThan256TrailPointsFinishOffThreadAndRoundTripWithoutSimplification()
        {
            List<Vector2> source = Rectangle();
            List<Vector2> trail = LongOutsideTrail();
            Assert.That(TerritoryBackgroundExpansionWorkItem.TryCreate(
                10,
                1,
                source,
                trail,
                out TerritoryBackgroundExpansionWorkItem item,
                out string reason), Is.True, reason);

            using var worker = new TerritoryBackgroundExpansionWorker();
            Assert.That(worker.TrySchedule(item, out reason), Is.True, reason);
            TerritoryBackgroundExpansionResult result = Wait(worker);

            Assert.That(result.IsSuccess, Is.True, result.FailureReason);
            Assert.That(result.WorkerThreadId, Is.Not.EqualTo(Environment.CurrentManagedThreadId));
            Assert.That(result.Presentation.Vertices.Count, Is.GreaterThan(256));
            Assert.That(result.Presentation.Revision, Is.EqualTo(2));
            Assert.That(result.Packets, Is.Not.Empty);

            var replica = new TerritoryExpansionResultReplica();
            replica.Reset(1);
            Assert.That(replica.TryBegin(
                1,
                2,
                result.Presentation.Vertices.Count,
                result.Presentation.Triangles.Count,
                result.Packets.Count,
                out reason), Is.True, reason);
            for (int i = 0; i < result.Packets.Count; i++)
            {
                TerritoryExpansionResultPacket packet = result.Packets[i];
                Assert.That(packet.Words.Count,
                    Is.InRange(1, TerritoryExpansionResultPacketizer.MaximumWordsPerPacket));
                Assert.That(replica.TryAppend(
                    packet.SourceRevision,
                    packet.Revision,
                    packet.Sequence,
                    packet.CopyWords(),
                    out reason), Is.True, reason);
            }
            Assert.That(replica.TryComplete(
                1,
                2,
                out TerritoryExpansionPresentationData received,
                out reason), Is.True, reason);
            Assert.That(received.Vertices, Is.EqualTo(result.Presentation.Vertices));
            Assert.That(received.Triangles, Is.EqualTo(result.Presentation.Triangles));
        }

        [Test]
        public void RejectedWorkDoesNotPreventTheNextValidExpansion()
        {
            List<Vector2> source = Rectangle();
            using var worker = new TerritoryBackgroundExpansionWorker();
            Assert.That(TerritoryBackgroundExpansionWorkItem.TryCreate(
                20,
                1,
                source,
                new[] { new Vector2(2, 2), new Vector2(3, 3) },
                out TerritoryBackgroundExpansionWorkItem invalid,
                out string reason), Is.True, reason);
            Assert.That(worker.TrySchedule(invalid, out reason), Is.True, reason);
            Assert.That(Wait(worker).IsSuccess, Is.False);

            Assert.That(TerritoryBackgroundExpansionWorkItem.TryCreate(
                21,
                1,
                source,
                new[]
                {
                    new Vector2(9, 2),
                    new Vector2(12, 2),
                    new Vector2(12, 8),
                    new Vector2(9, 8)
                },
                out TerritoryBackgroundExpansionWorkItem valid,
                out reason), Is.True, reason);
            Assert.That(worker.TrySchedule(valid, out reason), Is.True, reason);
            Assert.That(Wait(worker).IsSuccess, Is.True);
        }

        [Test]
        public void ReplicationSendsAtMostTwoDataPacketsPerTick()
        {
            TerritoryExpansionPresentationData presentation = FanPresentation(50);
            var packetizer = new TerritoryExpansionResultPacketizer();
            Assert.That(packetizer.TryCreate(
                presentation,
                out IReadOnlyList<TerritoryExpansionResultPacket> packets,
                out string reason), Is.True, reason);
            Assert.That(packets.Count, Is.GreaterThan(2));

            var replication = new TerritoryExpansionReplication();
            replication.Reset(1);
            Assert.That(replication.TryEnqueue(presentation, packets, out reason), Is.True, reason);

            int totalDataPackets = 0;
            bool beginSeen = false;
            bool completeSeen = false;
            while (!completeSeen)
            {
                int dataPacketsThisTick = 0;
                while (replication.TryTakeOutbound(
                           TerritoryExpansionReplication.MaximumDataPacketsPerTick -
                           dataPacketsThisTick,
                           out TerritoryExpansionReplication.OutboundMessage message))
                {
                    switch (message.Type)
                    {
                        case TerritoryExpansionReplication.OutboundMessageType.Begin:
                            Assert.That(beginSeen, Is.False);
                            beginSeen = true;
                            break;
                        case TerritoryExpansionReplication.OutboundMessageType.Data:
                            dataPacketsThisTick++;
                            totalDataPackets++;
                            break;
                        case TerritoryExpansionReplication.OutboundMessageType.Complete:
                            completeSeen = true;
                            break;
                    }
                }

                Assert.That(dataPacketsThisTick,
                    Is.LessThanOrEqualTo(
                        TerritoryExpansionReplication.MaximumDataPacketsPerTick));
            }

            Assert.That(beginSeen, Is.True);
            Assert.That(totalDataPackets, Is.EqualTo(packets.Count));
        }

        [Test]
        public void ReplicaFastForwardsToLatestFullSnapshotAfterMissingARevision()
        {
            TerritoryExpansionPresentationData latest = FanPresentation(50, 3, 4);
            IReadOnlyList<TerritoryExpansionResultPacket> packets = Packetize(latest);
            var replica = new TerritoryExpansionResultReplica();
            replica.Reset(1);

            Assert.That(replica.TryBegin(3, 4, latest.Vertices.Count, latest.Triangles.Count,
                packets.Count, out string reason), Is.True, reason);
            foreach (TerritoryExpansionResultPacket packet in packets)
                Assert.That(replica.TryAppend(packet.SourceRevision, packet.Revision,
                    packet.Sequence, packet.CopyWords(), out reason), Is.True, reason);

            Assert.That(replica.TryComplete(3, 4,
                out TerritoryExpansionPresentationData received, out reason), Is.True, reason);
            Assert.That(replica.CurrentRevision, Is.EqualTo(4));
            Assert.That(received.Vertices, Is.EqualTo(latest.Vertices));
            Assert.That(received.Triangles, Is.EqualTo(latest.Triangles));
        }

        [Test]
        public void ReplicaIgnoresAppliedReplayAndKeepsCurrentRevision()
        {
            TerritoryExpansionPresentationData presentation = FanPresentation(4);
            IReadOnlyList<TerritoryExpansionResultPacket> packets = Packetize(presentation);
            var replica = new TerritoryExpansionResultReplica();
            replica.Reset(1);
            Apply(replica, presentation, packets);

            Assert.That(replica.TryBegin(1, 2, presentation.Vertices.Count,
                presentation.Triangles.Count, packets.Count, out string reason), Is.True, reason);
            foreach (TerritoryExpansionResultPacket packet in packets)
                Assert.That(replica.TryAppend(packet.SourceRevision, packet.Revision,
                    packet.Sequence, packet.CopyWords(), out reason), Is.True, reason);
            Assert.That(replica.TryComplete(1, 2,
                out TerritoryExpansionPresentationData replay, out reason), Is.True, reason);
            Assert.That(replay, Is.Null);
            Assert.That(replica.CurrentRevision, Is.EqualTo(2));
        }

        [Test]
        public void ReplicaPreservesInboundTransferAfterOutOfOrderOrMalformedPacket()
        {
            TerritoryExpansionPresentationData presentation = FanPresentation(50);
            IReadOnlyList<TerritoryExpansionResultPacket> packets = Packetize(presentation);
            var replica = new TerritoryExpansionResultReplica();
            replica.Reset(1);
            Assert.That(replica.TryBegin(1, 2, presentation.Vertices.Count,
                presentation.Triangles.Count, packets.Count, out string reason), Is.True, reason);

            Assert.That(replica.TryAppend(1, 2, 1, packets[1].CopyWords(), out reason), Is.False);
            Assert.That(replica.IsReceiving, Is.True);
            Assert.That(replica.TryAppend(1, 2, 0, new[] { 1 }, out reason), Is.False);
            Assert.That(replica.IsReceiving, Is.True);

            ApplyRemaining(replica, packets);
            Assert.That(replica.TryComplete(1, 2, out _, out reason), Is.True, reason);
            Assert.That(replica.CurrentRevision, Is.EqualTo(2));
        }

        [Test]
        public void ReplicaAcceptsDuplicateBeginAndPacketWithoutResettingProgress()
        {
            TerritoryExpansionPresentationData presentation = FanPresentation(50);
            IReadOnlyList<TerritoryExpansionResultPacket> packets = Packetize(presentation);
            var replica = new TerritoryExpansionResultReplica();
            replica.Reset(1);
            Assert.That(replica.TryBegin(1, 2, presentation.Vertices.Count,
                presentation.Triangles.Count, packets.Count, out string reason), Is.True, reason);
            Assert.That(replica.TryAppend(1, 2, 0, packets[0].CopyWords(), out reason), Is.True, reason);
            Assert.That(replica.TryBegin(1, 2, presentation.Vertices.Count,
                presentation.Triangles.Count, packets.Count, out reason), Is.True, reason);
            Assert.That(replica.TryAppend(1, 2, 0, packets[0].CopyWords(), out reason), Is.True, reason);

            for (int i = 1; i < packets.Count; i++)
                Assert.That(replica.TryAppend(1, 2, packets[i].Sequence,
                    packets[i].CopyWords(), out reason), Is.True, reason);
            Assert.That(replica.TryComplete(1, 2, out _, out reason), Is.True, reason);
        }

        private static TerritoryBackgroundExpansionResult Wait(
            TerritoryBackgroundExpansionWorker worker)
        {
            var timeout = Stopwatch.StartNew();
            while (timeout.Elapsed < TimeSpan.FromSeconds(15))
            {
                if (worker.TryTakeCompleted(out TerritoryBackgroundExpansionResult result))
                    return result;
                Thread.Sleep(1);
            }

            Assert.Fail("Timed out waiting for background Territory expansion.");
            return null;
        }

        private static List<Vector2> Rectangle()
            => new()
            {
                new Vector2(0, 0),
                new Vector2(0, 10),
                new Vector2(10, 10),
                new Vector2(10, 0)
            };

        private static List<Vector2> LongOutsideTrail()
        {
            var trail = new List<Vector2>(304)
            {
                new Vector2(9, 2),
                new Vector2(11, 2)
            };
            for (int i = 1; i <= 300; i++)
            {
                float y = 2f + 6f * i / 301f;
                float x = 11f + (i % 2 == 0 ? 0.25f : 0f);
                trail.Add(new Vector2(x, y));
            }
            trail.Add(new Vector2(11, 8));
            trail.Add(new Vector2(9, 8));
            return trail;
        }

        private static IReadOnlyList<TerritoryExpansionResultPacket> Packetize(
            TerritoryExpansionPresentationData presentation)
        {
            var packetizer = new TerritoryExpansionResultPacketizer();
            Assert.That(packetizer.TryCreate(presentation,
                out IReadOnlyList<TerritoryExpansionResultPacket> packets,
                out string reason), Is.True, reason);
            return packets;
        }

        private static void Apply(
            TerritoryExpansionResultReplica replica,
            TerritoryExpansionPresentationData presentation,
            IReadOnlyList<TerritoryExpansionResultPacket> packets)
        {
            Assert.That(replica.TryBegin(presentation.SourceRevision, presentation.Revision,
                presentation.Vertices.Count, presentation.Triangles.Count, packets.Count,
                out string reason), Is.True, reason);
            ApplyRemaining(replica, packets);
            Assert.That(replica.TryComplete(presentation.SourceRevision, presentation.Revision,
                out _, out reason), Is.True, reason);
        }

        private static void ApplyRemaining(
            TerritoryExpansionResultReplica replica,
            IReadOnlyList<TerritoryExpansionResultPacket> packets)
        {
            foreach (TerritoryExpansionResultPacket packet in packets)
                Assert.That(replica.TryAppend(packet.SourceRevision, packet.Revision,
                    packet.Sequence, packet.CopyWords(), out string reason), Is.True, reason);
        }

        private static TerritoryExpansionPresentationData FanPresentation(
            int vertexCount,
            ulong sourceRevision = 1,
            ulong revision = 2)
        {
            var vertices = new Vector2[vertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                float angle = Mathf.PI * 2f * i / vertices.Length;
                vertices[i] = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            }

            var triangles = new int[(vertexCount - 2) * 3];
            for (int i = 0; i < vertexCount - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }

            return new TerritoryExpansionPresentationData(
                sourceRevision,
                revision,
                vertices,
                triangles);
        }
    }
}
