using Fusion;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
[RequireComponent(typeof(Rigidbody))]
public class RunnerProjectile : NetworkBehaviour
{
    private static readonly ProfilerMarker FixedUpdateMarker = new("RunnerProjectile.FixedUpdateNetwork");
    private const float MaximumRangeEpsilon = 0.001f;

    private Rigidbody _rigidbody;
    private PlayerRunner _owner;
    private Vector3 _direction;
    private float _speed;
    private float _damage;
    private float _maximumRange;
    private Vector3 _spawnPosition;
    private LayerMask _damageableMask;
    private TickTimer _lifeTimer;
    private bool _initialized;
    private bool _hitResolved;
    private bool _despawnAtMaximumRangeNextTick;

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
        float maximumRange,
        LayerMask damageableMask)
    {
        if (!HasStateAuthority) return;

        _owner = owner;
        _direction = direction.sqrMagnitude > 0.0001f ? direction.normalized : transform.forward;
        _speed = speed;
        _damage = damage;
        _maximumRange = Mathf.Max(0.01f, maximumRange);
        _spawnPosition = transform.position;
        _damageableMask = damageableMask;
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, lifetime));
        _hitResolved = false;
        _despawnAtMaximumRangeNextTick = false;
        _initialized = true;

        ApplyVelocity();
    }

    public override void FixedUpdateNetwork()
    {
        using (FixedUpdateMarker.Auto())
        {
            if (!HasStateAuthority || !_initialized) return;

            if (_despawnAtMaximumRangeNextTick)
            {
                DespawnAtMaximumRange();
                return;
            }

            if (_lifeTimer.Expired(Runner))
            {
                Runner.Despawn(Object);
                return;
            }

            if (HasReachedMaximumRange())
            {
                DespawnAtMaximumRange();
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
        if (!HasStateAuthority || !_initialized || _hitResolved || other == null) return;
        if (HasExceededMaximumRange())
        {
            _hitResolved = true;
            DespawnAtMaximumRange();
            return;
        }
        if (IsOwnerCollider(other)) return;
        if (!IsInDamageableMask(other.gameObject.layer)) return;

        WorldMonster worldMonster = other.GetComponentInParent<WorldMonster>();
        if (worldMonster == null) return;

        _hitResolved = true;
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

        float speed = Mathf.Max(0f, _speed);
        if (_initialized && Runner != null && Runner.DeltaTime > 0f)
        {
            float travelled = Vector3.Dot(transform.position - _spawnPosition, _direction);
            float remainingDistance = Mathf.Max(0f, _maximumRange - travelled);
            float maximumStepDistance = speed * Runner.DeltaTime;

            if (remainingDistance <= MaximumRangeEpsilon)
            {
                speed = 0f;
                _despawnAtMaximumRangeNextTick = true;
            }
            else if (maximumStepDistance >= remainingDistance)
            {
                speed = remainingDistance / Runner.DeltaTime;
                _despawnAtMaximumRangeNextTick = true;
            }
        }

        _rigidbody.linearVelocity = _direction * speed;
    }

    private bool HasReachedMaximumRange()
    {
        float travelled = Vector3.Dot(transform.position - _spawnPosition, _direction);
        return travelled >= _maximumRange - MaximumRangeEpsilon;
    }

    private bool HasExceededMaximumRange()
    {
        Vector3 offset = transform.position - _spawnPosition;
        return offset.sqrMagnitude > (_maximumRange * _maximumRange) + 0.0001f;
    }

    private void DespawnAtMaximumRange()
    {
        Vector3 maximumRangePosition = _spawnPosition + _direction * _maximumRange;

        if (_rigidbody == null)
            _rigidbody = GetComponent<Rigidbody>();

        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.position = maximumRangePosition;
        transform.position = maximumRangePosition;
        Runner.Despawn(Object);
    }
}
