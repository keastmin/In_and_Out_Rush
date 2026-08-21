using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryTrailPacket
    {
        public const int MaximumSampleCount = 24;
        private const int IntegersPerSample = 3;

        private readonly ReadOnlyCollection<TerritoryTrailSample> _samples;

        public TerritoryTrailPacket(
            ulong sessionId,
            uint sequence,
            uint firstSampleSequence,
            IReadOnlyList<TerritoryTrailSample> samples)
        {
            if (sessionId == 0)
                throw new ArgumentOutOfRangeException(nameof(sessionId));
            if (samples == null)
                throw new ArgumentNullException(nameof(samples));
            if (samples.Count == 0 || samples.Count > MaximumSampleCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(samples),
                    $"A Trail packet requires between 1 and {MaximumSampleCount} samples.");
            }

            var copy = new TerritoryTrailSample[samples.Count];
            int previousTick = int.MinValue;
            for (int index = 0; index < samples.Count; index++)
            {
                TerritoryTrailSample sample = samples[index];
                ulong expectedSequence = (ulong)firstSampleSequence + (uint)index;
                if (sample.SessionId != sessionId)
                    throw new ArgumentException("Every packet sample must use the packet SessionId.", nameof(samples));
                if (expectedSequence > uint.MaxValue || sample.Sequence != (uint)expectedSequence)
                    throw new ArgumentException("Packet sample sequences must be contiguous.", nameof(samples));
                if (sample.SimulationTick < previousTick)
                    throw new ArgumentException("Packet simulation ticks must be nondecreasing.", nameof(samples));

                copy[index] = sample;
                previousTick = sample.SimulationTick;
            }

            SessionId = sessionId;
            Sequence = sequence;
            FirstSampleSequence = firstSampleSequence;
            _samples = Array.AsReadOnly(copy);
        }

        public ulong SessionId { get; }
        public uint Sequence { get; }
        public uint FirstSampleSequence { get; }
        public IReadOnlyList<TerritoryTrailSample> Samples => _samples;

        public static int[] Encode(TerritoryTrailPacket packet)
        {
            if (packet == null)
                throw new ArgumentNullException(nameof(packet));

            var payload = new int[packet.Samples.Count * IntegersPerSample];
            for (int index = 0; index < packet.Samples.Count; index++)
            {
                TerritoryTrailSample sample = packet.Samples[index];
                int payloadIndex = index * IntegersPerSample;
                payload[payloadIndex] = sample.SimulationTick;
                payload[payloadIndex + 1] = sample.Point.X;
                payload[payloadIndex + 2] = sample.Point.Y;
            }

            return payload;
        }

        public static bool TryDecode(
            ulong sessionId,
            uint packetSequence,
            uint firstSampleSequence,
            int[] payload,
            out TerritoryTrailPacket packet,
            out string reason)
        {
            packet = null;
            if (payload == null ||
                payload.Length == 0 ||
                payload.Length % IntegersPerSample != 0)
            {
                reason = "Trail packet payload must contain tick/x/y integer triples.";
                return false;
            }

            int sampleCount = payload.Length / IntegersPerSample;
            if (sampleCount > MaximumSampleCount)
            {
                reason = $"Trail packet exceeds {MaximumSampleCount} samples.";
                return false;
            }

            try
            {
                var samples = new TerritoryTrailSample[sampleCount];
                for (int index = 0; index < sampleCount; index++)
                {
                    ulong sequence = (ulong)firstSampleSequence + (uint)index;
                    if (sequence > uint.MaxValue)
                    {
                        reason = "Trail sample sequence exceeds UInt32 range.";
                        return false;
                    }

                    int payloadIndex = index * IntegersPerSample;
                    samples[index] = new TerritoryTrailSample(
                        sessionId,
                        (uint)sequence,
                        payload[payloadIndex],
                        new FixedTerritoryPoint(
                            payload[payloadIndex + 1],
                            payload[payloadIndex + 2]));
                }

                packet = new TerritoryTrailPacket(
                    sessionId,
                    packetSequence,
                    firstSampleSequence,
                    samples);
                reason = null;
                return true;
            }
            catch (Exception exception)
            {
                reason = exception.Message;
                return false;
            }
        }
    }
}
