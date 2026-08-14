using Fusion;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class RunnerProjectile : NetworkBehaviour
{
    private static readonly ProfilerMarker FixedUpdateMarker = new("RunnerProjectile.FixedUpdateNetwork");

    private Rigidbody _rigidbody;
    private PlayerRunner _owner;
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private LayerMask _damageableMask;
    private TickTimer _lifeTimer;
    private bool _initialized;

    public override void Spawned()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void Init(
        PlayerRunner owner,
        Vector3 direction,
        float speed,
        float damage,
        float lifetime,
        LayerMask damageableMask)
    {
        if (!HasStateAuthority) return;

        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _speed = speed;
        _damage = damage;
        _damageableMask = damageableMask;
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, lifetime));
        _initialized = true;

        ApplyVelocity();
    }

    public override void FixedUpdateNetwork()
    {
        using (FixedUpdateMarker.Auto())
        {
            if (!HasStateAuthority || !_initialized) return;

            if (_lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            ApplyVelocity();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryHit(collision.collider);
    }

    private void TryHit(Collider other)
    {
        if (!HasStateAuthority || !_initialized || other == null) return;
        if (IsOwnerCollider(other)) return;
        if (!IsInDamageableMask(other.gameObject.layer)) return;

        WorldMonster worldMonster = other.GetComponentInParent<WorldMonster>();
        if (worldMonster == null) return;

        worldMonster.TakeDamage(_damage);
        Runner.Despawn(Object);
    }

    private bool IsOwnerCollider(Collider other)
    {
        if (_owner == null) return false;

        PlayerRunner hitRunner = other.GetComponentInParent<PlayerRunner>();
        return hitRunner == _owner;
    }

    private bool IsInDamageableMask(int layer)
    {
        return (_damageableMask.value & (1 << layer)) != 0;
    }

    private void ApplyVelocity()
    {
        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.linearVelocity = _direction * _speed;
    }
}
