using Fusion;
using UnityEngine;

public class ShooterWorldMonster : WorldMonster
{
    [Header("Shooter Settings")]
    [SerializeField, Min(0f)] private float detectionRadius = 10f;
    [SerializeField, Min(0.01f)] private float fireInterval = 2f;
    [SerializeField, Min(0f)] private float projectileSpeed = 8f;
    [SerializeField, Min(0f)] private float projectileDamage = 10f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 5f;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MonsterProjectile projectilePrefab;

    private TickTimer _fireTimer;

    public override void Initialize()
    {
    }

    public override void UpdateMonster()
    {
        if (playerTransform == null || projectilePrefab == null || muzzle == null)
            return;

        Vector3 toPlayer = playerTransform.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > detectionRadius * detectionRadius)
            return;

        Vector3 direction = GetFireDirection();
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        if (!_fireTimer.ExpiredOrNotRunning(Runner))
            return;

        Fire(direction);
        _fireTimer = TickTimer.CreateFromSeconds(Runner, fireInterval);
    }

    private Vector3 GetFireDirection()
    {
        Vector3 direction = playerTransform.position - muzzle.position;
        direction.y = 0f;

        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        return direction.normalized;
    }

    private void Fire(Vector3 direction)
    {
        MonsterProjectile projectile = Runner.Spawn(
            projectilePrefab,
            muzzle.position,
            Quaternion.LookRotation(direction, Vector3.up));

        projectile.Initialize(
            this,
            direction,
            projectileSpeed,
            projectileDamage,
            projectileLifetime);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
