using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class InfiniteGridVisualController
    {
        private static readonly int GridOriginId = Shader.PropertyToID("_GridOriginWS");
        private static readonly int HexSizeId = Shader.PropertyToID("_HexSize");
        private static readonly int CellFillId = Shader.PropertyToID("_CellFill");
        private static readonly int StateOverlayEnabledId = Shader.PropertyToID("_StateOverlayEnabled");
        private static readonly int PrimaryGuideColorId = Shader.PropertyToID("_PrimaryGuideColor");
        private static readonly int SecondaryGuideColorId = Shader.PropertyToID("_SecondaryGuideColor");
        private static readonly int PreviewValidGuideColorId = Shader.PropertyToID("_PreviewValidGuideColor");
        private static readonly int PreviewBlockedGuideColorId = Shader.PropertyToID("_PreviewBlockedGuideColor");
        private static readonly int VisibleCellMinId = Shader.PropertyToID("_VisibleCellMin");
        private static readonly int VisibleCellSizeId = Shader.PropertyToID("_VisibleCellSize");
        private static readonly int GridStateTextureId = Shader.PropertyToID("_GridStateTex");
        private static readonly int GridBuffTextureId = Shader.PropertyToID("_GridBuffTex");
        private static readonly int BuffDataEnabledId = Shader.PropertyToID("_BuffDataEnabled");

        private readonly GridChunkVisibilityResolver _visibilityResolver = new();
        private readonly InfiniteGridOccupancyIndex _occupancyIndex = new();
        private readonly InfiniteGridChunkStateCache _chunkStateCache = new();
        private readonly InfiniteGridTerritoryChangeTracker _territoryChangeTracker = new();
        private readonly HashSet<Vector2Int> _previewCells = new();
        private readonly HashSet<Vector2Int> _blockedPreviewCells = new();
        private readonly HashSet<Vector2Int> _incomingPreviewCells = new();
        private readonly HashSet<Vector2Int> _incomingBlockedPreviewCells = new();

        private Renderer _groundRenderer;
        private Transform _ownerTransform;
        private InfiniteGridLayoutSettings _layout;
        private InfiniteGridGuideSettings _guide;
        private InfiniteGridRenderingSettings _rendering;
        private GridCalculator _gridCalculator;
        private Territory _territory;
        private IReadOnlyDictionary<Vector2Int, int> _buffCellRefCount;
        private IReadOnlyDictionary<Vector2Int, Color> _buffCellColorSum;
        private MaterialPropertyBlock _propertyBlock;
        private Texture2D _gridStateTexture;
        private Texture2D _gridBuffTexture;
        private Color32[] _gridStatePixels;
        private Color32[] _gridBuffPixels;
        private GridVisibleChunkRange _visibleRange;
        private Vector2Int _visibleCellMin;
        private Vector3 _lastGridOrigin;
        private int _visibleCellWidth;
        private int _visibleCellHeight;
        private float _lastCellSize;
        private bool _hasVisibleRange;
        private bool _hasGridOrigin;
        private bool _hasCellSize;
        private bool _baseStateDirty = true;
        private bool _previewDirty = true;
        private bool _buffDirty = true;
        private bool _visibleCellDataEnabled;
        private bool _stateCellDataEnabled;
        private bool _textureLimitWarningIssued;

        public void Apply(
            GameObject owner,
            Transform ownerTransform,
            InfiniteGridLayoutSettings layout,
            InfiniteGridGuideSettings guide,
            InfiniteGridRenderingSettings rendering,
            GridCalculator gridCalculator,
            IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid,
            IEnumerable<Vector2Int> additionalOccupiedIndices,
            IEnumerable<Vector2Int> previewIndices,
            IEnumerable<Vector2Int> blockedPreviewIndices,
            IReadOnlyDictionary<Vector2Int, int> buffCellRefCount,
            IReadOnlyDictionary<Vector2Int, Color> buffCellColorSum,
            Territory territory)
        {
            Apply(
                owner,
                ownerTransform,
                layout,
                guide,
                rendering,
                gridCalculator,
                networkGrid,
                additionalOccupiedIndices,
                previewIndices,
                blockedPreviewIndices,
                buffCellRefCount,
                buffCellColorSum,
                territory,
                InfiniteGridVisualDirtyFlags.All);
        }

        public void Apply(
            GameObject owner,
            Transform ownerTransform,
            InfiniteGridLayoutSettings layout,
            InfiniteGridGuideSettings guide,
            InfiniteGridRenderingSettings rendering,
            GridCalculator gridCalculator,
            IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid,
            IEnumerable<Vector2Int> additionalOccupiedIndices,
            IEnumerable<Vector2Int> previewIndices,
            IEnumerable<Vector2Int> blockedPreviewIndices,
            IReadOnlyDictionary<Vector2Int, int> buffCellRefCount,
            IReadOnlyDictionary<Vector2Int, Color> buffCellColorSum,
            Territory territory,
            InfiniteGridVisualDirtyFlags dirtyFlags)
        {
            if (owner == null ||
                ownerTransform == null ||
                layout == null ||
                guide == null ||
                rendering == null ||
                gridCalculator == null)
            {
                return;
            }

            bool rendererChanged = _groundRenderer == null || _groundRenderer.gameObject != owner;
            if (rendererChanged && !owner.TryGetComponent(out _groundRenderer))
            {
                return;
            }

            bool calculatorChanged = _gridCalculator != gridCalculator;
            bool cellSizeChanged = !_hasCellSize || !Mathf.Approximately(_lastCellSize, layout.CellSize);
            Vector3 gridOrigin = layout.ResolveOrigin(ownerTransform);
            bool gridOriginChanged = !_hasGridOrigin || gridOrigin != _lastGridOrigin;

            _ownerTransform = ownerTransform;
            _layout = layout;
            _guide = guide;
            _rendering = rendering;
            _gridCalculator = gridCalculator;
            _lastCellSize = layout.CellSize;
            _hasCellSize = true;
            _lastGridOrigin = gridOrigin;
            _hasGridOrigin = true;

            if (calculatorChanged)
            {
                _chunkStateCache.Clear();
            }

            if (rendererChanged || calculatorChanged || cellSizeChanged || gridOriginChanged)
            {
                _hasVisibleRange = false;
                _previewDirty = true;
                _buffDirty = true;
            }

            if ((dirtyFlags & InfiniteGridVisualDirtyFlags.Settings) != 0)
            {
                _hasVisibleRange = false;
            }

            if (rendering.ApplyInfiniteGridMaterial &&
                rendering.InfiniteGridMaterial != null &&
                _groundRenderer.sharedMaterial != rendering.InfiniteGridMaterial)
            {
                _groundRenderer.sharedMaterial = rendering.InfiniteGridMaterial;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _groundRenderer.GetPropertyBlock(_propertyBlock);

            bool gridGeometryChanged =
                calculatorChanged || cellSizeChanged || gridOriginChanged;
            if (gridGeometryChanged)
            {
                InvalidateBaseState();
            }

            bool legacyBaseStateDirty =
                (dirtyFlags & InfiniteGridVisualDirtyFlags.BaseState) != 0;
            bool territoryStateDirty =
                legacyBaseStateDirty ||
                (dirtyFlags & InfiniteGridVisualDirtyFlags.Territory) != 0;
            bool occupancyStateDirty =
                legacyBaseStateDirty ||
                (dirtyFlags & InfiniteGridVisualDirtyFlags.Occupancy) != 0;

            if (territoryStateDirty)
            {
                _territory = territory;
                if (_territoryChangeTracker.Capture(territory))
                {
                    if (_territoryChangeTracker.RequiresFullInvalidation)
                    {
                        _chunkStateCache.Invalidate();
                    }
                    else
                    {
                        _chunkStateCache.InvalidateWorldBounds(
                            _territoryChangeTracker.ChangedMin,
                            _territoryChangeTracker.ChangedMax,
                            _gridCalculator,
                            _lastGridOrigin,
                            _layout.CellSize);
                    }

                    _baseStateDirty = true;
                }
            }

            if (occupancyStateDirty &&
                _occupancyIndex.Rebuild(
                    _gridCalculator,
                    networkGrid,
                    additionalOccupiedIndices))
            {
                _chunkStateCache.InvalidateChunks(_occupancyIndex.ChangedChunks);
                _baseStateDirty = true;
            }

            if ((dirtyFlags & InfiniteGridVisualDirtyFlags.Preview) != 0)
            {
                _previewDirty |= RebuildPreviewCells(previewIndices, blockedPreviewIndices);
            }

            if ((dirtyFlags & InfiniteGridVisualDirtyFlags.Buff) != 0)
            {
                _buffCellRefCount = buffCellRefCount;
                _buffCellColorSum = buffCellColorSum;
                _buffDirty = true;
            }

            ApplySettings();
            _groundRenderer.SetPropertyBlock(_propertyBlock);
        }

        public void UpdateVisibleChunks(Camera renderCamera)
        {
            if (_groundRenderer == null ||
                _ownerTransform == null ||
                _layout == null ||
                _rendering == null ||
                _gridCalculator == null ||
                _propertyBlock == null)
            {
                return;
            }

            Vector3 gridOrigin = _layout.ResolveOrigin(_ownerTransform);
            if (!_hasGridOrigin || gridOrigin != _lastGridOrigin)
            {
                _lastGridOrigin = gridOrigin;
                _hasGridOrigin = true;
                _hasVisibleRange = false;
                InvalidateBaseState();
                _previewDirty = true;
                _buffDirty = true;
            }

            if (!HasVisibleCellData())
            {
                ApplySettings();
                DisableVisibleCellData();
                return;
            }

            if (!_visibilityResolver.TryResolve(
                    renderCamera,
                    gridOrigin.y,
                    gridOrigin,
                    _layout.CellSize,
                    _rendering.VisibleChunkPadding,
                    _gridCalculator,
                    _groundRenderer.bounds,
                    out GridVisibleChunkRange visibleRange))
            {
                DisableVisibleCellData();
                return;
            }

            if (!_hasVisibleRange || visibleRange != _visibleRange)
            {
                if (!TrySetVisibleRange(visibleRange))
                {
                    DisableVisibleCellData();
                    return;
                }
            }

            bool stateTextureChanged = false;
            bool buffTextureChanged = false;
            bool buffBindingChanged = false;
            bool hasStateCellData = HasStateCellData();
            bool stateBindingChanged =
                hasStateCellData != _stateCellDataEnabled;
            if (hasStateCellData)
            {
                EnsureStateTextureStorage();
            }

            if (_baseStateDirty && hasStateCellData)
            {
                RebuildBaseStateTexture();
                stateTextureChanged = true;
            }

            if (_previewDirty && hasStateCellData)
            {
                RebuildPreviewTextureChannels();
                stateTextureChanged = true;
            }

            if (_buffDirty)
            {
                buffTextureChanged = RebuildBuffTexture();
                buffBindingChanged = true;
            }

            if (stateTextureChanged)
            {
                UploadStateTexture();
            }

            if (buffTextureChanged)
            {
                UploadBuffTexture();
            }

            if (!stateTextureChanged &&
                !buffTextureChanged &&
                !stateBindingChanged &&
                !buffBindingChanged)
            {
                return;
            }

            ApplySettings();
            _propertyBlock.SetVector(
                VisibleCellMinId,
                new Vector4(_visibleCellMin.x, _visibleCellMin.y, 0f, 0f));
            _propertyBlock.SetVector(
                VisibleCellSizeId,
                new Vector4(_visibleCellWidth, _visibleCellHeight, 0f, 0f));
            _propertyBlock.SetTexture(
                GridStateTextureId,
                hasStateCellData && _gridStateTexture != null
                    ? _gridStateTexture
                    : Texture2D.blackTexture);
            _propertyBlock.SetTexture(
                GridBuffTextureId,
                HasBuffCellData() && _gridBuffTexture != null
                    ? _gridBuffTexture
                    : Texture2D.blackTexture);
            _propertyBlock.SetFloat(
                BuffDataEnabledId,
                HasBuffCellData() && _gridBuffTexture != null ? 1f : 0f);
            _groundRenderer.SetPropertyBlock(_propertyBlock);
            _visibleCellDataEnabled = true;
            _stateCellDataEnabled = hasStateCellData;
        }

        private void ApplySettings()
        {
            if (_propertyBlock == null || _layout == null || _guide == null || _ownerTransform == null)
            {
                return;
            }

            _propertyBlock.SetVector(GridOriginId, _layout.ResolveOrigin(_ownerTransform));
            _propertyBlock.SetFloat(HexSizeId, _layout.CellSize);
            _propertyBlock.SetFloat(CellFillId, _layout.CellFill);
            _propertyBlock.SetFloat(StateOverlayEnabledId, _layout.ShowCellStateOverlay ? 1f : 0f);
            _propertyBlock.SetColor(PrimaryGuideColorId, _guide.PrimaryGuideColor);
            _propertyBlock.SetColor(SecondaryGuideColorId, _guide.SecondaryGuideColor);
            _propertyBlock.SetColor(PreviewValidGuideColorId, _guide.PreviewValidGuideColor);
            _propertyBlock.SetColor(PreviewBlockedGuideColorId, _guide.PreviewBlockedGuideColor);
        }

        private bool RebuildPreviewCells(
            IEnumerable<Vector2Int> previewIndices,
            IEnumerable<Vector2Int> blockedPreviewIndices)
        {
            _incomingPreviewCells.Clear();
            _incomingBlockedPreviewCells.Clear();

            if (previewIndices != null)
            {
                foreach (Vector2Int previewIndex in previewIndices)
                {
                    _incomingPreviewCells.Add(previewIndex);
                }
            }

            if (blockedPreviewIndices != null)
            {
                foreach (Vector2Int previewIndex in blockedPreviewIndices)
                {
                    _incomingBlockedPreviewCells.Add(previewIndex);
                    _incomingPreviewCells.Remove(previewIndex);
                }
            }

            if (_previewCells.SetEquals(_incomingPreviewCells) &&
                _blockedPreviewCells.SetEquals(_incomingBlockedPreviewCells))
            {
                return false;
            }

            _previewCells.Clear();
            _previewCells.UnionWith(_incomingPreviewCells);
            _blockedPreviewCells.Clear();
            _blockedPreviewCells.UnionWith(_incomingBlockedPreviewCells);
            return true;
        }

        private bool HasVisibleCellData()
        {
            return HasStateCellData() || HasBuffCellData();
        }

        private bool HasStateCellData()
        {
            return _layout.ShowCellStateOverlay ||
                   _previewCells.Count > 0 ||
                   _blockedPreviewCells.Count > 0;
        }

        private bool HasBuffCellData()
        {
            return _buffCellRefCount != null && _buffCellRefCount.Count > 0;
        }

        private void InvalidateBaseState()
        {
            _chunkStateCache.Invalidate();
            _baseStateDirty = true;
        }

        private bool TrySetVisibleRange(GridVisibleChunkRange visibleRange)
        {
            long chunkWidth = (long)visibleRange.Max.X - visibleRange.Min.X + 1L;
            long chunkHeight = (long)visibleRange.Max.Y - visibleRange.Min.Y + 1L;
            long widthLong = chunkWidth * _gridCalculator.ChunkColumnCount;
            long heightLong = chunkHeight * _gridCalculator.ChunkRowCount;
            long pixelCountLong =
                widthLong > 0L && heightLong > 0L && widthLong <= long.MaxValue / heightLong
                    ? widthLong * heightLong
                    : long.MaxValue;
            int maxTextureSize = Mathf.Max(1, SystemInfo.maxTextureSize);

            if (widthLong <= 0L ||
                heightLong <= 0L ||
                widthLong > maxTextureSize ||
                heightLong > maxTextureSize ||
                pixelCountLong <= 0L ||
                pixelCountLong > _rendering.MaxVisibleCellCount ||
                pixelCountLong > int.MaxValue)
            {
                if (!_textureLimitWarningIssued)
                {
                    Debug.LogWarning(
                        $"Visible grid chunk atlas {widthLong}x{heightLong} exceeds the configured " +
                        $"{_rendering.MaxVisibleCellCount} cell or {maxTextureSize} texture limit. " +
                        "The guide is hidden instead of showing incorrect cell states.");
                    _textureLimitWarningIssued = true;
                }

                return false;
            }

            _textureLimitWarningIssued = false;
            int width = (int)widthLong;
            int height = (int)heightLong;
            int pixelCount = (int)pixelCountLong;
            _visibleRange = visibleRange;
            _visibleCellMin = visibleRange.GetMinCellIndex(_gridCalculator);
            _visibleCellWidth = width;
            _visibleCellHeight = height;
            _hasVisibleRange = true;

            if (HasBuffCellData() &&
                (_gridBuffPixels == null || _gridBuffPixels.Length != pixelCount))
            {
                _gridBuffPixels = new Color32[pixelCount];
            }

            if (HasStateCellData())
            {
                EnsureStateTextureStorage();
            }

            _baseStateDirty = true;
            _previewDirty = true;
            _buffDirty = true;
            return true;
        }

        private void RebuildBaseStateTexture()
        {
            _chunkStateCache.RebuildVisibleAtlas(
                _visibleRange,
                _gridCalculator,
                _lastGridOrigin,
                _layout.CellSize,
                _territory,
                _occupancyIndex,
                _gridStatePixels,
                _visibleCellWidth,
                _visibleCellMin,
                _rendering.MaxCachedChunkCount);
            _baseStateDirty = false;
        }

        private void RebuildPreviewTextureChannels()
        {
            for (int i = 0; i < _gridStatePixels.Length; i++)
            {
                Color32 state = _gridStatePixels[i];
                state.g = 0;
                state.b = 0;
                _gridStatePixels[i] = state;
            }

            foreach (Vector2Int index in _previewCells)
            {
                if (!TryGetPixelIndex(index, out int pixelIndex))
                {
                    continue;
                }

                Color32 state = _gridStatePixels[pixelIndex];
                state.g = byte.MaxValue;
                _gridStatePixels[pixelIndex] = state;
            }

            foreach (Vector2Int index in _blockedPreviewCells)
            {
                if (!TryGetPixelIndex(index, out int pixelIndex))
                {
                    continue;
                }

                Color32 state = _gridStatePixels[pixelIndex];
                state.g = 0;
                state.b = byte.MaxValue;
                _gridStatePixels[pixelIndex] = state;
            }

            _previewDirty = false;
        }

        private bool RebuildBuffTexture()
        {
            if (!HasBuffCellData())
            {
                _buffDirty = false;
                return false;
            }

            EnsureTexture(
                ref _gridBuffTexture,
                _visibleCellWidth,
                _visibleCellHeight,
                "Visible Grid Buff Atlas");
            int pixelCount = _visibleCellWidth * _visibleCellHeight;
            if (_gridBuffPixels == null || _gridBuffPixels.Length != pixelCount)
            {
                _gridBuffPixels = new Color32[pixelCount];
            }

            System.Array.Clear(_gridBuffPixels, 0, _gridBuffPixels.Length);
            for (int localRow = 0; localRow < _visibleCellHeight; localRow++)
            {
                int row = _visibleCellMin.y + localRow;
                for (int localCol = 0; localCol < _visibleCellWidth; localCol++)
                {
                    Vector2Int index = new Vector2Int(_visibleCellMin.x + localCol, row);
                    if (!_buffCellRefCount.TryGetValue(index, out int referenceCount) ||
                        referenceCount <= 0)
                    {
                        continue;
                    }

                    Color sumColor =
                        _buffCellColorSum != null &&
                        _buffCellColorSum.TryGetValue(index, out Color cachedColor)
                            ? cachedColor
                            : Color.clear;
                    _gridBuffPixels[GetPixelIndex(localCol, localRow)] =
                        sumColor / Mathf.Max(1, referenceCount);
                }
            }

            _buffDirty = false;
            return true;
        }

        private void EnsureStateTextureStorage()
        {
            int pixelCount = _visibleCellWidth * _visibleCellHeight;
            if (_gridStatePixels == null || _gridStatePixels.Length != pixelCount)
            {
                _gridStatePixels = new Color32[pixelCount];
            }

            EnsureTexture(
                ref _gridStateTexture,
                _visibleCellWidth,
                _visibleCellHeight,
                "Visible Grid State Atlas");
        }

        private void UploadStateTexture()
        {
            _gridStateTexture.SetPixels32(_gridStatePixels);
            _gridStateTexture.Apply(false, false);
        }

        private void UploadBuffTexture()
        {
            _gridBuffTexture.SetPixels32(_gridBuffPixels);
            _gridBuffTexture.Apply(false, false);
        }

        private int GetPixelIndex(int localCol, int localRow)
        {
            return localRow * _visibleCellWidth + localCol;
        }

        private bool TryGetPixelIndex(Vector2Int index, out int pixelIndex)
        {
            int localCol = index.x - _visibleCellMin.x;
            int localRow = index.y - _visibleCellMin.y;
            if (localCol < 0 ||
                localCol >= _visibleCellWidth ||
                localRow < 0 ||
                localRow >= _visibleCellHeight)
            {
                pixelIndex = -1;
                return false;
            }

            pixelIndex = GetPixelIndex(localCol, localRow);
            return true;
        }

        private void DisableVisibleCellData()
        {
            if (_propertyBlock == null || _groundRenderer == null)
            {
                return;
            }

            _hasVisibleRange = false;
            if (!_visibleCellDataEnabled)
            {
                return;
            }

            _propertyBlock.SetVector(VisibleCellSizeId, Vector4.zero);
            _groundRenderer.SetPropertyBlock(_propertyBlock);
            _visibleCellDataEnabled = false;
            _stateCellDataEnabled = false;
        }

        private static void EnsureTexture(
            ref Texture2D texture,
            int width,
            int height,
            string textureName)
        {
            if (texture != null && texture.width == width && texture.height == height)
            {
                return;
            }

            ReleaseTexture(ref texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false, true)
            {
                name = textureName,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontSave
            };
        }

        public void Release()
        {
            if (_propertyBlock != null && _groundRenderer != null)
            {
                _propertyBlock.SetVector(VisibleCellSizeId, Vector4.zero);
                _propertyBlock.SetTexture(GridStateTextureId, Texture2D.blackTexture);
                _propertyBlock.SetTexture(GridBuffTextureId, Texture2D.blackTexture);
                _propertyBlock.SetFloat(BuffDataEnabledId, 0f);
                _groundRenderer.SetPropertyBlock(_propertyBlock);
            }

            ReleaseTexture(ref _gridStateTexture);
            ReleaseTexture(ref _gridBuffTexture);
            _gridStatePixels = null;
            _gridBuffPixels = null;
            _occupancyIndex.Clear();
            _chunkStateCache.Clear();
            _territoryChangeTracker.Clear();
            _hasVisibleRange = false;
            _hasGridOrigin = false;
            _hasCellSize = false;
            _visibleCellDataEnabled = false;
            _stateCellDataEnabled = false;
            _baseStateDirty = true;
            _previewDirty = true;
            _buffDirty = true;
        }

        private static void ReleaseTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Object.Destroy(texture);
            }
            else
            {
                Object.DestroyImmediate(texture);
            }

            texture = null;
        }
    }
}
