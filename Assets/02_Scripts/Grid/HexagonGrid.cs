using System.Collections.Generic;
using UnityEngine;
using Fusion;

public sealed class HexagonGrid : MonoBehaviour
{
    [SerializeField] private TerritorySystem _territorySystem; // 영역 컴포넌트
    [SerializeField] private MeshCollider _gridBasePlaneCollider; // 그리드의 기반이 될 Plane의 콜라이더
    [SerializeField] private Vector2Int _gridCellCount; // 그리드의 셀 개수
    [SerializeField] private float _hexagonSize = 1.5f; // 육각형 중심에서 꼭짓점 까지의 거리
    [SerializeField] private Vector3 _gridOffset = Vector3.zero; // 그리드 위치 오프셋
    [SerializeField] private HexagonType _hexagonType = HexagonType.FlatTop; // Pointy-Top과 Flat-Top 중 선택
    [SerializeField, Range(1, 3)] private int _recheckRadius = 2; // 1이면 주변 7칸, 2면 주변 19칸

    private GridCell[,] _grid;

    // 육각형 구성 캐시
    private float _width => (_hexagonType == HexagonType.PointyTop) ? (0.8660254f * _hexagonSize) : (_hexagonSize); // 0.8660254: √3/2의 근사값
    private float _height => (_hexagonType == HexagonType.PointyTop) ? (_hexagonSize) : (0.8660254f * _hexagonSize);
    private float _xStep => (_hexagonType == HexagonType.PointyTop) ? (2f * _width) : (1.5f * _width); // x 간격
    private float _zStep => (_hexagonType == HexagonType.PointyTop) ? (1.5f * _height) : (2f * _height); // z 간격
    private const float SQRT3 = 1.7320508075688772f;

    // Plane 정보 캐시
    private Vector3 _planeMinPosition;

    private readonly HashSet<long> _visited = new HashSet<long>();

    public float GridHeight => transform.position.y + _gridOffset.y;

    #region MonoBehaviour

    private void OnValidate()
    {
        CalculatePlaneBound();
        InitGrid();
    }

    private void Awake()
    {
        OnValidate();
    }

    #endregion

    #region API

    /// <summary>
    /// 셀의 상태를 변경하는 함수
    /// </summary>
    /// <param name="index">변경할 인덱스</param>
    /// <param name="state">변경할 상태</param>
    public void ChangeCellState(Vector2Int changeIndex, CellState changeState)
    {
        _grid[changeIndex.x, changeIndex.y].SetState(changeState);
    }

    /// <summary>
    /// 셀이 비어있는지 확인하는 함수
    /// </summary>
    /// <param name="index">확인할 셀의 인덱스</param>
    /// <returns>셀이 비어있는지 여부</returns>
    public bool IsEmptyCell(Vector2Int index)
    {
        if (_grid[index.x, index.y].State == CellState.None) return true;
        return false;
    }

    /// <summary>
    /// 인덱스를 기반으로 해당 셀의 중심 위치를 찾는 함수
    /// </summary>
    /// <param name="index">위치를 찾을 인덱스</param>
    /// <returns>셀의 중심 위치</returns>
    public Vector3 GetNearCellPositionFromIndex(Vector2Int index)
    {
        return _grid[index.x, index.y].Position;
    }

    /// <summary>
    /// 위치를 기반으로 가장 가까운 셀의 중심점을 찾는 함수
    /// </summary>
    /// <param name="position">위치를 찾을 기반 위치</param>
    /// <returns>가장 가까운 셀의 위치</returns>
    public Vector3 GetNearCellPositionFromPosition(Vector3 position)
    {
        Vector2Int nearIndex = GetNearIndex(position);
        return _grid[nearIndex.x, nearIndex.y].Position;
    }

