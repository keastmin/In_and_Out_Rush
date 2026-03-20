using UnityEngine;

namespace Grid
{
    public readonly struct GridInitializationResult
    {
        public HexaCell[,] Grid { get; }
        public Vector3 GridOriginPosition { get; }
        public float HexHeight { get; }
        public float GridTilingHeightOffset { get; }
        public float GridTilingWidthOffset { get; }

        public GridInitializationResult(
            HexaCell[,] grid,
            Vector3 gridOriginPosition,
            float hexHeight,
            float gridTilingHeightOffset,
            float gridTilingWidthOffset)
        {
            Grid = grid;
            GridOriginPosition = gridOriginPosition;
            HexHeight = hexHeight;
            GridTilingHeightOffset = gridTilingHeightOffset;
            GridTilingWidthOffset = gridTilingWidthOffset;
        }
    }
}
