using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class InfiniteGridVisualController
    {
        private const int MaxOccupiedCells = 64;
        private const int MaxPreviewCells = 64;
        private const int MaxBlockedPreviewCells = 64;
        private const int MaxBuffCells = 64;
        private const int MaxTerritoryVertices = 64;

        private Renderer _groundRenderer;
        private MaterialPropertyBlock _propertyBlock;
        private readonly Color[] _occupiedCellsBuffer = new Color[MaxOccupiedCells];
        private readonly Color[] _previewCellsBuffer = new Color[MaxPreviewCells];
        private readonly Color[] _blockedPreviewCellsBuffer = new Color[MaxBlockedPreviewCells];
        private readonly Color[] _buffCellsBuffer = new Color[MaxBuffCells];
        private readonly Color[] _buffColorsBuffer = new Color[MaxBuffCells];
        private readonly Color[] _territoryVerticesBuffer = new Color[MaxTerritoryVertices];
        private Texture2D _occupiedCellsTexture;
        private Texture2D _previewCellsTexture;
        private Texture2D _blockedPreviewCellsTexture;
        private Texture2D _buffCellsTexture;
        private Texture2D _buffColorsTexture;
        private Texture2D _territoryVerticesTexture;

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
            if (layout == null || guide == null || rendering == null || gridCalculator == null)
            {
                return;
            }

            owner.TryGetComponent(out _groundRenderer);

            if (_groundRenderer == null)
            {
                return;
            }

            if (rendering.ApplyInfiniteGridMaterial &&
                rendering.InfiniteGridMaterial != null &&
                _groundRenderer.sharedMaterial != rendering.InfiniteGridMaterial)
            {
                _groundRenderer.sharedMaterial = rendering.InfiniteGridMaterial;
            }

            _propertyBlock ??= new MaterialPropertyBlock();
            _groundRenderer.GetPropertyBlock(_propertyBlock);
            _propertyBlock.SetVector("_GridOriginWS", layout.ResolveOrigin(ownerTransform));
            _propertyBlock.SetFloat("_HexSize", layout.CellSize);
            _propertyBlock.SetFloat("_CellFill", layout.CellFill);
            _propertyBlock.SetFloat("_StateOverlayEnabled", layout.ShowCellStateOverlay ? 1f : 0f);
            _propertyBlock.SetColor("_PrimaryGuideColor", guide.PrimaryGuideColor);
            _propertyBlock.SetColor("_SecondaryGuideColor", guide.SecondaryGuideColor);
            _propertyBlock.SetColor("_PreviewValidGuideColor", guide.PreviewValidGuideColor);
            _propertyBlock.SetColor("_PreviewBlockedGuideColor", guide.PreviewBlockedGuideColor);
            ApplyOccupiedCells(gridCalculator, networkGrid, additionalOccupiedIndices);
            ApplyPreviewCells(gridCalculator, previewIndices, blockedPreviewIndices);
            ApplyBuffCells(gridCalculator, buffCellRefCount, buffCellColorSum);
            ApplyTerritoryVertices(territory);
            _groundRenderer.SetPropertyBlock(_propertyBlock);
        }

        private void ApplyOccupiedCells(
            GridCalculator gridCalculator,
            IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid,
            IEnumerable<Vector2Int> additionalOccupiedIndices)
        {
            int occupiedCount = 0;
            var occupiedIndices = new HashSet<Vector2Int>();

            if (networkGrid != null)
            {
                foreach (var pair in networkGrid)
                {
                    List<Vector2Int> indicesInRange = gridCalculator.GetInRangeIndices(pair.Key, pair.Value.ActiveRange);
                    for (int i = 0; i < indicesInRange.Count; i++)
                    {
                        occupiedIndices.Add(indicesInRange[i]);
                    }
                }
            }

            if (additionalOccupiedIndices != null)
            {
                foreach (Vector2Int occupiedIndex in additionalOccupiedIndices)
                {
                    occupiedIndices.Add(occupiedIndex);
                }
            }

            foreach (Vector2Int occupiedIndex in occupiedIndices)
            {
                if (occupiedCount >= MaxOccupiedCells)
                {
                    break;
                }

                Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(occupiedIndex);
                _occupiedCellsBuffer[occupiedCount] = new Color(axial.x, axial.y, 0f, 0f);
                occupiedCount++;
            }

            _propertyBlock.SetFloat("_OccupiedCellCount", occupiedCount);
            SetDataTexture("_OccupiedCellsTex", _occupiedCellsBuffer, ref _occupiedCellsTexture, MaxOccupiedCells);
        }

        private void ApplyPreviewCells(
            GridCalculator gridCalculator,
            IEnumerable<Vector2Int> previewIndices,
            IEnumerable<Vector2Int> blockedPreviewIndices)
        {
            int previewCount = 0;
            int blockedPreviewCount = 0;

            if (previewIndices != null)
            {
                foreach (Vector2Int previewIndex in previewIndices)
                {
                    if (previewCount >= MaxPreviewCells)
                    {
                        break;
                    }

                    Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(previewIndex);
                    _previewCellsBuffer[previewCount] = new Color(axial.x, axial.y, 0f, 0f);
                    previewCount++;
                }
            }

            if (blockedPreviewIndices != null)
            {
                foreach (Vector2Int previewIndex in blockedPreviewIndices)
                {
                    if (blockedPreviewCount >= MaxBlockedPreviewCells)
                    {
                        break;
                    }

                    Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(previewIndex);
                    _blockedPreviewCellsBuffer[blockedPreviewCount] = new Color(axial.x, axial.y, 0f, 0f);
                    blockedPreviewCount++;
                }
            }

            _propertyBlock.SetFloat("_PreviewCellCount", previewCount);
            SetDataTexture("_PreviewCellsTex", _previewCellsBuffer, ref _previewCellsTexture, MaxPreviewCells);
            _propertyBlock.SetFloat("_BlockedPreviewCellCount", blockedPreviewCount);
            SetDataTexture("_BlockedPreviewCellsTex", _blockedPreviewCellsBuffer, ref _blockedPreviewCellsTexture, MaxBlockedPreviewCells);
        }

        private void ApplyBuffCells(
            GridCalculator gridCalculator,
            IReadOnlyDictionary<Vector2Int, int> buffCellRefCount,
            IReadOnlyDictionary<Vector2Int, Color> buffCellColorSum)
        {
            int buffCount = 0;

            if (buffCellRefCount != null)
            {
                foreach (KeyValuePair<Vector2Int, int> pair in buffCellRefCount)
                {
                    if (buffCount >= MaxBuffCells || pair.Value <= 0)
                    {
                        break;
                    }

                    Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(pair.Key);
                    _buffCellsBuffer[buffCount] = new Color(axial.x, axial.y, 0f, 0f);

                    Color sumColor = buffCellColorSum != null && buffCellColorSum.TryGetValue(pair.Key, out Color cachedColor)
                        ? cachedColor
                        : Color.clear;
                    Color mixedColor = sumColor / Mathf.Max(1, pair.Value);
                    _buffColorsBuffer[buffCount] = mixedColor;
                    buffCount++;
                }
            }

            _propertyBlock.SetFloat("_BuffCellCount", buffCount);
            SetDataTexture("_BuffCellsTex", _buffCellsBuffer, ref _buffCellsTexture, MaxBuffCells);
            SetDataTexture("_BuffCellColorsTex", _buffColorsBuffer, ref _buffColorsTexture, MaxBuffCells);
        }

        private void ApplyTerritoryVertices(Territory territory)
        {
            int territoryVertexCount = 0;

            if (territory != null && territory.Vertices != null)
            {
                int count = Mathf.Min(territory.Vertices.Count, MaxTerritoryVertices);
                for (int i = 0; i < count; i++)
                {
                    Vector2 vertex = territory.Vertices[i];
                    _territoryVerticesBuffer[i] = new Color(vertex.x, vertex.y, 0f, 0f);
                    territoryVertexCount++;
                }
            }

            _propertyBlock.SetFloat("_TerritoryVertexCount", territoryVertexCount);
            SetDataTexture("_TerritoryVerticesTex", _territoryVerticesBuffer, ref _territoryVerticesTexture, MaxTerritoryVertices);
        }

        private void SetDataTexture(string propertyName, Color[] data, ref Texture2D texture, int capacity)
        {
            if (texture == null || texture.width != capacity || texture.height != 1)
            {
                ReleaseTexture(ref texture);
                texture = new Texture2D(capacity, 1, GetDataTextureFormat(), false, true)
                {
                    name = propertyName,
                    filterMode = FilterMode.Point,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            texture.SetPixels(data);
            texture.Apply(false, false);
            _propertyBlock.SetTexture(propertyName, texture);
        }

        public void Release()
        {
            ReleaseTexture(ref _occupiedCellsTexture);
            ReleaseTexture(ref _previewCellsTexture);
            ReleaseTexture(ref _blockedPreviewCellsTexture);
            ReleaseTexture(ref _buffCellsTexture);
            ReleaseTexture(ref _buffColorsTexture);
            ReleaseTexture(ref _territoryVerticesTexture);
        }

        private static TextureFormat GetDataTextureFormat()
        {
            return SystemInfo.SupportsTextureFormat(TextureFormat.RGBAFloat)
                ? TextureFormat.RGBAFloat
                : TextureFormat.RGBAHalf;
        }

        private static void ReleaseTexture(ref Texture2D texture)
        {
            if (texture == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                UnityEngine.Object.Destroy(texture);
            }
            else
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }

            texture = null;
        }
    }
}