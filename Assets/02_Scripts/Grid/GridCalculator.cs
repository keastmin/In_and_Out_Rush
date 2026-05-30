using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class GridCalculator
    {
        private const float SQRT3 = 1.7320508075688772f;

        private readonly int _chunkColSize;
        private readonly int _chunkRowSize;

        public GridCalculator(int chunkColSize = 16, int chunkRowSize = 16)
        {
            _chunkColSize = Mathf.Max(1, chunkColSize);
            _chunkRowSize = Mathf.Max(1, chunkRowSize);
        }

        public Vector3 GetCellCenterPositionFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
        {
            if (cellSize <= 0f)
                return origin;

            Vector2Int nearestCellIndex = GetNearestCellIndexFromWorldPosition(origin, worldPos, cellSize);
            return GetCellCenterPositionFromCellIndex(origin, nearestCellIndex.x, nearestCellIndex.y, cellSize);
        }

        public GridChunkKey GetChunkKeyFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
        {
            Vector2Int nearestCellIndex = GetNearestCellIndexFromWorldPosition(origin, worldPos, cellSize);

            int chunkCol = FloorDiv(nearestCellIndex.x, _chunkColSize);
            int chunkRow = FloorDiv(nearestCellIndex.y, _chunkRowSize);

            return new GridChunkKey(chunkCol, chunkRow);
        }

        public Vector2Int GetNearestCellIndexFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
        {
            if (cellSize <= 0f)
                return Vector2Int.zero;

            Vector3 local = worldPos - origin;

            float qf = ((2f / 3f) * local.x) / cellSize;
            float rf = ((-1f / 3f) * local.x + (SQRT3 / 3f) * local.z) / cellSize;

            Vector2Int roundedAxial = RoundAxial(qf, rf);
            return AxialToOddQOffset(roundedAxial.x, roundedAxial.y);
        }

        public Vector3 GetCellCenterPositionFromCellIndex(Vector3 origin, int col, int row, float cellSize)
        {
            float xOffset = cellSize * 1.5f;
            float zOffset = cellSize * SQRT3;
            float halfZ = cellSize * (SQRT3 * 0.5f);

            float x = origin.x + (xOffset * row);
            float z = origin.z + (zOffset * col) + (((row & 1) == 1) ? halfZ : 0f);

            return new Vector3(x, origin.y, z);
        }

        public List<Vector2Int> GetInRangeIndices(Vector2Int index, int range)
        {
            var result = new List<Vector2Int>();
            int radius = Mathf.Max(0, range);
            int q0 = index.y;
            int r0 = index.x - (q0 >> 1);

            int estimatedCount = radius == 0 ? 1 : 1 + (3 * radius * (radius + 1));
            result.Capacity = estimatedCount;

            for (int dq = -radius; dq <= radius; dq++)
            {
                int drMin = Mathf.Max(-radius, -dq - radius);
                int drMax = Mathf.Min(radius, -dq + radius);

                for (int dr = drMin; dr <= drMax; dr++)
                {
                    int q = q0 + dq;
                    int r = r0 + dr;
                    result.Add(AxialToOddQOffset(q, r));
                }
            }

            return result;
        }

        public Vector2Int GetAxialFromOffsetIndex(Vector2Int index)
        {
            int q = index.y;
            int r = index.x - (q >> 1);
            return new Vector2Int(q, r);
        }

        private Vector2Int AxialToOddQOffset(int q, int r)
        {
            int row = q;
            int col = r + ((q - (q & 1)) >> 1);
            return new Vector2Int(col, row);
        }

        private Vector2Int RoundAxial(float qf, float rf)
        {
            float xf = qf;
            float zf = rf;
            float yf = -xf - zf;

            int rx = Mathf.RoundToInt(xf);
            int ry = Mathf.RoundToInt(yf);
            int rz = Mathf.RoundToInt(zf);

            float dx = Mathf.Abs(rx - xf);
            float dy = Mathf.Abs(ry - yf);
            float dz = Mathf.Abs(rz - zf);

            if (dx > dy && dx > dz)
            {
                rx = -ry - rz;
            }
            else if (dy > dz)
            {
                ry = -rx - rz;
            }
            else
            {
                rz = -rx - ry;
            }

            return new Vector2Int(rx, rz);
        }

        private int FloorDiv(int value, int divisor)
        {
            int quotient = value / divisor;
            int remainder = value % divisor;

            if (remainder != 0 && ((value < 0) ^ (divisor < 0)))
            {
                quotient--;
            }

            return quotient;
        }
    }
}