    /// <summary>
    /// 위치를 기반으로 가장 가까운 그리드 인덱스를 찾는 함수
    /// </summary>
    /// <param name="position">인덱스를 찾을 위치</param>
    /// <returns>가장 가까운 인덱스</returns>
    public Vector2Int GetNearIndex(Vector3 position)
    {
        if (_gridBasePlaneCollider == null)
            return new Vector2Int(-1, -1);

        // 최신 bound 반영 (에디터/런타임 둘 다 안전)
        CalculatePlaneBound();

        // 1) 월드XZ -> 그리드 원점(planeMin + gridOffset) 기준 상대좌표
        Vector2 originXZ = new Vector2(_planeMinPosition.x + _gridOffset.x,
                                       _planeMinPosition.z + _gridOffset.z);

        Vector2 p = new Vector2(position.x, position.z) - originXZ;

        // 2) 월드 -> axial 실수
        Vector2 axialF = WorldToAxialFloat(p);

        // 3) cube-rounding으로 정수 axial(qi,ri) 추정
        Vector2Int axialI = CubeRoundAxial(axialF);
        int qi = axialI.x;
        int ri = axialI.y;

        // 4) (핵심) 주변 후보들을 실제 거리로 재검증
        Vector2Int best = new Vector2Int(-1, -1);
        float bestSqr = float.PositiveInfinity;

        Vector2 wpXZ = new Vector2(position.x, position.z);

        // 중복 제거(클램프 때문에 같은 셀이 여러 번 나올 수 있음)
        System.Collections.Generic.HashSet<long> visited = new System.Collections.Generic.HashSet<long>();

        int radius = Mathf.Max(1, _recheckRadius);

        _visited.Clear();

        EnumerateAxialInRadius(qi, ri, radius, (q, r) =>
        {
            Vector2Int idx = AxialToOffsetIndex(q, r);

            if (idx.x < 0 || idx.x >= _gridCellCount.x || idx.y < 0 || idx.y >= _gridCellCount.y)
                return;

            long key = ((long)idx.x << 32) ^ (uint)idx.y;
            if (!_visited.Add(key))
                return;

            Vector3 center = GetCellCenter(idx.x, idx.y);
            Vector2 cXZ = new Vector2(center.x, center.z);
            float sqr = (wpXZ - cXZ).sqrMagnitude;

            if (sqr < bestSqr)
            {
                bestSqr = sqr;
                best = idx;
            }
        });

        // 혹시라도 best가 못 잡히면(예: gridCellCount가 0) 안전 처리
        if (best.x < 0)
        {
            int col = Mathf.Clamp(0, 0, _gridCellCount.x - 1);
            int row = Mathf.Clamp(0, 0, _gridCellCount.y - 1);
            return new Vector2Int(col, row);
        }

        return best;
    }

    /// <summary>
    /// radius값 만큼 주변 셀들의 상태를 복수로 변경하는 함수
    /// </summary>
    /// <param name="centerIndex">중심 인덱스</param>
    /// <param name="radius">변경 범위</param>
    /// <param name="occupyState">변경할 상태</param>
    /// <param name="requireEmpty">전부 비어있어야 하는지 여부</param>
    /// <returns>완료 여부</returns>
    public bool TryOccupyArea(Vector2Int centerIndex, int radius, CellState occupyState, bool requireEmpty = true)
    {
        var indices = GetIndicesInRadius(centerIndex, radius, includeCenter: true);

        // 1) 검증 단계 (전부 비었는지)
        if (requireEmpty)
        {
            for (int i = 0; i < indices.Count; i++)
            {
                var idx = indices[i];
                if (_grid[idx.x, idx.y].State != CellState.None) // CellState enum 참고 :contentReference[oaicite:3]{index=3}
                    return false;
            }
        }

        // 2) 커밋 단계 (한 번에 변경)
        for (int i = 0; i < indices.Count; i++)
        {
            var idx = indices[i];
            _grid[idx.x, idx.y].SetState(occupyState); // GridCell.SetState :contentReference[oaicite:4]{index=4}
        }

        return true;
    }

    /// <summary>
    /// 그리드의 중심 인덱스를 반환하는 함수
    /// </summary>
    /// <returns>그리드의 중심 인덱스</returns>
    public Vector2Int GetCenterIndex()
    {
        return _gridCellCount / 2;
    }

