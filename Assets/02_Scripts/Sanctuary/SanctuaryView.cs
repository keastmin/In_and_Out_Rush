using System;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("ProjectIO/Sanctuary View")]
[RequireComponent(typeof(MeshFilter))]
public class SanctuaryView : MonoBehaviour
{
    public enum SanctuaryState
    {
        Inactive,
        Active,
        Expired
    }

    private readonly Territory _territory = new();
    private MeshFilter _meshFilter;
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
        _meshFilter = GetComponent<MeshFilter>();
    }

    public void Initialize(float activeDuration)
    {
        _activeDuration = Mathf.Max(0.01f, activeDuration);
        _activeElapsedTime = 0f;
        State = SanctuaryState.Inactive;
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
        Expired?.Invoke(this);
    }
}
