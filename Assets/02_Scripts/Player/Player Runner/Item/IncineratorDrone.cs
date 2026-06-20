using System.Collections.Generic;
using Fusion;
using KIM.Dev;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
[RequireComponent(typeof(LineRenderer))]
public sealed class IncineratorDrone : NetworkBehaviour
{
    private const float DefaultCellSize = 1.6f;
    private const int CollisionBufferSize = 64;

    [Header("Lifetime and Placement")]
    [SerializeField, Min(0.01f)] private float duration = 10f;
    [SerializeField] private Vector3 localOffset = new(0f, 2f, -0.5f);

    [Header("Flame")]
    [SerializeField, Min(0.01f)] private float rangeInTiles = 5f;
    [SerializeField, Min(0.01f)] private float widthInTiles = 0.5f;
    [SerializeField, Min(0f)] private float damagePerSecond = 10f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.2f;
    [SerializeField, Min(0f)] private float turnSpeedDegrees = 180f;
    [SerializeField] private LayerMask monsterLayerMask = 1 << 6;

    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private Vector3 AimDirection { get; set; }

    private readonly Collider[] _targetBuffer = new Collider[CollisionBufferSize];
    private readonly HashSet<IDamageable> _damagedTargets = new();
    private readonly List<IItemDestructibleProjectile> _projectileBuffer = new();

    private LineRenderer _flameRenderer;
    private Material _runtimeMaterial;
    private TickTimer _lifeTimer;
    private TickTimer _damageTimer;
    private bool _initialized;
    private bool _despawnRequested;
    private float _cellSize;

    public Vector3 LocalOffset => localOffset;
    public bool IsActive => Object != null && Object.IsValid && _initialized && !_despawnRequested;

    public override void Spawned()
    {
        _flameRenderer = GetComponent<LineRenderer>();
        _cellSize = ResolveCellSize();
        ConfigureFlameRenderer();
    }

    public bool Initialize(PlayerRunner owner)
    {
        if (!HasStateAuthority || owner == null || owner.Object == null || !owner.Object.IsValid)
            return false;

        Owner = owner.Object;
        _cellSize = ResolveCellSize();

        Vector3 forward = FlattenDirection(owner.transform.forward);
        AimDirection = forward;
        transform.SetPositionAndRotation(
            owner.transform.TransformPoint(localOffset),
            Quaternion.LookRotation(forward, Vector3.up));

        _lifeTimer = TickTimer.CreateFromSeconds(Runner, duration);
        _damageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
        _initialized = true;
        return true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_initialized || _despawnRequested)
            return;

        if (Owner == null || !Owner.IsValid || _lifeTimer.Expired(Runner))
        {
            Despawn();
            return;
        }

        PlayerRunner owner = Owner.GetComponent<PlayerRunner>();
        if (owner == null)
        {
            Despawn();
            return;
        }

        _cellSize = ResolveCellSize();
        transform.position = owner.transform.TransformPoint(localOffset);

        Vector3 desiredDirection = FindDesiredDirection(owner);
        AimDirection = RotateDirection(AimDirection, desiredDirection, Runner.DeltaTime);
        transform.rotation = Quaternion.LookRotation(AimDirection, Vector3.up);

