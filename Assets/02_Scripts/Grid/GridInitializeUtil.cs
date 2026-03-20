using UnityEngine;

namespace Grid
{
    public static class GridInitializeUtil
    {
        private const float SQRT3 = 1.7320508075688772f;

        /// <summary>
        /// Flat-top hex grid 초기화 결과를 생성한다.
        /// </summary>
        public static bool TryInitializeGrid(
            int gridCol,
            int gridRow,
            float hexSize,
            Vector3 gridOffset,
            Bounds planeBounds,
            out GridInitializationResult result)
        {
            result = default;

            if (gridCol <= 0 || gridRow <= 0) return false;
            if (hexSize <= 0f) return false;

            float hexHeight = hexSize * (SQRT3 / 2f);
            float gridTilingHeightOffset = SQRT3 * hexSize;
            float gridTilingWidthOffset = 1.5f * hexSize;

            Vector3 gridOriginPosition =
                planeBounds.min + new Vector3(hexSize, 0f, hexHeight) + gridOffset;

            HexaCell[,] grid = new HexaCell[gridCol, gridRow];

            for (int col = 0; col < gridCol; col++)
            {
                float height = col * gridTilingHeightOffset;

                for (int row = 0; row < gridRow; row++)
                {
                    float rowHeightOffset = ((row & 1) == 0) ? 0f : hexHeight;
                    float width = row * gridTilingWidthOffset;

                    Vector3 center =
                        gridOriginPosition + new Vector3(width, 0f, height + rowHeightOffset);

                    grid[col, row] = new HexaCell(center, CreateHexVertices(center, hexSize));
                }
            }

            result = new GridInitializationResult(
                grid,
                gridOriginPosition,
                hexHeight,
                gridTilingHeightOffset,
                gridTilingWidthOffset);

            return true;
        }

        private static Vector3[] CreateHexVertices(Vector3 center, float hexSize)
        {
            Vector3[] vertices = new Vector3[6];

            for (int i = 0; i < 6; i++)
            {
                float rad = (60f * i) * Mathf.Deg2Rad;
                vertices[i] = center + new Vector3(
                    Mathf.Cos(rad) * hexSize,
                    0f,
                    Mathf.Sin(rad) * hexSize);
            }

            return vertices;
        }
    }
}
