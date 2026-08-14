using System.Collections.Generic;
using KIM.Dev;
using UnityEngine;

namespace ProjectIO.Monsters
{
    public sealed class WorldObstacleBoundsIndex
    {
        private readonly Dictionary<MonsterChunkCoordinate, List<Entry>> _entriesByChunk = new();
        private readonly HashSet<WorldObstacle> _visitedObstacles = new();
        private readonly float _chunkSize;

        public WorldObstacleBoundsIndex(float chunkSize)
        {
            _chunkSize = Mathf.Max(1f, chunkSize);
        }

        public void Rebuild(IReadOnlyList<WorldObstacle> obstacles)
        {
            _entriesByChunk.Clear();
            if (obstacles == null)
                return;

            for (int i = 0; i < obstacles.Count; i++)
            {
                WorldObstacle obstacle = obstacles[i];
                if (obstacle == null)
                    continue;

                Bounds bounds = obstacle.Bounds;
                var entry = new Entry(obstacle, bounds);
                MonsterChunkCoordinate minimum = GetChunk(bounds.min);
                MonsterChunkCoordinate maximum = GetChunk(bounds.max);

                for (int y = minimum.Y; y <= maximum.Y; y++)
                {
                    for (int x = minimum.X; x <= maximum.X; x++)
                        AddEntry(new MonsterChunkCoordinate(x, y), entry);
                }
            }
        }

        public bool IsPathBlocked(Vector3 startPosition, Vector3 endPosition, float clearance)
        {
            float safeClearance = Mathf.Max(0f, clearance);
            Vector3 minimumPosition = Vector3.Min(startPosition, endPosition) -
                                      new Vector3(safeClearance, 0f, safeClearance);
            Vector3 maximumPosition = Vector3.Max(startPosition, endPosition) +
                                      new Vector3(safeClearance, 0f, safeClearance);
            MonsterChunkCoordinate minimum = GetChunk(minimumPosition);
            MonsterChunkCoordinate maximum = GetChunk(maximumPosition);

            _visitedObstacles.Clear();
            for (int y = minimum.Y; y <= maximum.Y; y++)
            {
                for (int x = minimum.X; x <= maximum.X; x++)
                {
                    var coordinate = new MonsterChunkCoordinate(x, y);
                    if (!_entriesByChunk.TryGetValue(coordinate, out List<Entry> entries))
                        continue;

                    for (int i = 0; i < entries.Count; i++)
                    {
                        Entry entry = entries[i];
                        if (entry.Obstacle == null || !_visitedObstacles.Add(entry.Obstacle))
                            continue;

                        if (DoesSegmentOverlapBounds(
                                startPosition,
                                endPosition,
                                entry.Bounds,
                                safeClearance))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private void AddEntry(MonsterChunkCoordinate coordinate, Entry entry)
        {
            if (!_entriesByChunk.TryGetValue(coordinate, out List<Entry> entries))
            {
                entries = new List<Entry>();
                _entriesByChunk.Add(coordinate, entries);
            }

            entries.Add(entry);
        }

        private MonsterChunkCoordinate GetChunk(Vector3 position)
        {
            return new MonsterChunkCoordinate(
                Mathf.FloorToInt(position.x / _chunkSize),
                Mathf.FloorToInt(position.z / _chunkSize));
        }

        private static bool DoesSegmentOverlapBounds(
            Vector3 startPosition,
            Vector3 endPosition,
            Bounds bounds,
            float clearance)
        {
            Vector2 boundsMin = new(bounds.min.x - clearance, bounds.min.z - clearance);
            Vector2 boundsMax = new(bounds.max.x + clearance, bounds.max.z + clearance);
            Vector2 start = new(startPosition.x, startPosition.z);
            Vector2 direction = new(endPosition.x - startPosition.x, endPosition.z - startPosition.z);
            float minimumT = 0f;
            float maximumT = 1f;

            return ClipSegmentAxis(start.x, direction.x, boundsMin.x, boundsMax.x, ref minimumT, ref maximumT) &&
                   ClipSegmentAxis(start.y, direction.y, boundsMin.y, boundsMax.y, ref minimumT, ref maximumT);
        }

        private static bool ClipSegmentAxis(
            float start,
            float direction,
            float min,
            float max,
            ref float minimumT,
            ref float maximumT)
        {
            if (Mathf.Abs(direction) <= Mathf.Epsilon)
                return start >= min && start <= max;

            float inverseDirection = 1f / direction;
            float enter = (min - start) * inverseDirection;
            float exit = (max - start) * inverseDirection;
            if (enter > exit)
                (enter, exit) = (exit, enter);

            minimumT = Mathf.Max(minimumT, enter);
            maximumT = Mathf.Min(maximumT, exit);
            return minimumT <= maximumT;
        }

        private readonly struct Entry
        {
            public Entry(WorldObstacle obstacle, Bounds bounds)
            {
                Obstacle = obstacle;
                Bounds = bounds;
            }

            public WorldObstacle Obstacle { get; }
            public Bounds Bounds { get; }
        }
    }
}
