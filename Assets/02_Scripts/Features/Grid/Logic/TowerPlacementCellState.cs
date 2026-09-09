namespace ProjectIO.GridPlacement
{
    public readonly struct TowerPlacementCellState
    {
        public bool IsInBuildArea { get; }
        public bool IsBlockedByTrack { get; }
        public bool IsOccupied { get; }

        public TowerPlacementCellState(
            bool isInBuildArea,
            bool isBlockedByTrack,
            bool isOccupied)
        {
            IsInBuildArea = isInBuildArea;
            IsBlockedByTrack = isBlockedByTrack;
            IsOccupied = isOccupied;
        }
    }
}
