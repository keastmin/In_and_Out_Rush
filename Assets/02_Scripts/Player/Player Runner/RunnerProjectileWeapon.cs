using Fusion;
using ProjectIO.RunnerWeapons;
using UnityEngine;

public abstract class RunnerProjectileWeapon : RunnerWeaponNetworkBehaviour
{
    [Header("Projectile")]
    [SerializeField] private RunnerProjectile _projectilePrefab;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private LayerMask _damageableMask = ~0;

    [Header("Statistics")]
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _projectileSpeed = 18f;
    [SerializeField] private float _projectileLifetime = 3f;
    [SerializeField] private float _maximumRange = 10f;

    protected override bool TryExecuteShot(
        PlayerRunner owner,
        Vector3 targetPosition,
        RunnerWeaponHand hand,
        out Vector3 shotDirection)
    {
        shotDirection = owner != null ? owner.transform.forward : Vector3.forward;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"{nameof(RunnerProjectileWeapon)} requires a projectile prefab.", this);
            return false;
        }

        Transform muzzle = ResolveMuzzle(hand);
        if (muzzle == null)
        {
            Debug.LogWarning($"{nameof(RunnerProjectileWeapon)} requires a muzzle transform.", this);
            return false;
        }

        shotDirection = GetFireDirection(owner, muzzle, targetPosition);

        RunnerProjectile projectile = owner.Runner.Spawn(
            _projectilePrefab,
            muzzle.position,
            Quaternion.LookRotation(shotDirection, Vector3.up));

        if (projectile == null)
            return false;

        float finalDamage = _damage * owner.WeaponDamage * owner.WeaponDamageScaler;
        projectile.Init(
            owner,
            shotDirection,
            _projectileSpeed,
            finalDamage,
            _projectileLifetime,
            _maximumRange,
            _damageableMask);
        return true;
    }

    protected virtual Transform ResolveMuzzle(RunnerWeaponHand hand)
    {
        return _muzzle;
    }

    private static Vector3 GetFireDirection(
        PlayerRunner owner,
        Transform muzzle,
        Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - muzzle.position;
        direction.y = 0f;

        if (!IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            direction = owner.transform.forward;

        direction.y = 0f;
        if (!IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }
}
