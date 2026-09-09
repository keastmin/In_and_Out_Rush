namespace ProjectIO.Monsters
{
    public readonly struct WorldMonsterSpawnCandidate
    {
        public WorldMonsterSpawnCandidate(
            int recordId,
            bool isDestroyed,
            bool hasActiveMonster,
            bool isInsideTerritory,
            bool isWithinActiveChunkRange)
        {
            RecordId = recordId;
            IsDestroyed = isDestroyed;
            HasActiveMonster = hasActiveMonster;
            IsInsideTerritory = isInsideTerritory;
            IsWithinActiveChunkRange = isWithinActiveChunkRange;
        }

        public int RecordId { get; }
        public bool IsDestroyed { get; }
        public bool HasActiveMonster { get; }
        public bool IsInsideTerritory { get; }
        public bool IsWithinActiveChunkRange { get; }
    }
}
