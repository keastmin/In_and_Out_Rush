using System.Collections.Generic;
using UnityEngine;

public class InfiniteGridVisualController
{
    private const int MaxOccupiedCells = 512;

    private Renderer _groundRenderer;
    private MaterialPropertyBlock _propertyBlock;
    private readonly Vector4[] _occupiedCellsBuffer = new Vector4[MaxOccupiedCells];

    public void Apply(
        GameObject owner,
        Transform ownerTransform,
        InfiniteGridLayoutSettings layout,
        InfiniteGridGuideSettings guide,
        InfiniteGridRenderingSettings rendering,
        GridCalculator gridCalculator,
        IEnumerable<KeyValuePair<Vector2Int, CellData>> networkGrid)
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
        ApplyOccupiedCells(gridCalculator, networkGrid);
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
}
