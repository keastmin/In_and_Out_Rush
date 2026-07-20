using UnityEngine;

namespace KIM.Dev
{
    public sealed class FogOfWarSystem : MonoBehaviour
    {
        private const float DefaultWorldSize = 512f;

        [Tooltip("전장의 안개가 적용될 카메라")]
        [SerializeField] private Camera _renderCamera;
        [Tooltip("전장의 안개에 가려져도 살짝 보이게 될 오브젝트들의 레이어")]
        [SerializeField] private LayerMask _fogVisibleLayers;
        [Tooltip("전장의 안개에 가려지면 완전히 모습을 감출 오브젝트들의 레이어")]
        [SerializeField] private LayerMask _fogHiddenLayers;
        [Tooltip("러너의 시야를 표현할 브러쉬 텍스쳐")]
        [SerializeField] private Texture2D _runnerVisionBrush;
        [Tooltip("플레이어 러너의 시야 범위")]
        [SerializeField] private float _playerRunnerVisibleRange = 5f;
        [Tooltip("안개의 밀도: 안개로 가려진 부분이 얼마나 어둡게 보일지 정하는 수치")]
        [SerializeField][Range(0f, 1f)] private float _fogDensity = 0.75f;
        [Tooltip("영역 오브젝트의 메쉬 필터")]
        [SerializeField] private MeshFilter _territoryMeshFilter;
        [Tooltip("영역 오브젝트의 메쉬 렌더러")]
        [SerializeField] private MeshRenderer _territoryMeshRenderer;
        [Tooltip("영역 오브젝트 모양에서 어느 정도 더 넓게 보일 것인지 정하는 수치")]
        [SerializeField][Min(0)] private float _territoryVisibleRange = 4f;

        private readonly FogOfWarRuntimeDriver _runtimeDriver = new();

        private PlayerRunner _playerRunner;
        private Texture2D _fallbackBrush;
        private InfiniteGrid _worldBoundsGrid;
        private MeshRenderer _worldBoundsRenderer;
        private float _worldSize = DefaultWorldSize;
        private Vector2 _worldCenter = Vector2.zero;
        private bool _hasExplicitWorldBounds;

        public LayerMask FogVisibleLayers => _fogVisibleLayers;

        public void InitializeFogOfWarSystem(PlayerRunner playerRunner)
        {
            _playerRunner = playerRunner;
        }

        public void SetTerritorySource(MeshFilter territoryMeshFilter, MeshRenderer territoryMeshRenderer)
        {
            _territoryMeshFilter = territoryMeshFilter;
            _territoryMeshRenderer = territoryMeshRenderer;
        }

        public void SetWorldBounds(float worldBoundaryRadius)
        {
            if (worldBoundaryRadius <= 0f)
                return;

            _worldSize = Mathf.Max(64f, worldBoundaryRadius * 2f);
            _worldCenter = Vector2.zero;
            _hasExplicitWorldBounds = true;
        }

        private void LateUpdate()
        {
            if (_playerRunner == null)
                return;

            RefreshSettings();
            _runtimeDriver.UpdateFogOfWar(
                transform,
                _fogHiddenLayers,
                _territoryMeshFilter,
                _playerRunner.transform,
                _runnerVisionBrush,
                _playerRunnerVisibleRange,
                _territoryVisibleRange,
                _fogDensity,
                _worldSize,
                _worldCenter);
        }

        private void OnDestroy()
        {
            _runtimeDriver.Dispose();
            DestroyIfNeeded(_fallbackBrush);
        }

        private void RefreshSettings()
        {
            if (_renderCamera == null)
            {
                _renderCamera = Camera.main;
            }

            if (_territoryMeshRenderer == null && _territoryMeshFilter != null)
            {
                _territoryMeshRenderer = _territoryMeshFilter.GetComponent<MeshRenderer>();
            }

            _playerRunnerVisibleRange = Mathf.Max(0f, _playerRunnerVisibleRange);
            _territoryVisibleRange = Mathf.Max(0f, _territoryVisibleRange);
            _fogDensity = Mathf.Clamp01(_fogDensity);
            _runnerVisionBrush ??= CreateFallbackBrush();
            ResolveWorldBounds();
        }

        private void ResolveWorldBounds()
        {
            if (TryResolveInfiniteGridBounds())
                return;

            if (_hasExplicitWorldBounds)
                return;

            if (_territoryMeshRenderer != null)
            {
                Bounds bounds = _territoryMeshRenderer.bounds;
                _worldSize = Mathf.Max(DefaultWorldSize, bounds.size.x, bounds.size.z);
            }
            else
            {
                _worldSize = DefaultWorldSize;
            }

            _worldCenter = Vector2.zero;
        }

        private bool TryResolveInfiniteGridBounds()
        {
            InfiniteGrid grid = InfiniteGrid.Instance;
            if (grid == null)
                return false;

            if (_worldBoundsGrid != grid || _worldBoundsRenderer == null)
            {
                _worldBoundsGrid = grid;
                _worldBoundsRenderer = grid.GetComponent<MeshRenderer>();
            }

            if (_worldBoundsRenderer == null)
                return false;

            Bounds bounds = _worldBoundsRenderer.bounds;
            float groundSize = Mathf.Max(bounds.size.x, bounds.size.z);
            if (groundSize <= Mathf.Epsilon)
                return false;

            _worldSize = groundSize;
            _worldCenter = new Vector2(bounds.center.x, bounds.center.z);
            return true;
        }

        private Texture2D CreateFallbackBrush()
        {
            if (_fallbackBrush != null)
                return _fallbackBrush;

            const int size = 128;
            _fallbackBrush = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                name = "Fallback Fog Runner Brush",
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                hideFlags = HideFlags.HideAndDontSave
            };

            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                    float alpha = 1f - Mathf.SmoothStep(0.65f, 1f, distance);
                    _fallbackBrush.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            _fallbackBrush.Apply(false, true);
            return _fallbackBrush;
        }

        private static void DestroyIfNeeded(Object target)
        {
            if (target != null)
            {
                Destroy(target);
            }
        }
    }
}
