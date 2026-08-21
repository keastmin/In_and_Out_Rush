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

    public Transform ProjectileTransform => transform;

    public override void Spawned()
    {
        _rigidbody = GetComponent<Rigidbody>();

        if (HasStateAuthority)
            MonsterProjectileRegistry.Register(this);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        MonsterProjectileRegistry.Unregister(this);
        base.Despawned(runner, hasState);
    }

    public void Initialize(
        Monster owner,
        Vector3 direction,
        float speed,
        float damage,
        float lifetime)
    {
        if (!HasStateAuthority)
            return;

        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _speed = Mathf.Max(0f, speed);
        _damage = Mathf.Max(0f, damage);
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

            if (_lifeTimer.Expired(Runner))
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

        PlayerRunner playerRunner = other.GetComponentInParent<PlayerRunner>();
        if (playerRunner != null)
            playerRunner.TakeDamage(_damage);

        Despawn();
    }

    private void ApplyVelocity()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.linearVelocity = _direction * _speed;
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
