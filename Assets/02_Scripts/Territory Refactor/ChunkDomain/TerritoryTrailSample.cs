using System;

namespace ProjectIO.Territory
{
    public readonly struct TerritoryTrailSample : IEquatable<TerritoryTrailSample>
    {
        public TerritoryTrailSample(
            ulong sessionId,
            uint sequence,
            int simulationTick,
            FixedTerritoryPoint point)
        {
            SessionId = sessionId;
            Sequence = sequence;
            SimulationTick = simulationTick;
            Point = point;
        }

        public ulong SessionId { get; }
        public uint Sequence { get; }
        public int SimulationTick { get; }
        public FixedTerritoryPoint Point { get; }

        public bool Equals(TerritoryTrailSample other)
            => SessionId == other.SessionId &&
               Sequence == other.Sequence &&
               SimulationTick == other.SimulationTick &&
               Point == other.Point;

        public override bool Equals(object obj)
            => obj is TerritoryTrailSample other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = ((int)SessionId * 397) ^ (int)(SessionId >> 32);
                hash = (hash * 397) ^ (int)Sequence;
                hash = (hash * 397) ^ SimulationTick;
                return (hash * 397) ^ Point.GetHashCode();
            }
        }
    }
}
