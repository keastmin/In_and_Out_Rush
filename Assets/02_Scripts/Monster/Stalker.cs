#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class Stalker : WorldMonster
{
    private const float AttackRangeThresholdRatio = 0.98f;

    [Header("Stalker Settings")]
    [SerializeField] private float _sensingRange = 5f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _chaseResumeRange = 2.2f;
    [SerializeField] private float _attackSpeed = 1f;
    [SerializeField, Min(0f)] private float _projectileSpeed = 8f;
    [SerializeField, Min(0f)] private float _projectileDamage = 1f;
    [SerializeField, Min(0.01f)] private float _projectileLifetime = 5f;
    [SerializeField] private Transform _muzzle;
    [SerializeField] private MonsterProjectile _projectilePrefab;

    private bool _isChasing;
    private bool _isAttacking;
    private bool _isStunLogged;
    private float _attackElapsedTime = 0f;
    private string _lastChaseStatus;

    public override void UpdateMonster()
    {
        if (_isStunLogged)
        {
            _isStunLogged = false;
            Debug.Log($"{name} Stalker stun ended: resuming AI update.");
        }

        if (_isChasing)
        {
            if (attackTargetTransform == null)
            {
                StopChasing();
                return;
            }

            if (IsTargetInRunnerSafeZone(attackTargetTransform))
            {
                StopChasing();
                return;
            }

            if (_isAttacking)
            {
                if (IsTargetWithinRange(GetChaseResumeRange()))
                {
                    Attack();
                    return;
                }

                Debug.Log(
                    $"{name} Stalker attack state -> chase: target left {GetChaseResumeRange():F2}m resume range.");
                _isAttacking = false;
            }

            if (CanEnterAttackState())
            {
                EnterAttackState();
                Attack();
                return;
            }

            Chase();
            if (!_isChasing)
                return;

            if (CanEnterAttackState())
            {
                EnterAttackState();
                Attack();
            }
        }
        else
        {
            base.UpdateMonster();

            if (playerTransform == null) { return; }
            if (IsTargetInRunnerSafeZone(playerTransform)) { return; }

            if (GetPlanarSqrDistance(playerTransform.position) < _sensingRange * _sensingRange)
            {
                StartChasing(playerTransform);
            }
        }
    }

    private bool CanEnterAttackState()
        => IsTargetWithinRange(GetAttackRangeThreshold());

    private bool IsTargetWithinRange(float range)
        => GetPlanarSqrDistance(attackTargetTransform.position) <= range * range;

    private float GetAttackRangeThreshold()
        => _attackRange * AttackRangeThresholdRatio;

    private float GetChaseResumeRange()
        => Mathf.Max(_attackRange, _chaseResumeRange);

    private float GetPlanarSqrDistance(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - RigidbodyPosition;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    private void Attack()
    {
        StopMovement();
        FaceTarget();
        _attackElapsedTime += Runner.DeltaTime * _attackSpeed;
        if (_attackElapsedTime >= 1f)
        {
            FireProjectile();
            _attackElapsedTime = 0f;
        }
    }

    private void FireProjectile()
    {
        if (_projectilePrefab == null || attackTargetTransform == null)
        {
            Debug.LogWarning(
                $"{name} Stalker projectile skipped: prefab={_projectilePrefab != null}, target={attackTargetTransform != null}.");
            return;
        }

        Vector3 muzzlePosition = GetMuzzlePosition();
        Vector3 direction = attackTargetTransform.position - muzzlePosition;
        direction.y = 0f;
        if (direction.sqrMagnitude <= 0.0001f)
            direction = transform.forward;

        direction.Normalize();
        MonsterProjectile projectile = Runner.Spawn(
            _projectilePrefab,
            muzzlePosition,
            Quaternion.LookRotation(direction, Vector3.up));

        projectile.Initialize(
            this,
            direction,
            _projectileSpeed,
            _projectileDamage,
            _projectileLifetime);

        // Debug.Log(
        //     $"{name} Stalker projectile fired: distance={Mathf.Sqrt(GetPlanarSqrDistance(attackTargetTransform.position)):F2}, "
        //     + $"muzzle={muzzlePosition}, projectile={projectile.name}.");
    }

    private Vector3 GetMuzzlePosition()
    {
        if (_muzzle != null)
            return _muzzle.position;

        return transform.TransformPoint(new Vector3(0f, 1f, 0.5f));
    }

    private void EnterAttackState()
    {
        _isAttacking = true;
        Debug.Log(
            $"{name} Stalker chase -> attack: distance={Mathf.Sqrt(GetPlanarSqrDistance(attackTargetTransform.position)):F2}, "
            + $"enterRange={GetAttackRangeThreshold():F2}, attackRange={_attackRange:F2}.");
    }

    public void StartChasing(Transform target)
    {
        StopMovement();
        attackTargetTransform = target;
        _isChasing = true;
        _isAttacking = false;
        Debug.Log($"{name} Stalker patrol -> chase: target={target.name}.");
    }

    protected virtual void Chase()
    {
        var attackTargetPosition = attackTargetTransform.position;
        if (IsPositionInRunnerSafeZone(attackTargetPosition))
        {
            LogChaseStatus("stopped: target entered runner safe zone");
            StopChasing();
            return;
        }

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            LogChaseStatus("waiting: simulation delta time is zero");
            StopMovement();
            return;
        }

        Vector3 currentPosition = RigidbodyPosition;
        Vector3 toTarget = attackTargetPosition - currentPosition;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance <= GetAttackRangeThreshold())
        {
            LogChaseStatus("at attack-entry range");
            StopMovement();
            return;
        }

        float moveDistance = Mathf.Min(
            Mathf.Max(0f, movementSpeed) * deltaTime,
            Mathf.Max(0f, distance - GetAttackRangeThreshold()));

        if (moveDistance <= Mathf.Epsilon)
        {
            LogChaseStatus("waiting: computed movement distance is zero");
            StopMovement();
            return;
        }

        Vector3 direction = toTarget / distance;
        Vector3 nextPosition = currentPosition + direction * moveDistance;
        if (IsPositionInRunnerSafeZone(nextPosition))
        {
            LogChaseStatus("stopped: next position enters runner safe zone");
            StopChasing();
            return;
        }

        if (IsMovementPathBlocked(currentPosition, nextPosition))
        {
            LogChaseStatus("stopped: movement path is blocked");
            StopChasing();
            return;
        }

        LogChaseStatus("moving");
        SetMovementVelocity(direction * (moveDistance / deltaTime));
        FaceTarget();
    }

    protected override void StopByStun()
    {
        if (!_isStunLogged)
        {
            _isStunLogged = true;
            Debug.Log($"{name} Stalker stunned: AI update and movement are paused.");
        }

        base.StopByStun();
    }

    private void StopChasing()
    {
        _isChasing = false;
        _isAttacking = false;
        attackTargetTransform = null;
        StopMovement();
    }

    private void LogChaseStatus(string status)
    {
        if (_lastChaseStatus == status)
            return;

        _lastChaseStatus = status;
        Debug.Log(
            $"{name} Stalker chase: {status}, distance={Mathf.Sqrt(GetPlanarSqrDistance(attackTargetTransform.position)):F2}, "
            + $"attackRange={GetAttackRangeThreshold():F2}, resumeRange={GetChaseResumeRange():F2}.");
    }

    private void FaceTarget()
    {
        Vector3 lookDirection = attackTargetTransform.position - RigidbodyPosition;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.0001f)
            SetRigidbodyRotation(Quaternion.LookRotation(lookDirection, Vector3.up));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _sensingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

#if UNITY_EDITOR
        DrawStateLabel();
#endif
    }

#if UNITY_EDITOR
    private void DrawStateLabel()
    {
        string state = _isAttacking ? "Attack" : _isChasing ? "Chase" : "Patrol";
        Color stateColor = _isAttacking ? Color.red : _isChasing ? Color.yellow : Color.green;
        string targetDistance = attackTargetTransform == null
            ? "None"
            : Mathf.Sqrt(GetPlanarSqrDistance(attackTargetTransform.position)).ToString("F2");

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = stateColor;

        Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"State: {state}\n"
            + $"Target Distance: {targetDistance}\n"
            + $"Attack Range: {GetAttackRangeThreshold():F2} / Chase Resume Range: {GetChaseResumeRange():F2}\n"
            + $"Attack Timer: {_attackElapsedTime:F2}",
            style);
    }
#endif
}
