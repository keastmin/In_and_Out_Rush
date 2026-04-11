using UnityEngine;

[System.Serializable]
public class InfiniteGridGuideSettings
{
    [Header("Guide Colors")]
    [SerializeField] private Color _primaryGuideColor = new(0.2f, 0.45f, 1f, 0.28f);
    [SerializeField] private Color _secondaryGuideColor = new(1f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color _previewGuideColor = new(0.1f, 1f, 0.2f, 0.45f);

    public Color PrimaryGuideColor => _primaryGuideColor;
    public Color SecondaryGuideColor => _secondaryGuideColor;
    public Color PreviewGuideColor => _previewGuideColor;
}
