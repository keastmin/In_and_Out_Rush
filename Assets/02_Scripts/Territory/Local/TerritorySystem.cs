using System.Collections.Generic;
using UnityEngine;

namespace Dev.Local
{
    public class TerritorySystem : System
    {
        [Header("Territory")]
        [SerializeField] TerritoryVisible territoryVisiblePrefab;
        [SerializeField] int initialTerritoryVertexCount;
        [SerializeField] float initialTerritoryRadius;

        public bool CreateInitialCircleTerritory(out Territory territory, out TerritoryVisible territoryVisible)
        {
            var vertices = CreateCircleTerritoryVertices();
            territory = CreateTerritory(vertices);
            territoryVisible = CreateTerritoryVisible(vertices);
            return true;
        }

        private Territory CreateTerritory(List<Vector2> vertices)
            => new() { Vertices = vertices };

        private TerritoryVisible CreateTerritoryVisible(List<Vector2> vertices)
        {
            var territoryVisible = Instantiate(territoryVisiblePrefab);
            territoryVisible.name = $"Territory";
            territoryVisible.SetVertices(vertices);
            return territoryVisible;
        }

        List<Vector2> CreateCircleTerritoryVertices()
        {
            var vertices = new List<Vector2>();

            var partOfAngle = 2f * Mathf.PI / initialTerritoryVertexCount;

            for (int i = 0; i < initialTerritoryVertexCount; i++)
            {
                var angle = (initialTerritoryVertexCount - 1 - i) * partOfAngle;
                var x = initialTerritoryRadius * Mathf.Cos(angle);
                var y = initialTerritoryRadius * Mathf.Sin(angle);
                var vertex = new Vector2(x, y);
                vertices.Add(vertex);
            }

            return vertices;
        }
    }
}