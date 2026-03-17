using UnityEngine;
using System.Collections.Generic;

namespace Grid
{
    public partial class GridManager : MonoBehaviour
    {
        [Header("Grid")]
        [SerializeField] private GameObject _plane; // 지면 오브젝트
        [SerializeField][Min(1)] private int _gridRow = 20; // 그리드 행
        [SerializeField][Min(1)] private int _gridCol = 20; // 그리드 열
        [SerializeField][Min(0.001f)] private float _hexSize = 1.6f; // 정육각형 크기
        [SerializeField] private Vector3 _gridOffset = Vector3.zero; // 그리드 위치 오프셋
        [SerializeField] private bool _gridDebug = true; // 기즈모 디버그 여부

        [Header("Overlay Colors")]
        [SerializeField] private Color _noneColor = new Color(0.2f, 0.45f, 1f, 0.28f);
        [SerializeField] private Color _buildColor = new Color(1f, 0.2f, 0.2f, 0.30f);
        [SerializeField] private Color _previewTintColor = new Color(0.1f, 1f, 0.2f, 0.45f);
        [SerializeField][Range(0f, 1f)] private float _cellFill = 0.2f;
        [SerializeField] private bool _showCellStateOverlay = false;

        [Header("Territory")]
        [SerializeField] private TerritorySystem _territorySystem;

        public static GridManager Instance { get; private set; }

        // 그리드
        private HexaCell[,] _grid;

        // 그리드 캐시
        private Vector3 _gridOriginPosition;
        private const float SQRT3 = 1.7320508075688772f;
        private float _hexHeight;
        private float _gridTilingHeightOffset;
        private float _gridTilingWidthOffset;
        public float GridHeight => _gridOriginPosition.y;

        private MeshCollider _planeCollider;
        private Renderer _planeRenderer;
        private MaterialPropertyBlock _planePropertyBlock;

        // 텍스쳐
        private Texture2D _stateTexture;
        private Texture2D _previewTexture;
        private Texture2D _buffTexture;
        private bool _isTerritoryEventBound;
        private bool _previewEnabled;
        private readonly HashSet<Vector2Int> _previewCellIndices = new();
        private Color32[] _previewPixelsCache;
        private Color32[] _buffPixelsCache;

        private struct BuffSourceState
        {
            public Vector2Int CenterIndex;
            public int Range;
            public Color Color;
            public HashSet<Vector2Int> Cells;
        }

        private readonly Dictionary<int, BuffSourceState> _buffSources = new();
        private readonly Dictionary<Vector2Int, int> _buffCellRefCount = new();
        private readonly Dictionary<Vector2Int, Color> _buffCellColorSum = new();

        // 오버레이
        public bool ShowCellStateOverlay => _showCellStateOverlay;

        private void OnValidate()
        {
            if (_plane != null)
            {
                _plane.TryGetComponent(out _planeCollider);
                _plane.TryGetComponent(out _planeRenderer);
                if (_planePropertyBlock == null)
                {
                    _planePropertyBlock = new MaterialPropertyBlock();
                }
            }

            if(_planeCollider != null && _planeRenderer != null && _planePropertyBlock != null)
            {
                InitGrid(_gridCol, _gridRow);
                PushShaderData();
            }
        }

        private void Awake()
        {
            OnValidate();

            _showCellStateOverlay = false;
            Instance = this;
            BindTerritoryEventsIfNeeded();
        }

        // Flat-top 육각 타일링
        private void InitGrid(int col, int row)
        {
            _grid = new HexaCell[col, row];
            InitGridVariable();
            FindGridOriginPosition();
            InitEachCellCenter();
            RebuildCellStateTextures();
        }

        // 그리드 계산에 사용할 변수들 초기화
        private void InitGridVariable()
        {
            _hexHeight = _hexSize * (SQRT3 / 2f);
            _gridTilingHeightOffset = SQRT3 * _hexSize;
            _gridTilingWidthOffset = 1.5f * _hexSize;
        }

        // 그리드를 시작할 오리진 포지션 계산
        private void FindGridOriginPosition()
        {
            Vector3 planeBoundMin = _planeCollider.bounds.min;
            _gridOriginPosition = planeBoundMin + new Vector3(_hexSize, 0f, _hexHeight) + _gridOffset;
        }

        // 각 셀의 중심점 계산 후 셀 객체 생성
        private void InitEachCellCenter()
        {
            for (int i = 0; i < _gridCol; i++)
            {
                float height = i * _gridTilingHeightOffset;
                for (int j = 0; j < _gridRow; j++)
                {
                    float rowHeightOffset = (j % 2 == 0) ? 0f : _hexHeight;
                    float width = j * _gridTilingWidthOffset;

                    Vector3 center = _gridOriginPosition + new Vector3(width, 0f, height + rowHeightOffset);
                    Vector3[] vertices = new Vector3[6];
                    for (int k = 0; k < 6; k++)
                    {
                        float rad = (60f * (k % 6)) * Mathf.Deg2Rad;
                        vertices[k] = center + new Vector3(Mathf.Cos(rad) * _hexSize, 0f, Mathf.Sin(rad) * _hexSize);
                    }

                    _grid[i, j] = new HexaCell(center, vertices);
                }
            }
        }

