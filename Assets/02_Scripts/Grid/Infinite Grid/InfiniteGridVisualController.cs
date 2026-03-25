using UnityEngine;

public class InfiniteGridVisualController
{
    private Renderer _groundRenderer;
    private MaterialPropertyBlock _propertyBlock;

    public void Apply(
        GameObject owner,
        Transform ownerTransform,
        InfiniteGridLayoutSettings layout,
        InfiniteGridGuideSettings guide,
        InfiniteGridRenderingSettings rendering)
    {
        if (layout == null || guide == null || rendering == null)
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
        _groundRenderer.SetPropertyBlock(_propertyBlock);
    }
}
