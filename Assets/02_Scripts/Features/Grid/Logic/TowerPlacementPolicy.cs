namespace ProjectIO.GridPlacement
{
    public static class TowerPlacementPolicy
    {
        public static bool CanPlace(TowerPlacementCellState cellState)
        {
            return cellState.IsInBuildArea &&
                   !cellState.IsBlockedByTrack &&
                   !cellState.IsOccupied;
        }
    }
}