        private void RebuildCellStateTextures()
        {
            if (_grid == null) return;

            if (_stateTexture == null || _stateTexture.width != _gridRow || _stateTexture.height != _gridCol)
            {
                _stateTexture = CreateGridTexture("GridCellStateTex");
            }
            if (_previewTexture == null || _previewTexture.width != _gridRow || _previewTexture.height != _gridCol)
            {
                _previewTexture = CreateGridTexture("GridCellPreviewTex");
                RebuildPreviewTexture();
            }
            if (_buffTexture == null || _buffTexture.width != _gridRow || _buffTexture.height != _gridCol)
            {
                _buffTexture = CreateGridTexture("GridCellBuffTex");
                RebuildBuffTexture();
            }

            Color32[] statePixels = new Color32[_gridRow * _gridCol];

            int idx = 0;
            for (int i = 0; i < _gridCol; i++)
            {
                for (int j = 0; j < _gridRow; j++)
                {
                    bool inTerritory = IsCellInTerritory(i, j);
                    bool canBuild = inTerritory && !_grid[i, j].IsBuild;
                    byte state = (byte)(canBuild ? 0 : 255);

                    statePixels[idx] = new Color32(state, 0, 0, 255);
                    idx++;
                }
            }

            _stateTexture.SetPixels32(statePixels);
            _stateTexture.Apply(false, false);
        }

        private void RebuildPreviewTexture()
        {
            if (_grid == null) return;

            if (_previewTexture == null || _previewTexture.width != _gridRow || _previewTexture.height != _gridCol)
            {
                _previewTexture = CreateGridTexture("GridCellPreviewTex");
            }

            int pixelCount = _gridRow * _gridCol;
            if (_previewPixelsCache == null || _previewPixelsCache.Length != pixelCount)
            {
                _previewPixelsCache = new Color32[pixelCount];
            }

            for (int i = 0; i < pixelCount; i++)
            {
                _previewPixelsCache[i] = new Color32(0, 0, 0, 255);
            }

            if (_previewEnabled)
            {
                foreach (var idx in _previewCellIndices)
                {
                    if (!IsValidCell(idx.x, idx.y)) continue;
                    int flatIndex = (idx.x * _gridRow) + idx.y;
                    _previewPixelsCache[flatIndex] = new Color32(255, 0, 0, 255);
                }
            }

            _previewTexture.SetPixels32(_previewPixelsCache);
            _previewTexture.Apply(false, false);
        }

        private void RebuildBuffTexture()
        {
            if (_grid == null) return;

            if (_buffTexture == null || _buffTexture.width != _gridRow || _buffTexture.height != _gridCol)
            {
                _buffTexture = CreateGridTexture("GridCellBuffTex");
            }

            int pixelCount = _gridRow * _gridCol;
            if (_buffPixelsCache == null || _buffPixelsCache.Length != pixelCount)
            {
                _buffPixelsCache = new Color32[pixelCount];
            }

            for (int i = 0; i < pixelCount; i++)
            {
                _buffPixelsCache[i] = new Color32(0, 0, 0, 0);
            }

            foreach (var pair in _buffCellRefCount)
            {
                if (pair.Value <= 0) continue;
                Vector2Int idx = pair.Key;
                if (!IsValidCell(idx.x, idx.y)) continue;

                int flatIndex = (idx.x * _gridRow) + idx.y;
                Color sumColor = _buffCellColorSum.TryGetValue(idx, out var cachedColor) ? cachedColor : Color.clear;
                float divisor = Mathf.Max(1, pair.Value);
                Color mixedColor = sumColor / divisor;
                _buffPixelsCache[flatIndex] = (Color32)mixedColor;
            }

            _buffTexture.SetPixels32(_buffPixelsCache);
            _buffTexture.Apply(false, false);
        }

        private void OnDestroy()
        {
            if (_isTerritoryEventBound && _territorySystem != null)
            {
                _territorySystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
            }
        }

        private void BindTerritoryEventsIfNeeded()
        {
            if (_isTerritoryEventBound || _territorySystem == null) return;

            _territorySystem.OnTerritoryExpandedEvent += OnTerritoryExpanded;
            _isTerritoryEventBound = true;
        }

        private void OnTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
        {
            RebuildCellStateTextures();
            PushShaderData();
        }