        if (_damageTimer.ExpiredOrNotRunning(Runner))
        {
            ApplyFlameDamage();
            DestroyIntersectingProjectiles();
            _damageTimer = TickTimer.CreateFromSeconds(Runner, damageInterval);
        }
    }

    public override void Render()
    {
        if (_flameRenderer == null)
            _flameRenderer = GetComponent<LineRenderer>();

        Vector3 direction = FlattenDirection(AimDirection);
        float range = rangeInTiles * ResolveCellSize();
        _flameRenderer.SetPosition(0, transform.position);
        _flameRenderer.SetPosition(1, transform.position + direction * range);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (_runtimeMaterial != null)
            Destroy(_runtimeMaterial);

        base.Despawned(runner, hasState);
    }

    private Vector3 FindDesiredDirection(PlayerRunner owner)
    {
        Vector3 origin = transform.position;
        float range = rangeInTiles * _cellSize;
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            range,
            _targetBuffer,
            monsterLayerMask,
            QueryTriggerInteraction.Collide);

        Transform nearestTarget = null;
        float nearestDistanceSquared = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            Collider candidate = _targetBuffer[i];
            if (candidate == null)
                continue;

            IDamageable damageable = candidate.GetComponentInParent<IDamageable>();
            if (damageable is not Component damageableComponent)
                continue;

            float distanceSquared = (damageableComponent.transform.position - origin).sqrMagnitude;
            if (distanceSquared >= nearestDistanceSquared)
                continue;

            nearestDistanceSquared = distanceSquared;
            nearestTarget = damageableComponent.transform;
        }

        Vector3 desired = nearestTarget != null
            ? nearestTarget.position - origin
            : owner.transform.forward;

        return FlattenDirection(desired);
    }

    private Vector3 RotateDirection(Vector3 current, Vector3 target, float deltaTime)
    {
        current = FlattenDirection(current);
        target = FlattenDirection(target);
        float maxRadians = turnSpeedDegrees * Mathf.Deg2Rad * deltaTime;
        return Vector3.RotateTowards(current, target, maxRadians, 0f).normalized;
    }

    private void ApplyFlameDamage()
    {
        GetBeam(out Vector3 start, out Vector3 end, out float radius);
        int hitCount = Physics.OverlapCapsuleNonAlloc(
            start,
            end,
            radius,
            _targetBuffer,
            monsterLayerMask,
            QueryTriggerInteraction.Collide);

        _damagedTargets.Clear();
        float damage = damagePerSecond * damageInterval;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _targetBuffer[i];
            IDamageable damageable = hit != null ? hit.GetComponentInParent<IDamageable>() : null;
            if (damageable != null && _damagedTargets.Add(damageable))
                damageable.TakeDamage(damage);
        }
    }

    private void DestroyIntersectingProjectiles()
    {
        MonsterProjectileRegistry.CopyActiveProjectilesTo(_projectileBuffer);
        GetBeam(out Vector3 start, out Vector3 end, out float radius);
        float radiusSquared = radius * radius;

        for (int i = 0; i < _projectileBuffer.Count; i++)
        {
            IItemDestructibleProjectile projectile = _projectileBuffer[i];
            Transform projectileTransform = projectile?.ProjectileTransform;
            if (projectileTransform == null)
                continue;

            if (DistanceToSegmentSquared(projectileTransform.position, start, end) <= radiusSquared)
                projectile.DestroyByItemEffect();
        }
    }

    private void GetBeam(out Vector3 start, out Vector3 end, out float radius)
    {
        start = transform.position;
        end = start + FlattenDirection(AimDirection) * (rangeInTiles * _cellSize);
        radius = widthInTiles * _cellSize * 0.5f;
    }

    private void ConfigureFlameRenderer()
    {
        _flameRenderer.useWorldSpace = true;
        _flameRenderer.positionCount = 2;
        _flameRenderer.startWidth = widthInTiles * _cellSize;
        _flameRenderer.endWidth = widthInTiles * _cellSize * 0.6f;
        _flameRenderer.startColor = new Color(1f, 0.75f, 0.1f, 0.95f);
        _flameRenderer.endColor = new Color(1f, 0.1f, 0f, 0.35f);

        if (_flameRenderer.sharedMaterial != null)
            return;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        if (shader != null)
        {
            _runtimeMaterial = new Material(shader)
            {
                color = new Color(1f, 0.35f, 0.05f, 1f)
            };
            _flameRenderer.sharedMaterial = _runtimeMaterial;
        }
    }

    private void Despawn()
    {
        if (_despawnRequested || Object == null || !Object.IsValid)
            return;

        _despawnRequested = true;
        Runner.Despawn(Object);
    }

    private static Vector3 FlattenDirection(Vector3 direction)
    {
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    private static float DistanceToSegmentSquared(Vector3 point, Vector3 start, Vector3 end)
    {
        Vector3 segment = end - start;
        float lengthSquared = segment.sqrMagnitude;
        if (lengthSquared <= 0.0001f)
            return (point - start).sqrMagnitude;

        float t = Mathf.Clamp01(Vector3.Dot(point - start, segment) / lengthSquared);
        Vector3 closestPoint = start + segment * t;
        return (point - closestPoint).sqrMagnitude;
    }

    private static float ResolveCellSize()
    {
        return InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;
    }
}
