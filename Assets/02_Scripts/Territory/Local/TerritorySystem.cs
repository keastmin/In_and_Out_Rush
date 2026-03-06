using System.Collections.Generic;
using UnityEngine;

namespace Dev.Local
{
    public class TerritorySystem : System
    {
        [Header("Territory")]
        [SerializeField] private TerritoryVisible _territoryVisiblePrefab;
        [SerializeField] private int _initialTerritoryVertexCount;
        [SerializeField] private float _initialTerritoryRadius;

        public bool CreateInitialCircleTerritory(out Territory territory, out TerritoryVisible territoryVisible)
        {
            var vertices = CreateCircleTerritoryVertices();
            territory = CreateTerritory(vertices);
            territoryVisible = CreateTerritoryVisible(vertices);
            return true;
        }

        private List<Vector2> CreateCircleTerritoryVertices()
        {
            var vertices = new List<Vector2>();

            var partOfAngle = 2f * Mathf.PI / _initialTerritoryVertexCount;

            for (int i = 0; i < _initialTerritoryVertexCount; i++)
            {
                var angle = (_initialTerritoryVertexCount - 1 - i) * partOfAngle;
                var x = _initialTerritoryRadius * Mathf.Cos(angle);
                var y = _initialTerritoryRadius * Mathf.Sin(angle);
                var vertex = new Vector2(x, y);
                vertices.Add(vertex);
            }

            return vertices;
        }

        private Territory CreateTerritory(List<Vector2> vertices)
            => new() { Vertices = vertices };

        private TerritoryVisible CreateTerritoryVisible(List<Vector2> vertices)
        {
            var territoryVisible = Instantiate(_territoryVisiblePrefab);
            territoryVisible.name = $"Territory";
            territoryVisible.SetVertices(vertices);
            return territoryVisible;
        }
    }
}