        private Texture2D CreateGridTexture(string texName)
        {
            Texture2D texture = new Texture2D(_gridRow, _gridCol, TextureFormat.RGBA32, false, true)
            {
                name = texName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp
            };
            return texture;
        }

        private void PushShaderData()
        {
            if (_grid == null || _planeRenderer == null) return;

            _planeRenderer.GetPropertyBlock(_planePropertyBlock);
            _planePropertyBlock.SetVector("_GridOriginWS", _gridOriginPosition);
            _planePropertyBlock.SetFloat("_HexSize", _hexSize);
            _planePropertyBlock.SetFloat("_GridCols", _gridCol);
            _planePropertyBlock.SetFloat("_GridRows", _gridRow);
            _planePropertyBlock.SetColor("_NoneColor", _noneColor);
            _planePropertyBlock.SetColor("_BuildColor", _buildColor);
            _planePropertyBlock.SetColor("_PreviewTintColor", _previewTintColor);
            _planePropertyBlock.SetFloat("_CellFill", _cellFill);
            _planePropertyBlock.SetFloat("_StateOverlayEnabled", _showCellStateOverlay ? 1f : 0f);
            _planePropertyBlock.SetFloat("_PreviewEnabled", _previewEnabled ? 1f : 0f);
            _planePropertyBlock.SetTexture("_StateTex", _stateTexture);
            _planePropertyBlock.SetTexture("_PreviewTex", _previewTexture);
            _planePropertyBlock.SetTexture("_BuffTex", _buffTexture);
            _planeRenderer.SetPropertyBlock(_planePropertyBlock);
        }

        private bool IsValidCell(int col, int row)
        {
            return _grid != null && col >= 0 && row >= 0 && col < _gridCol && row < _gridRow; 
        }

        #region 그리드 계산

        private void EvaluateAxialCandidateAndNeighbors(int q, int r, Vector3 worldPos, ref Vector2Int best, ref float bestD2)
        {
            // axial directions (q,r)
            // (q+1,r), (q+1,r-1), (q,r-1), (q-1,r), (q-1,r+1), (q,r+1)
            CheckAxial(q, r, worldPos, ref best, ref bestD2);

            CheckAxial(q + 1, r, worldPos, ref best, ref bestD2);
            CheckAxial(q + 1, r - 1, worldPos, ref best, ref bestD2);
            CheckAxial(q, r - 1, worldPos, ref best, ref bestD2);
            CheckAxial(q - 1, r, worldPos, ref best, ref bestD2);
            CheckAxial(q - 1, r + 1, worldPos, ref best, ref bestD2);
            CheckAxial(q, r + 1, worldPos, ref best, ref bestD2);
        }

        private void CheckAxial(int q, int r, Vector3 worldPos, ref Vector2Int best, ref float bestD2)
        {
            Vector2Int idx = AxialToOddQOffset(q, r);
            int col = idx.x;
            int row = idx.y;

            if (!IsValidCell(col, row)) return;

            Vector3 c = GetCellCenterFast(col, row);

            float dx = c.x - worldPos.x;
            float dz = c.z - worldPos.z;
            float d2 = dx * dx + dz * dz;

            if (d2 < bestD2)
            {
                bestD2 = d2;
                best = idx;
            }
        }

        // axial(q,r) -> odd-q offset(col,row)
        private Vector2Int AxialToOddQOffset(int q, int r)
        {
            int row = q;
            int col = r + ((q - (q & 1)) >> 1); // (q - (q&1))/2 = floor(q/2)
            return new Vector2Int(col, row);
        }

        /// <summary>
        /// _grid 배열 접근 없이(캐시 미스 줄임), 생성 규칙 그대로 중심점 계산.
        /// </summary>
        private Vector3 GetCellCenterFast(int col, int row)
        {
            float xOffset = _hexSize * 1.5f;          // 3/2 * size
            float zOffset = _hexSize * SQRT3;         // sqrt(3) * size
            float halfZ = _hexSize * (SQRT3 * 0.5f);// sqrt(3)/2 * size

            float x = _gridOriginPosition.x + xOffset * row;
            float z = _gridOriginPosition.z + zOffset * col + ((row & 1) == 1 ? halfZ : 0f);

            return new Vector3(x, _gridOriginPosition.y, z);
        }

        #endregion

        private void OnDrawGizmos()
        {
            if (_planeCollider == null || !_gridDebug || _grid == null) return;

            foreach (var cell in _grid)
            {
                Vector3 c = cell.CenterPosition;

                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(c, 0.05f);

                Gizmos.color = new Color(0.0f, 0.85f, 1f, 1f);
                int l = cell.Vertices.Length;
                for (int i = 0; i < l; i++)
                {
                    Vector3 pos1 = cell.Vertices[i];
                    Vector3 pos2 = cell.Vertices[(i + 1) % l];
                    Gizmos.DrawLine(pos1, pos2);
                }
            }
        }
    }
}
