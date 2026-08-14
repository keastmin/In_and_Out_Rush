using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryBoundsIndex
    {
        private const float BoundaryPadding = 0.0001f;
        private Vector2 _minimum;
        private Vector2 _maximum;

        public bool IsValid { get; private set; }

        public void Rebuild(IReadOnlyList<Vector2> vertices)
        {
            IsValid = vertices != null && vertices.Count >= 3;
            if (!IsValid)
                return;

            _minimum = vertices[0];
            _maximum = vertices[0];

            for (int i = 1; i < vertices.Count; i++)
            {
                Vector2 vertex = vertices[i];
                _minimum = Vector2.Min(_minimum, vertex);
                _maximum = Vector2.Max(_maximum, vertex);
            }
        }

        public bool Contains(Vector2 point)
        {
            return IsValid &&
                   point.x >= _minimum.x - BoundaryPadding &&
                   point.x <= _maximum.x + BoundaryPadding &&
                   point.y >= _minimum.y - BoundaryPadding &&
                   point.y <= _maximum.y + BoundaryPadding;
        }
    }
}