    /// <summary>
    /// 인덱스를 기반으로 영역 내부인지 검사하는 함수
    /// </summary>
    /// <param name="index">검사할 인덱스</param>
    /// <returns>영역 내부 여부</returns>
    public bool IsPointInTerritory(Vector2Int index)
    {
        Vector3 pos = _grid[index.x, index.y].Position;
        Vector2 point = new Vector2(pos.x, pos.z);
        return _territorySystem.Territory.IsPointInPolygon(point);
    }

    #endregion

    #region Core

    // Plane의 바운드 계산
    private void CalculatePlaneBound()
    {
        if (_gridBasePlaneCollider != null)
        {
            _planeMinPosition = _gridBasePlaneCollider.bounds.min;
        }
    }

    // 그리드 초기화
    private void InitGrid()
    {
        if (_gridBasePlaneCollider != null)
        {
            // 그리드 배열 초기화
            _grid = new GridCell[_gridCellCount.x, _gridCellCount.y];

            // 그리드의 각 셀 초기화
            for (int i = 0; i < _gridCellCount.y; i++)
            {
                for (int j = 0; j < _gridCellCount.x; j++)
                {
                    Vector3 cellCenter = GetCellCenter(j, i);

                    // 각 셀의 위치 초기화
                    _grid[j, i] = new GridCell(cellCenter);
                }
            }
        }
    }

    // 셀 중심 계산
    private Vector3 GetCellCenter(int col, int row)
    {
        float posY = _planeMinPosition.y + _gridOffset.y;

        float rowOffset = (row & 1) == 1 ? _width : 0f;
        float colOffset = (col & 1) == 1 ? _height : 0f;

        float posX = (_hexagonType == HexagonType.PointyTop)
            ? _planeMinPosition.x + _gridOffset.x + (col * _xStep) + rowOffset
            : _planeMinPosition.x + _gridOffset.x + (col * _xStep);

        float posZ = (_hexagonType == HexagonType.PointyTop)
            ? _planeMinPosition.z + _gridOffset.z + (row * _zStep)
            : _planeMinPosition.z + _gridOffset.z + (row * _zStep) + colOffset;

        return new Vector3(posX, posY, posZ);
    }

    // 중심 + 코너 인덱스(0~5)로 월드 코너 위치 반환
    private Vector3 GetHexCorner(Vector3 center, int cornerIndex)
    {
        // FlatTop: 0,60,120...
        // PointyTop: -30,30,90... (즉 30도 회전)
        float angleDeg = (_hexagonType == HexagonType.PointyTop)
            ? (60f * cornerIndex - 30f)
            : (60f * cornerIndex);

        float rad = angleDeg * Mathf.Deg2Rad;

        // XZ 평면 기준
        float x = Mathf.Cos(rad) * _hexagonSize;
        float z = Mathf.Sin(rad) * _hexagonSize;

        return center + new Vector3(x, 0f, z);
    }

    private Vector2Int AxialToOffsetIndex(int q, int r)
    {
        if (_hexagonType == HexagonType.PointyTop)
        {
            // odd-r (row가 홀수면 X로 밀림) - 너의 GetCellCenter와 일치
            int row = r;
            int col = q + ((r - (r & 1)) / 2);
            return new Vector2Int(col, row);
        }
        else
        {
            // odd-q (col이 홀수면 Z로 밀림) - 너의 GetCellCenter와 일치
            int col = q;
            int row = r + ((q - (q & 1)) / 2);
            return new Vector2Int(col, row);
        }
    }

    private Vector2 WorldToAxialFloat(Vector2 pXZ)
    {
        float s = _hexagonSize;

        if (_hexagonType == HexagonType.PointyTop)
        {
            // Pointy-Top axial
            // q = (sqrt3/3 * x - 1/3 * z) / s
            // r = (2/3 * z) / s
            float q = (SQRT3 / 3f * pXZ.x - 1f / 3f * pXZ.y) / s;
            float r = (2f / 3f * pXZ.y) / s;
            return new Vector2(q, r);
        }
        else
        {
            // Flat-Top axial
            // q = (2/3 * x) / s
            // r = (-1/3 * x + sqrt3/3 * z) / s
            float q = (2f / 3f * pXZ.x) / s;
            float r = (-1f / 3f * pXZ.x + SQRT3 / 3f * pXZ.y) / s;
            return new Vector2(q, r);
        }
    }

