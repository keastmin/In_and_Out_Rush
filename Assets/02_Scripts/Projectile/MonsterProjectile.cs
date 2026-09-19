using Fusion;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class MonsterProjectile : NetworkBehaviour, IItemDestructibleProjectile
{
    private static readonly ProfilerMarker FixedUpdateMarker = new("MonsterProjectile.FixedUpdateNetwork");

    private Rigidbody _rigidbody;
    private Monster _owner;
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private TickTimer _lifeTimer;
    private bool _initialized;
    private bool _destroyRequested;
    private float _maximumRange;
    private Vector3 _spawnPosition;
    private Vector3 _originalScale;
    private bool _rangeEndsNextTick;
    private System.Func<Vector3, bool> _isProtectedPosition;
    private bool _ignoreSameOwnerProjectiles;
    [Networked] private float SizeMultiplier { get; set; }

    public Transform ProjectileTransform => transform;

    public override void Spawned()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _originalScale = transform.localScale;

        if (HasStateAuthority)
        {
            SizeMultiplier = 1f;
            _initialized = false;
            _destroyRequested = false;
            MonsterProjectileRegistry.Register(this);
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MonsterProjectileRegistry.Unregister(this);
        transform.localScale = _originalScale;
        _isProtectedPosition = null;
        base.Despawned(runner, hasState);
    }

    public void Initialize(
        Monster owner,
        Vector3 direction,
        float speed,
        float damage,
        float lifetime,
        float maximumRange = 0f,
        float sizeMultiplier = 1f,
        System.Func<Vector3, bool> isProtectedPosition = null,
        bool ignoreSameOwnerProjectiles = false)
    {
        if (!HasStateAuthority)
            return;

        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _speed = Mathf.Max(0f, speed);
        _damage = Mathf.Max(0f, damage);
        _maximumRange = Mathf.Max(0f, maximumRange);
        // Fusion places the Transform after instantiation. With autoSyncTransforms off,
        // the physics pose can still be at the prefab origin until the next physics step.
        // Align this body before the first range check; otherwise some directions start
        // with a positive travelled distance and immediately despawn.
        _rigidbody.position = transform.position;
        _rigidbody.rotation = transform.rotation;
        _spawnPosition = _rigidbody.position;
        _rangeEndsNextTick = false;
        _isProtectedPosition = isProtectedPosition;
        _ignoreSameOwnerProjectiles = ignoreSameOwnerProjectiles;
        SizeMultiplier = Mathf.Max(0.01f, sizeMultiplier);
        transform.localScale = _originalScale * SizeMultiplier;
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, lifetime));
        _initialized = true;

        ApplyVelocity();
    }

    public override void FixedUpdateNetwork()
    {
        using (FixedUpdateMarker.Auto())
        {
            if (!HasStateAuthority || !_initialized || _destroyRequested)
                return;

            if (_lifeTimer.Expired(Runner) || _rangeEndsNextTick)
            {
                Despawn();
                return;
            }

            ApplyVelocity();
        }
    }

    public void DestroyByItemEffect()
    {
        if (HasStateAuthority)
            Despawn();
    }

    public override void Render()
    {
        transform.localScale = _originalScale * (SizeMultiplier > 0f ? SizeMultiplier : 1f);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleCollision(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleCollision(collision.collider);
    }

    private void HandleCollision(Collider other)
    {
        if (!HasStateAuthority || !_initialized || _destroyRequested || other == null)
            return;

        if (_owner != null && other.GetComponentInParent<Monster>() == _owner)
            return;

        // Radial volleys share an origin and overlap until their straight paths separate.
        if (_ignoreSameOwnerProjectiles &&
            other.GetComponentInParent<MonsterProjectile>() is MonsterProjectile sibling &&
            ReferenceEquals(sibling._owner, _owner))
            return;

        PlayerRunner playerRunner = other.GetComponentInParent<PlayerRunner>();
        if (playerRunner != null &&
            (_isProtectedPosition == null || !_isProtectedPosition(playerRunner.transform.position)))
            playerRunner.TakeDamage(_damage);

        Despawn();
    }

    private void ApplyVelocity()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        float speed = _speed;
        if (_maximumRange > 0f && Runner.DeltaTime > 0f)
        {
            float travelled = Vector3.Dot(_rigidbody.position - _spawnPosition, _direction);
            float remaining = Mathf.Max(0f, _maximumRange - travelled);
            if (speed * Runner.DeltaTime >= remaining)
            {
                speed = remaining / Runner.DeltaTime;
                _rangeEndsNextTick = true;
            }
        }
        _rigidbody.linearVelocity = _direction * speed;
    }

    private void Despawn()
    {
        if (_destroyRequested || Object == null || !Object.IsValid)
            return;

        _destroyRequested = true;
        MonsterProjectileRegistry.Unregister(this);
        Runner.Despawn(Object);
    }
}
