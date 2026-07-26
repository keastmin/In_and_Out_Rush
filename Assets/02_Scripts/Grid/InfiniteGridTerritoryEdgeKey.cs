using System;
using UnityEngine;

namespace KIM.Dev
{
    public readonly struct InfiniteGridTerritoryEdgeKey : IEquatable<InfiniteGridTerritoryEdgeKey>
    {
        public readonly Vector2 A;
        public readonly Vector2 B;

        public InfiniteGridTerritoryEdgeKey(Vector2 first, Vector2 second)
        {
            if (ComesBeforeOrEquals(first, second))
            {
                A = first;
                B = second;
            }
            else
            {
                A = second;
                B = first;
            }
        }

        public bool Equals(InfiniteGridTerritoryEdgeKey other)
        {
            return A.Equals(other.A) && B.Equals(other.B);
        }

        public override bool Equals(object obj)
        {
            return obj is InfiniteGridTerritoryEdgeKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (A.GetHashCode() * 397) ^ B.GetHashCode();
            }
        }

        private static bool ComesBeforeOrEquals(Vector2 first, Vector2 second)
        {
            return first.x < second.x ||
                   first.x.Equals(second.x) && first.y <= second.y;
        }
    }
}
