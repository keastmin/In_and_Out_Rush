using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.Territory
{
    public sealed class TerritoryExpansionReplication
    {
        private readonly List<Vector2> _receivedVertices = new();

        public void BeginVertices()
            => _receivedVertices.Clear();

        public void AppendVertices(Vector2[] vertices)
        {
            if (vertices != null && vertices.Length > 0)
                _receivedVertices.AddRange(vertices);
        }

        public bool TryConsumeVertices(out List<Vector2> vertices)
        {
            if (_receivedVertices.Count == 0)
            {
                vertices = null;
                return false;
            }

            vertices = new List<Vector2>(_receivedVertices);
            _receivedVertices.Clear();
            return true;
        }
    }
}
