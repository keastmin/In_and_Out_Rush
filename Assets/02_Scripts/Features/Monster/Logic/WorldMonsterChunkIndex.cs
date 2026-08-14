using System.Collections.Generic;

namespace ProjectIO.Monsters
{
    public sealed class WorldMonsterChunkIndex<T> where T : class
    {
        private readonly Dictionary<MonsterChunkCoordinate, List<T>> _recordsByChunk = new();

        public void Add(MonsterChunkCoordinate chunk, T record)
        {
            if (record == null)
                return;

            if (!_recordsByChunk.TryGetValue(chunk, out List<T> records))
            {
                records = new List<T>();
                _recordsByChunk.Add(chunk, records);
            }

            records.Add(record);
        }

        public void CollectRange(
            MonsterChunkCoordinate center,
            int radius,
            List<T> results)
        {
            if (results == null)
                return;

            results.Clear();
            int safeRadius = radius < 0 ? 0 : radius;

            for (int y = center.Y - safeRadius; y <= center.Y + safeRadius; y++)
            {
                for (int x = center.X - safeRadius; x <= center.X + safeRadius; x++)
                {
                    var coordinate = new MonsterChunkCoordinate(x, y);
                    if (_recordsByChunk.TryGetValue(coordinate, out List<T> records))
                        results.AddRange(records);
                }
            }
        }

        public void Clear()
            => _recordsByChunk.Clear();
    }
}
