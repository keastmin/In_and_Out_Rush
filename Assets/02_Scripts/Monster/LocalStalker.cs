#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class LocalStalker : LocalWorldMonster
{
    private const float AttackRangeThresholdRatio = 0.95f;

    [SerializeField] protected float sensingRange = 5f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackSpeed = 1f;

    protected bool isChasing;
    private bool isAttacking;
    float attackElapsedTime = 0f;

    public override void UpdateMonster()
    {
        if (isChasing)
        {
            if (attackTargetTransform == null)
            {
                isChasing = false;
                isAttacking = false;
                return;
            }

            if (isAttacking)
            {
                Attack();
                if (!IsTargetWithinRange(attackRange))
                {
                    isAttacking = false;
                }
            }
            else
            {
                Chase();
                if (CanEnterAttackState())
                {
                    isAttacking = true;
                }
            }
        }
        else
        {
            base.UpdateMonster();

            if (playerTransform == null) { return; }
            if (GetPlanarSqrDistance(playerTransform.position) < sensingRange * sensingRange)
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
        => attackRange * AttackRangeThresholdRatio;

    private float GetPlanarSqrDistance(Vector3 targetPosition)
    {
        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude;
    }

    private void Attack()
    {
        FaceTarget();
        attackElapsedTime += Time.deltaTime * attackSpeed;
        if (attackElapsedTime >= 1f)
        {
            // playerTransform.GetComponent<LocalRunner>().Health -= 1;
            Debug.Log($"{name} attacks {playerTransform.name}");
            attackElapsedTime = 0f;
        }
    }

    public void StartChasing(Transform target)
    {
        attackTargetTransform = target;
        isChasing = true;
        isAttacking = false;
    }

    protected virtual void Chase()
    {
        Vector3 toTarget = attackTargetTransform.position - transform.position;
        toTarget.y = 0f;

        float distance = toTarget.magnitude;
        float moveDistance = Mathf.Min(
            movementSpeed * Time.deltaTime,
            Mathf.Max(0f, distance - GetAttackRangeThreshold()));

        transform.position += toTarget.normalized * moveDistance;
        FaceTarget();
    }

    private void FaceTarget()
    {
        Vector3 lookDirection = attackTargetTransform.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.0001f)
            transform.rotation = Quaternion.LookRotation(lookDirection, Vector3.up);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, sensingRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

#if UNITY_EDITOR
        DrawStateLabel();
#endif
    }

#if UNITY_EDITOR
    private void DrawStateLabel()
    {
        string state = isAttacking ? "Attack" : isChasing ? "Chase" : "Patrol";
        Color stateColor = isAttacking ? Color.red : isChasing ? Color.yellow : Color.green;
        string targetDistance = attackTargetTransform == null
            ? "None"
            : Mathf.Sqrt(GetPlanarSqrDistance(attackTargetTransform.position)).ToString("F2");

        GUIStyle style = new GUIStyle(EditorStyles.boldLabel);
        style.normal.textColor = stateColor;

        Handles.Label(
            transform.position + Vector3.up * 2.5f,
            $"State: {state}\n"
            + $"Target Distance: {targetDistance}\n"
            + $"Enter Range: {GetAttackRangeThreshold():F2} / Exit Range: {attackRange:F2}\n"
            + $"Attack Timer: {attackElapsedTime:F2}",
            style);
    }
#endif
}
