namespace ProjectIO.Monsters
{
    public static class WorldMonsterSpawnCandidatePolicy
    {
        public static bool CanSpawn(WorldMonsterSpawnCandidate candidate)
        {
            return !candidate.IsDestroyed &&
                   !candidate.HasActiveMonster &&
                   !candidate.IsInsideTerritory &&
                   candidate.IsWithinActiveChunkRange;
        }
    }
}
