using UnityEngine;

[System.Serializable]
public class InfiniteGridGuideSettings
{
    [Header("Guide Colors")]
    [SerializeField] private Color _primaryGuideColor = new(0.2f, 0.45f, 1f, 0.28f);
    [SerializeField] private Color _secondaryGuideColor = new(1f, 0.2f, 0.2f, 0.3f);
    [SerializeField] private Color _previewValidGuideColor = new(0.1f, 1f, 0.2f, 0.35f);
    [SerializeField] private Color _previewBlockedGuideColor = new(1f, 0.55f, 0.1f, 0.42f);

    public Color PrimaryGuideColor => _primaryGuideColor;
    public Color SecondaryGuideColor => _secondaryGuideColor;
    public Color PreviewValidGuideColor => _previewValidGuideColor;
    public Color PreviewBlockedGuideColor => _previewBlockedGuideColor;
}
