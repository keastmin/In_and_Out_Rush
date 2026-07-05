using System.Collections.Generic;
using Fusion;
using KIM.Dev;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(NetworkTransform))]
public sealed class IncineratorDrone : NetworkBehaviour
{
    private const float DefaultCellSize = 1.6f;
    private const int CollisionBufferSize = 64;

    [Header("Lifetime and Placement")]
    [SerializeField, Min(0.01f)] private float duration = 10f;
    [SerializeField] private Vector3 localOffset = new(0f, 2f, -0.5f);

    [Header("Flame")]
    [SerializeField] private Transform flameMuzzle;
    [SerializeField] private ParticleSystem flameParticle;
    [SerializeField, Min(0.01f)] private float coneRangeInTiles = 5f;
    [SerializeField, Range(1f, 89f)] private float coneHalfAngleDegrees = 20f;
    [SerializeField, Min(0f)] private float damagePerSecond = 10f;
    [SerializeField, Min(0.01f)] private float damageInterval = 0.2f;
    [SerializeField, Min(0f)] private float turnSpeedDegrees = 180f;
    [SerializeField] private LayerMask monsterLayerMask = 1 << 6;

    [Networked] private NetworkObject Owner { get; set; }
    [Networked] private Vector3 AimDirection { get; set; }

    private readonly Collider[] _targetBuffer = new Collider[CollisionBufferSize];
    private readonly HashSet<IDamageable> _damagedTargets = new();
    private readonly List<IItemDestructibleProjectile> _projectileBuffer = new();

    private TickTimer _lifeTimer;
    private TickTimer _damageTimer;
    private bool _initialized;
    private bool _despawnRequested;
    private float _cellSize;

    public Vector3 LocalOffset => localOffset;
    public bool IsActive => Object != null && Object.IsValid && _initialized && !_despawnRequested;

    public override void Spawned()
    {
        _cellSize = ResolveCellSize();
        PlayFlameParticle();
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
        PlayFlameParticle();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        StopFlameParticle();
        base.Despawned(runner, hasState);
    }

    private Vector3 FindDesiredDirection(PlayerRunner owner)
    {
        Vector3 origin = GetFlameOrigin();
        float range = GetConeRange();
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
        GetCone(out Vector3 origin, out Vector3 forward, out float range, out float minimumDot);
        int hitCount = Physics.OverlapSphereNonAlloc(
            origin,
            range,
            _targetBuffer,
            monsterLayerMask,
            QueryTriggerInteraction.Collide);

        _damagedTargets.Clear();
        float damage = damagePerSecond * damageInterval;

        for (int i = 0; i < hitCount; i++)
        {
            Collider hit = _targetBuffer[i];
            if (!IsInsideCone(hit, origin, forward, range, minimumDot))
                continue;

            IDamageable damageable = hit != null ? hit.GetComponentInParent<IDamageable>() : null;
            if (damageable != null && _damagedTargets.Add(damageable))
                damageable.TakeDamage(damage);
        }
    }

    private void DestroyIntersectingProjectiles()
    {
        MonsterProjectileRegistry.CopyActiveProjectilesTo(_projectileBuffer);
        GetCone(out Vector3 origin, out Vector3 forward, out float range, out float minimumDot);

        for (int i = 0; i < _projectileBuffer.Count; i++)
        {
            IItemDestructibleProjectile projectile = _projectileBuffer[i];
            Transform projectileTransform = projectile?.ProjectileTransform;
            if (projectileTransform == null)
                continue;

            if (IsInsideCone(projectileTransform.position, origin, forward, range, minimumDot))
                projectile.DestroyByItemEffect();
        }
    }

    private void GetCone(out Vector3 origin, out Vector3 forward, out float range, out float minimumDot)
    {
        origin = GetFlameOrigin();
        forward = GetFlameForward();
        range = GetConeRange();
        minimumDot = Mathf.Cos(Mathf.Clamp(coneHalfAngleDegrees, 0f, 89f) * Mathf.Deg2Rad);
    }

    private Vector3 GetFlameOrigin()
    {
        return flameMuzzle != null ? flameMuzzle.position : transform.position;
    }

    private Vector3 GetFlameForward()
    {
        Vector3 forward = flameMuzzle != null ? flameMuzzle.forward : transform.forward;
        if (forward.sqrMagnitude <= 0.0001f)
            forward = AimDirection;

        return FlattenDirection(forward);
    }

    private float GetConeRange()
    {
        return Mathf.Max(0.01f, coneRangeInTiles) * _cellSize;
    }

    private static bool IsInsideCone(Collider target, Vector3 origin, Vector3 forward, float range, float minimumDot)
    {
        if (target == null)
            return false;

        return IsInsideCone(target.bounds.center, origin, forward, range, minimumDot);
    }

    private static bool IsInsideCone(Vector3 point, Vector3 origin, Vector3 forward, float range, float minimumDot)
    {
        Vector3 offset = point - origin;
        offset.y = 0f;

        float distanceSquared = offset.sqrMagnitude;
        if (distanceSquared <= 0.0001f)
            return true;

        float rangeSquared = range * range;
        if (distanceSquared > rangeSquared)
            return false;

        Vector3 direction = offset / Mathf.Sqrt(distanceSquared);
        return Vector3.Dot(forward, direction) >= minimumDot;
    }

    private void PlayFlameParticle()
    {
        if (flameParticle != null && !flameParticle.isPlaying)
            flameParticle.Play(true);
    }

    private void StopFlameParticle()
    {
        if (flameParticle != null)
            flameParticle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
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

    private static float ResolveCellSize()
    {
        return InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;
    }
}
