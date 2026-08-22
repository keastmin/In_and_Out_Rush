using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ProjectIO.Territory
{
    public sealed class TerritoryCompactRowCoverage : IEquatable<TerritoryCompactRowCoverage>
    {
        private readonly ReadOnlyCollection<TerritoryChunkFillRun> _runs;

        public TerritoryCompactRowCoverage(int y, IReadOnlyList<TerritoryChunkFillRun> runs)
        {
            if (runs == null)
                throw new ArgumentNullException(nameof(runs));
            if (runs.Count == 0)
                throw new ArgumentException("A compact row requires at least one Full run.", nameof(runs));

            var copy = new TerritoryChunkFillRun[runs.Count];
            int previousMaximum = 0;
            for (int i = 0; i < runs.Count; i++)
            {
                TerritoryChunkFillRun run = runs[i];
                if (run.Y != y)
                    throw new ArgumentException("Every Full run must belong to the compact row.", nameof(runs));
                if (i > 0 && (previousMaximum == int.MaxValue || run.MinimumX <= previousMaximum + 1))
                    throw new ArgumentException("Compact row runs must be sorted, disjoint and maximally merged.", nameof(runs));

                copy[i] = run;
                previousMaximum = run.MaximumX;
            }

            Y = y;
            _runs = Array.AsReadOnly(copy);
        }

        public int Y { get; }
        public IReadOnlyList<TerritoryChunkFillRun> Runs => _runs;

        public bool Contains(int x)
        {
            int low = 0;
            int high = _runs.Count - 1;
            while (low <= high)
            {
                int middle = low + ((high - low) >> 1);
                TerritoryChunkFillRun run = _runs[middle];
                if (x < run.MinimumX)
                    high = middle - 1;
                else if (x > run.MaximumX)
                    low = middle + 1;
                else
                    return true;
            }
            return false;
        }

        public TerritoryCompactRowCoverage Union(TerritoryChunkFillRun addition)
        {
            if (addition.Y != Y)
                throw new ArgumentException("A Full run can only be added to its own row.", nameof(addition));

            var merged = new List<TerritoryChunkFillRun>(_runs.Count + 1);
            int minimum = addition.MinimumX;
            int maximum = addition.MaximumX;
            bool inserted = false;
            for (int i = 0; i < _runs.Count; i++)
            {
                TerritoryChunkFillRun current = _runs[i];
                bool currentBefore = current.MaximumX != int.MaxValue && current.MaximumX + 1 < minimum;
                bool additionBefore = maximum != int.MaxValue && maximum + 1 < current.MinimumX;
                if (currentBefore)
                {
                    merged.Add(current);
                    continue;
                }
                if (additionBefore)
                {
                    if (!inserted)
                    {
                        merged.Add(new TerritoryChunkFillRun(Y, minimum, maximum));
                        inserted = true;
                    }
                    merged.Add(current);
                    continue;
                }

                minimum = Math.Min(minimum, current.MinimumX);
                maximum = Math.Max(maximum, current.MaximumX);
            }

            if (!inserted)
                merged.Add(new TerritoryChunkFillRun(Y, minimum, maximum));

            if (merged.Count == _runs.Count)
            {
                bool equal = true;
                for (int i = 0; i < merged.Count; i++)
                    equal &= merged[i] == _runs[i];
                if (equal)
                    return this;
            }

            return new TerritoryCompactRowCoverage(Y, merged);
        }

        public bool Equals(TerritoryCompactRowCoverage other)
        {
            if (ReferenceEquals(null, other) || Y != other.Y || _runs.Count != other._runs.Count)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            for (int i = 0; i < _runs.Count; i++)
            {
                if (_runs[i] != other._runs[i])
                    return false;
            }
            return true;
        }

        public override bool Equals(object obj)
            => Equals(obj as TerritoryCompactRowCoverage);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Y;
                for (int i = 0; i < _runs.Count; i++)
                    hash = (hash * 397) ^ _runs[i].GetHashCode();
                return hash;
            }
        }
    }
}