    // cube-rounding: (x=q, z=r, y=-x-z)
    private Vector2Int CubeRoundAxial(Vector2 axialFloat)
    {
        float cx = axialFloat.x;     // q
        float cz = axialFloat.y;     // r
        float cy = -cx - cz;

        int rx = Mathf.RoundToInt(cx);
        int ry = Mathf.RoundToInt(cy);
        int rz = Mathf.RoundToInt(cz);

        float dx = Mathf.Abs(rx - cx);
        float dy = Mathf.Abs(ry - cy);
        float dz = Mathf.Abs(rz - cz);

        if (dx > dy && dx > dz) rx = -ry - rz;
        else if (dy > dz) ry = -rx - rz;
        else rz = -rx - ry;

        // 정수 axial(q,r) = (x,z)
        return new Vector2Int(rx, rz);
    }

    // axial 중심(q0,r0) 주변 반경 radius(=0..N) 안의 axial들을 전부 열거 (hex region)
    private static void EnumerateAxialInRadius(int q0, int r0, int radius, System.Action<int, int> emit)
    {
        for (int dq = -radius; dq <= radius; dq++)
        {
            int drMin = Mathf.Max(-radius, -dq - radius);
            int drMax = Mathf.Min(radius, -dq + radius);

            for (int dr = drMin; dr <= drMax; dr++)
            {
                int q = q0 + dq;
                int r = r0 + dr;
                emit(q, r);
            }
        }
    }

    private Vector2Int OffsetToAxialIndex(Vector2Int index)
    {
        int col = index.x;
        int row = index.y;

        if (_hexagonType == HexagonType.PointyTop)
        {
            // odd-r 역변환
            int r = row;
            int q = col - ((r - (r & 1)) / 2);
            return new Vector2Int(q, r);
        }
        else
        {
            // odd-q 역변환
            int q = col;
            int r = row - ((q - (q & 1)) / 2);
            return new Vector2Int(q, r);
        }
    }

    private List<Vector2Int> GetIndicesInRadius(Vector2Int centerIndex, int radius, bool includeCenter = true)
    {
        var centerAx = OffsetToAxialIndex(centerIndex);
        var result = new List<Vector2Int>(radius == 1 ? 7 : 19);

        EnumerateAxialInRadius(centerAx.x, centerAx.y, radius, (q, r) =>
        {
            if (!includeCenter && q == centerAx.x && r == centerAx.y)
                return;

            Vector2Int idx = AxialToOffsetIndex(q, r);

            // "잘려나간" 경계 밖은 제외 (원하면 여기서 실패 처리로 바꿀 수도 있음)
            if (idx.x < 0 || idx.x >= _gridCellCount.x || idx.y < 0 || idx.y >= _gridCellCount.y)
                return;

            result.Add(idx);
        });

        return result;
    }

    #endregion

    // 디버그
    private void OnDrawGizmos()
    {
        if (_gridBasePlaneCollider == null) return;

        // 에디터에서 콜라이더/오프셋 바뀌는 걸 바로 반영
        CalculatePlaneBound();

        // 보기 좋은 디버그 크기(원하면 인스펙터로 빼도 됨)
        float centerDotRadius = Mathf.Max(0.03f, _hexagonSize * 0.06f);

        for (int row = 0; row < _gridCellCount.y; row++)
        {
            for (int col = 0; col < _gridCellCount.x; col++)
            {
                Vector3 center = GetCellCenter(col, row);

                // 1) 셀 중심 점
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(center, centerDotRadius);

                // 2) 셀 테두리 라인
                Gizmos.color = Color.cyan;

                Vector3 prev = GetHexCorner(center, 0);
                for (int k = 1; k <= 6; k++)
                {
                    Vector3 curr = GetHexCorner(center, k % 6);
                    Gizmos.DrawLine(prev, curr);
                    prev = curr;
                }
            }
        }
    }
}
