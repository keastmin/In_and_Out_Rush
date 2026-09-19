using System;

namespace ProjectIO.Monsters
{
    /// <summary>One emission per simulation tick; waits begin at the actual last emission.</summary>
    public sealed class RafflesiaAttackPattern
    {
        private int _pattern;
        private int _volley;
        private double _nextTime;

        public void Reset()
        {
            _pattern = 0;
            _volley = 0;
            _nextTime = double.NegativeInfinity;
        }

        public RafflesiaAttackPattern() => Reset();

        // Angles are clockwise from world +Z as viewed from above (+Y).
        public int TryEmit(double time, float[] angles)
        {
            if (angles == null || angles.Length < 8)
                throw new ArgumentException("Eight output slots are required.", nameof(angles));
            if (time < _nextTime)
                return 0;

            int count = _pattern == 0 ? 8 : _pattern == 3 ? 2 : 4;
            for (int i = 0; i < count; i++)
                angles[i] = _pattern == 3
                    ? (270f + i * 180f + _volley * 22.5f) % 360f
                    : i * (360f / count) + (_pattern == 2 ? 45f : 0f);

            int volleys = _pattern == 0 ? 1 : _pattern == 3 ? 8 : 4;
            _volley++;
            if (_volley == volleys)
            {
                _pattern = (_pattern + 1) % 4;
                _volley = 0;
                _nextTime = time + 1.5;
            }
            else
                _nextTime = time + (_pattern == 3 ? 0.15 : 0.25);
            return count;
        }
    }
}
