using System.Collections.Generic;
using UnityEngine;

public class GridCalculator
{
    private const float SQRT3 = 1.7320508075688772f;

    // offset index 기준 청크 크기
    private readonly int _chunkColSize;
    private readonly int _chunkRowSize;

    public GridCalculator(int chunkColSize = 16, int chunkRowSize = 16)
    {
        _chunkColSize = Mathf.Max(1, chunkColSize);
        _chunkRowSize = Mathf.Max(1, chunkRowSize);
    }

    /// <summary>
    /// 입력한 월드 위치에서 가장 가까운 셀의 중심점을 구합니다
    /// </summary>
    public Vector3 GetCellCenterPositionFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
    {
        if (cellSize <= 0f)
            return origin;

        Vector2Int nearestCellIndex = GetNearestCellIndexFromWorldPosition(origin, worldPos, cellSize);
        return GetCellCenterPositionFromCellIndex(origin, nearestCellIndex.x, nearestCellIndex.y, cellSize);
    }

    /// <summary>
    /// 입력한 월드 위치의 청크 키를 구합니다
    /// </summary>
    public GridChunkKey GetChunkKeyFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
    {
        Vector2Int nearestCellIndex = GetNearestCellIndexFromWorldPosition(origin, worldPos, cellSize);

        int chunkCol = FloorDiv(nearestCellIndex.x, _chunkColSize);
        int chunkRow = FloorDiv(nearestCellIndex.y, _chunkRowSize);

        return new GridChunkKey(chunkCol, chunkRow);
    }

    /// <summary>
    /// 월드 위치에서 가장 가까운 셀의 offset 인덱스(col, row)를 구합니다.
    /// x = col(Z축), y = row(X축)
    /// </summary>
    public Vector2Int GetNearestCellIndexFromWorldPosition(Vector3 origin, Vector3 worldPos, float cellSize)
    {
        if (cellSize <= 0f)
            return Vector2Int.zero;

        Vector3 local = worldPos - origin;

        // Flat-top axial 연속좌표
        float qf = ((2f / 3f) * local.x) / cellSize;
        float rf = ((-1f / 3f) * local.x + (SQRT3 / 3f) * local.z) / cellSize;

        Vector2Int roundedAxial = RoundAxial(qf, rf);
        return AxialToOddQOffset(roundedAxial.x, roundedAxial.y);
    }

    /// <summary>
    /// offset 인덱스(col, row)의 중심점을 구합니다.
    /// </summary>
    public Vector3 GetCellCenterPositionFromCellIndex(Vector3 origin, int col, int row, float cellSize)
    {
        float xOffset = cellSize * 1.5f;
        float zOffset = cellSize * SQRT3;
        float halfZ = cellSize * (SQRT3 * 0.5f);

        float x = origin.x + (xOffset * row);
        float z = origin.z + (zOffset * col) + (((row & 1) == 1) ? halfZ : 0f);

        return new Vector3(x, origin.y, z);
    }

    /// <summary>
    /// 해당 인덱스로부터 주어진 범위만큼의 주변 인덱스들을 구함
    /// </summary>
    /// <param name="index">기준 인덱스</param>
    /// <param name="range">범위</param>
    /// <returns>기준 인덱스로부터 주어진 범위만큼 포함되는 인덱스들의 리스트</returns>
    public List<Vector2Int> GetInRangeIndices(Vector2Int index, int range)
    {
        return new();
    }

    /// <summary>
    /// axial(q, r)를 odd-q offset(col, row)로 변환합니다.
    /// </summary>
    private Vector2Int AxialToOddQOffset(int q, int r)
    {
        int row = q;
        int col = r + ((q - (q & 1)) >> 1);
        return new Vector2Int(col, row);
    }

    /// <summary>
    /// 연속 axial 좌표를 가장 가까운 정수 axial 좌표로 반올림합니다.
    /// </summary>
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

        // axial(q, r) = (cube.x, cube.z)
        return new Vector2Int(rx, rz);
    }

    /// <summary>
    /// 음수 좌표에서도 올바르게 동작하는 floor division
    /// </summary>
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
