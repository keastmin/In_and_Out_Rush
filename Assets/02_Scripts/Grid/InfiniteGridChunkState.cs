namespace KIM.Dev
{
    public sealed class InfiniteGridChunkState
    {
        public GridChunkKey Key { get; }
        public byte[] BlockedCells { get; }
        public byte[] TerritoryCells { get; }
        public int BaseStateRevision { get; set; }
        public int TerritoryRevision { get; set; }
        public long LastUseSequence { get; set; }

        public InfiniteGridChunkState(GridChunkKey key, int cellCount)
        {
            Key = key;
            BlockedCells = new byte[cellCount];
            TerritoryCells = new byte[cellCount];
            BaseStateRevision = -1;
            TerritoryRevision = -1;
        }
    }
}
