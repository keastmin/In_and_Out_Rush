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
    [SerializeField] private float _attackSpeed = 1f;

    private bool _isChasing;
    private bool _isAttacking;
    private float _attackElapsedTime = 0f;

    public override void UpdateMonster()
    {
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
                if (IsTargetWithinRange(_attackRange))
                {
                    Attack();
                    return;
                }

                _isAttacking = false;
            }

            if (CanEnterAttackState())
            {
                _isAttacking = true;
                Attack();
                return;
            }

            Chase();
            if (!_isChasing)
                return;

            if (CanEnterAttackState())
            {
                _isAttacking = true;
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
            attackTargetTransform.GetComponent<IDamageable>()?.TakeDamage(1f);
            Debug.Log($"{name} attacks {attackTargetTransform.name}");
            _attackElapsedTime = 0f;
        }
    }

    public void StartChasing(Transform target)
    {
        StopMovement();
        attackTargetTransform = target;
        _isChasing = true;
        _isAttacking = false;
    }

    protected virtual void Chase()
    {
        var attackTargetPosition = attackTargetTransform.position;
        if (IsPositionInRunnerSafeZone(attackTargetPosition))
        {
            StopChasing();
            return;
        }

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        Vector3 currentPosition = RigidbodyPosition;
        Vector3 toTarget = attackTargetPosition - currentPosition;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        if (distance <= GetAttackRangeThreshold())
        {
            StopMovement();
            return;
        }

        float moveDistance = Mathf.Min(
            Mathf.Max(0f, movementSpeed) * deltaTime,
            Mathf.Max(0f, distance - GetAttackRangeThreshold()));

        if (moveDistance <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        Vector3 direction = toTarget / distance;
        Vector3 nextPosition = currentPosition + direction * moveDistance;
        if (IsPositionInRunnerSafeZone(nextPosition))
        {
            StopChasing();
            return;
        }

        if (IsWorldObstaclePathBlocked(currentPosition, nextPosition))
        {
            StopChasing();
            return;
        }

        SetMovementVelocity(direction * (moveDistance / deltaTime));
        FaceTarget();
    }

    private void StopChasing()
    {
        _isChasing = false;
        _isAttacking = false;
        attackTargetTransform = null;
        StopMovement();
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
            + $"Enter Range: {GetAttackRangeThreshold():F2} / Exit Range: {_attackRange:F2}\n"
            + $"Attack Timer: {_attackElapsedTime:F2}",
            style);
    }
#endif
}
