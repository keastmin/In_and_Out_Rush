using System.Collections.Generic;
using UnityEngine;

public class InfiniteGridVisualController
{
    private const int MaxOccupiedCells = 512;
    private const int MaxPreviewCells = 512;
    private const int MaxBuffCells = 512;
    private const int MaxTerritoryVertices = 256;

    private Renderer _groundRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private readonly Vector4[] _occupiedCellsBuffer = new Vector4[MaxOccupiedCells];
    private readonly Vector4[] _previewCellsBuffer = new Vector4[MaxPreviewCells];
    private readonly Vector4[] _buffCellsBuffer = new Vector4[MaxBuffCells];
    private readonly Vector4[] _buffColorsBuffer = new Vector4[MaxBuffCells];
    private readonly Vector4[] _territoryVerticesBuffer = new Vector4[MaxTerritoryVertices];

    public void Apply(
        GameObject owner,
        Transform ownerTransform,
        InfiniteGridLayoutSettings layout,
        InfiniteGridGuideSettings guide,
        InfiniteGridRenderingSettings rendering,
        GridCalculator gridCalculator,
        IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid,
        IEnumerable<Vector2Int> previewIndices,
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
        _propertyBlock.SetColor("_PreviewGuideColor", guide.PreviewGuideColor);
        ApplyOccupiedCells(gridCalculator, networkGrid);
        ApplyPreviewCells(gridCalculator, previewIndices);
        ApplyBuffCells(gridCalculator, buffCellRefCount, buffCellColorSum);
        ApplyTerritoryVertices(territory);
        _groundRenderer.SetPropertyBlock(_propertyBlock);
    }

    private void ApplyOccupiedCells(GridCalculator gridCalculator, IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid)
    {
        int occupiedCount = 0;

        if (networkGrid != null)
        {
            var occupiedIndices = new HashSet<Vector2Int>();

            foreach (var pair in networkGrid)
            {
                List<Vector2Int> indicesInRange = gridCalculator.GetInRangeIndices(pair.Key, pair.Value.ActiveRange);
                for (int i = 0; i < indicesInRange.Count; i++)
                {
                    occupiedIndices.Add(indicesInRange[i]);
                }
            }

            foreach (Vector2Int occupiedIndex in occupiedIndices)
            {
                if (occupiedCount >= MaxOccupiedCells)
                {
                    break;
                }

                Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(occupiedIndex);
                _occupiedCellsBuffer[occupiedCount] = new Vector4(axial.x, axial.y, 0f, 0f);
                occupiedCount++;
            }
        }

        _propertyBlock.SetFloat("_OccupiedCellCount", occupiedCount);
        _propertyBlock.SetVectorArray("_OccupiedCells", _occupiedCellsBuffer);
    }

    private void ApplyPreviewCells(GridCalculator gridCalculator, IEnumerable<Vector2Int> previewIndices)
    {
        int previewCount = 0;

        if (previewIndices != null)
        {
            foreach (Vector2Int previewIndex in previewIndices)
            {
                if (previewCount >= MaxPreviewCells)
                {
                    break;
                }

                Vector2Int axial = gridCalculator.GetAxialFromOffsetIndex(previewIndex);
                _previewCellsBuffer[previewCount] = new Vector4(axial.x, axial.y, 0f, 0f);
                previewCount++;
            }
        }

        _propertyBlock.SetFloat("_PreviewCellCount", previewCount);
        _propertyBlock.SetVectorArray("_PreviewCells", _previewCellsBuffer);
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
                _buffCellsBuffer[buffCount] = new Vector4(axial.x, axial.y, 0f, 0f);

                Color sumColor = buffCellColorSum != null && buffCellColorSum.TryGetValue(pair.Key, out Color cachedColor)
                    ? cachedColor
                    : Color.clear;
                Color mixedColor = sumColor / Mathf.Max(1, pair.Value);
                _buffColorsBuffer[buffCount] = new Vector4(mixedColor.r, mixedColor.g, mixedColor.b, mixedColor.a);
                buffCount++;
            }
        }

        _propertyBlock.SetFloat("_BuffCellCount", buffCount);
        _propertyBlock.SetVectorArray("_BuffCells", _buffCellsBuffer);
        _propertyBlock.SetVectorArray("_BuffCellColors", _buffColorsBuffer);
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
                _territoryVerticesBuffer[i] = new Vector4(vertex.x, vertex.y, 0f, 0f);
                territoryVertexCount++;
            }
        }

        _propertyBlock.SetFloat("_TerritoryVertexCount", territoryVertexCount);
        _propertyBlock.SetVectorArray("_TerritoryVertices", _territoryVerticesBuffer);
    }
}
