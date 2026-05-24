using Fusion;
using UnityEngine;

public class RunnerProjectileWeapon : MonoBehaviour, IRunnerWeapon
{
    [Header("Projectile")]
    [SerializeField] private RunnerProjectile _projectilePrefab;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private LayerMask _damageableMask = ~0;

    [Header("Statistics")]
    [SerializeField] private float _damage = 1f;
    [SerializeField] private float _projectileSpeed = 18f;
    [SerializeField] private float _cooldown = 0.25f;
    [SerializeField] private float _projectileLifetime = 3f;

    private TickTimer _cooldownTimer;

    public void TryFire(PlayerRunner owner, Vector3 targetPosition)
    {
        if (owner == null || !owner.HasStateAuthority) return;
        if (!_cooldownTimer.ExpiredOrNotRunning(owner.Runner)) return;

        if (_projectilePrefab == null)
        {
            Debug.LogWarning($"{nameof(RunnerProjectileWeapon)} requires a projectile prefab.", this);
            return;
        }

        if (_muzzle == null)
        {
            Debug.LogWarning($"{nameof(RunnerProjectileWeapon)} requires a muzzle transform.", this);
            return;
        }

        Vector3 direction = GetFireDirection(owner, targetPosition);
        float attackSpeedScaler = Mathf.Max(owner.WeaponAttackSpeedScaler, 0.01f);
        float cooldownSeconds = Mathf.Max(0f, _cooldown / attackSpeedScaler);

        _cooldownTimer = TickTimer.CreateFromSeconds(owner.Runner, cooldownSeconds);

        RunnerProjectile projectile = owner.Runner.Spawn(
            _projectilePrefab,
            _muzzle.position,
            Quaternion.LookRotation(direction, Vector3.up));

        float finalDamage = _damage * owner.WeaponDamage * owner.WeaponDamageScaler;
        projectile.Init(owner, direction, _projectileSpeed, finalDamage, _projectileLifetime, _damageableMask);
    }

    private Vector3 GetFireDirection(PlayerRunner owner, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - _muzzle.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = owner.transform.forward;

        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            return Vector3.forward;

        return direction.normalized;
    }
}
