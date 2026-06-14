using System.Collections.Generic;
using Fusion;
using KIM.Dev;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(LineRenderer))]
public class BarrierWave : NetworkBehaviour
{
    private const float DefaultCellSize = 1.6f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float duration = 2f;
    [SerializeField, Min(0f)] private float movementSpeedInTiles = 3f;

    [Header("Ring")]
    [SerializeField, Min(0f)] private float initialDiameterInTiles = 1f;
    [SerializeField, Min(0f)] private float finalDiameterInTiles = 6f;
    [SerializeField, Min(0.01f)] private float ringThicknessInTiles = 0.2f;
    [SerializeField, Range(8, 128)] private int lineSegments = 48;

    [Networked] private float CurrentDiameter { get; set; }

    private readonly List<IBarrierDestructibleProjectile> _projectileBuffer = new();
    private LineRenderer _lineRenderer;
    private Vector3 _direction;
    private TickTimer _lifeTimer;
    private float _cellSize;
    private float _elapsedTime;
    private bool _initialized;

    public override void Spawned()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        ConfigureLineRenderer();

        if (!HasStateAuthority)
            return;

        _cellSize = ResolveCellSize();
        CurrentDiameter = initialDiameterInTiles * _cellSize;
    }

    public void Initialize(Vector3 direction)
    {
        if (!HasStateAuthority)
            return;

        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _direction.y = 0f;
        if (_direction.sqrMagnitude <= 0.0001f)
            _direction = Vector3.forward;

        _direction.Normalize();
        _cellSize = ResolveCellSize();
        _elapsedTime = 0f;
        CurrentDiameter = initialDiameterInTiles * _cellSize;
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, duration));
        _initialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_initialized)
            return;

        if (_lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        float deltaTime = Runner.DeltaTime;
        _elapsedTime += deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / Mathf.Max(0.01f, duration));

        transform.position += _direction * (movementSpeedInTiles * _cellSize * deltaTime);
        CurrentDiameter = Mathf.Lerp(
            initialDiameterInTiles * _cellSize,
            finalDiameterInTiles * _cellSize,
            progress);

        DestroyIntersectingProjectiles();
    }

    public override void Render()
    {
        DrawRing();
    }

    private void ConfigureLineRenderer()
    {
        if (_lineRenderer == null)
            return;

        _lineRenderer.useWorldSpace = false;
        _lineRenderer.loop = true;
        _lineRenderer.positionCount = lineSegments;
    }

    private void DrawRing()
    {
        if (_lineRenderer == null)
            _lineRenderer = GetComponent<LineRenderer>();

        if (_lineRenderer.positionCount != lineSegments)
            ConfigureLineRenderer();

        float radius = CurrentDiameter * 0.5f;
        for (int i = 0; i < lineSegments; i++)
        {
            float angle = (Mathf.PI * 2f * i) / lineSegments;
            _lineRenderer.SetPosition(i, new Vector3(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius,
                0f));
        }
    }

    private void DestroyIntersectingProjectiles()
    {
        MonsterProjectileRegistry.CopyActiveProjectilesTo(_projectileBuffer);

        float radius = CurrentDiameter * 0.5f;
        float halfThickness = ringThicknessInTiles * _cellSize * 0.5f;
        Vector3 ringNormal = transform.forward;

        for (int i = 0; i < _projectileBuffer.Count; i++)
        {
            IBarrierDestructibleProjectile projectile = _projectileBuffer[i];
            if (projectile?.ProjectileTransform == null)
                continue;

            Vector3 offset = projectile.ProjectileTransform.position - transform.position;
            float planeDistance = Mathf.Abs(Vector3.Dot(offset, ringNormal));
            if (planeDistance > halfThickness)
                continue;

            Vector3 projectedOffset = offset - Vector3.Dot(offset, ringNormal) * ringNormal;
            if (projectedOffset.magnitude <= radius + halfThickness)
                projectile.DestroyByBarrier();
        }
    }

    private static float ResolveCellSize()
    {
        return InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;
    }

    private void OnValidate()
    {
        finalDiameterInTiles = Mathf.Max(initialDiameterInTiles, finalDiameterInTiles);
    }
}
