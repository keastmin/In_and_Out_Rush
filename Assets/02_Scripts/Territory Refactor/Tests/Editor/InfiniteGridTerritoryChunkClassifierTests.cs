using System.Collections.Generic;
using KIM.Dev;
using NUnit.Framework;
using UnityEngine;

namespace ProjectIO.Territory.Tests
{
    public sealed class InfiniteGridTerritoryChunkClassifierTests
    {
        [TestCase(0, 0)]
        [TestCase(-1, -1)]
        public void ChunkCellsMatchTerritorySpatialQuery(int chunkX, int chunkY)
        {
            var territory = new global::Territory();
            territory.ReplaceVertices(new List<Vector2>
            {
                new(-8f, -8f),
                new(8f, -8f),
                new(8f, 8f),
                new(2f, 8f),
                new(2f, 1f),
                new(-2f, 1f),
                new(-2f, 8f),
                new(-8f, 8f)
            });

            var calculator = new GridCalculator(4, 4);
            var state = new InfiniteGridChunkState(new GridChunkKey(chunkX, chunkY), 16);
            var classifier = new InfiniteGridTerritoryChunkClassifier();
            Vector3 origin = new(-0.5f, 0f, 0.25f);

            classifier.Rebuild(state, calculator, origin, 1f, territory);

            Vector2Int chunkMin = calculator.GetChunkMinCellIndex(state.Key);
            for (int localRow = 0; localRow < calculator.ChunkRowCount; localRow++)
            {
                int row = chunkMin.y + localRow;
                for (int localColumn = 0; localColumn < calculator.ChunkColumnCount; localColumn++)
                {
                    int column = chunkMin.x + localColumn;
                    Vector3 center = calculator.GetCellCenterPositionFromCellIndex(
                        origin,
                        column,
                        row,
                        1f);
                    bool expected = territory.IsPointInPolygon(new Vector2(center.x, center.z));
                    int localIndex = localRow * calculator.ChunkColumnCount + localColumn;

                    Assert.That(
                        state.TerritoryCells[localIndex] == byte.MaxValue,
                        Is.EqualTo(expected),
                        $"Cell ({column}, {row}) at ({center.x}, {center.z})");
                }
            }
        }

        [Test]
        public void MissingTerritoryClearsPreviouslyClassifiedCells()
        {
            var calculator = new GridCalculator(2, 2);
            var state = new InfiniteGridChunkState(new GridChunkKey(0, 0), 4);
            for (int i = 0; i < state.TerritoryCells.Length; i++)
                state.TerritoryCells[i] = byte.MaxValue;

            var classifier = new InfiniteGridTerritoryChunkClassifier();
            classifier.Rebuild(state, calculator, Vector3.zero, 1f, null);

            Assert.That(state.TerritoryCells, Is.All.EqualTo((byte)0));
        }
    }
}
