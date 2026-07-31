using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("ProjectIO/Sanctuary View")]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SanctuaryView : MonoBehaviour
{
    public enum SanctuaryState
    {
        Inactive,
        Active,
        Expired
    }

    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    [Header("Material Color")]
    [SerializeField] private Color _activeColor = new(0.25f, 1f, 0.55f, 1f);
    [SerializeField] private Color _inactiveColor = new(0.35f, 0.45f, 0.55f, 1f);

    private readonly Territory _territory = new();
    private MeshFilter _meshFilter;
    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _materialPropertyBlock;
    private float _activeDuration;
    private float _activeElapsedTime;

    public Territory Territory => _territory;
    public SanctuaryState State { get; private set; } = SanctuaryState.Inactive;
    public bool IsActive => State == SanctuaryState.Active;
    public bool IsExpired => State == SanctuaryState.Expired;

    public event Action<SanctuaryView> Activated;
    public event Action<SanctuaryView> Expired;

    private void Awake()
    {
        CacheComponents();
        ApplyStateColor();
    }

    public void Initialize(float activeDuration)
    {
        _activeDuration = Mathf.Max(0.01f, activeDuration);
        _activeElapsedTime = 0f;
        State = SanctuaryState.Inactive;
        ApplyStateColor();
    }

    public void SetVertices(List<Vector2> vertices)
    {
        _territory.Vertices.Clear();
        if (vertices != null)
            _territory.Vertices.AddRange(vertices);

        Mesh mesh = Territory.GenerateMesh(_territory.Vertices);
        if (mesh == null)
        {
            Debug.LogError("Sanctuary mesh generation failed.", this);
            return;
        }

        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        _meshFilter.mesh = mesh;
    }

    public bool IsPointInSanctuary(Vector3 worldPosition)
    {
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition);
        return _territory.IsPointInPolygon(new Vector2(localPosition.x, localPosition.z));
    }

    public bool TryActivate()
    {
        if (State != SanctuaryState.Inactive)
            return false;

        State = SanctuaryState.Active;
        _activeElapsedTime = 0f;
        ApplyStateColor();
        Activated?.Invoke(this);
        return true;
    }

    public void Tick(float deltaTime)
    {
        if (State != SanctuaryState.Active)
            return;

        _activeElapsedTime += Mathf.Max(0f, deltaTime);
        if (_activeElapsedTime < _activeDuration)
            return;

        State = SanctuaryState.Expired;
        ApplyStateColor();
        Expired?.Invoke(this);
    }

    private void CacheComponents()
    {
        if (_meshFilter == null)
            _meshFilter = GetComponent<MeshFilter>();

        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void ApplyStateColor()
    {
        CacheComponents();

        if (_meshRenderer == null)
            return;

        _materialPropertyBlock ??= new MaterialPropertyBlock();
        _meshRenderer.GetPropertyBlock(_materialPropertyBlock);

        Color color = IsActive ? _activeColor : _inactiveColor;
        _materialPropertyBlock.SetColor(BaseColorId, color);
        _materialPropertyBlock.SetColor(ColorId, color);
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    private void OnValidate()
    {
        ApplyStateColor();
    }
}
