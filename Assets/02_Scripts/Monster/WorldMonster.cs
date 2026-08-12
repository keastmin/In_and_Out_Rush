#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class WorldMonster : Monster
{
    [Header("World Monster Settings")]
    [SerializeField] protected float patrolMinimumRadius = 5f;
    [SerializeField] protected float patrolMaximumRadius = 10f;

    protected Vector3 patrolPivotPosition;
    protected float patrolRadius;
    protected Vector3 patrolTargetPosition;
    protected bool isPatrolling = false;

    protected override bool ShouldDestroyInsideTerritory => true;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(patrolPivotPosition, 0.1f);
        Gizmos.DrawWireSphere(patrolPivotPosition, patrolRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(patrolTargetPosition, 0.1f);
    }

    protected virtual void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawLine(transform.position + Vector3.up * 0.1f, patrolTargetPosition + Vector3.up * 0.1f);
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 0.5f, name);
    }
#endif

    public void SetPatrolPivotPosition(Vector3 position) => patrolPivotPosition = position;

    public override void Initialize()
    {
        patrolRadius = Random.Range(patrolMinimumRadius, patrolMaximumRadius);
    }

    public override void UpdateMonster() => Patrol();

    protected virtual void Patrol()
    {
        if (isPatrolling == false)
        {
            StopMovement();

            var patrolPivotPosition2d = new Vector2(patrolPivotPosition.x, patrolPivotPosition.z);
            var randomTargetPosition = patrolPivotPosition2d + Random.insideUnitCircle * patrolRadius;
            if (!IsPositionInRunnerSafeZone(randomTargetPosition))
            {
                patrolTargetPosition = new Vector3(randomTargetPosition.x, RigidbodyPosition.y, randomTargetPosition.y);
                isPatrolling = true;
            }

            return;
        }

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        Vector3 currentPosition = RigidbodyPosition;
        Vector3 toTarget = patrolTargetPosition - currentPosition;
        float distance = toTarget.magnitude;
        if (distance <= arrivalThreshold)
        {
            isPatrolling = false;
            StopMovement();
            return;
        }

        Vector3 direction = toTarget / distance;
        float targetDistance = Mathf.Max(0f, distance - arrivalThreshold);
        float moveDistance = Mathf.Min(
            Mathf.Max(0f, movementSpeed) * deltaTime,
            targetDistance);

        if (moveDistance <= Mathf.Epsilon)
        {
            isPatrolling = false;
            StopMovement();
            return;
        }

        Vector3 nextPosition = currentPosition + direction * moveDistance;
        if (IsPositionInRunnerSafeZone(nextPosition))
        {
            isPatrolling = false;
            StopMovement();
            return;
        }

        SetMovementVelocity(direction * (moveDistance / deltaTime));

        if (moveDistance >= targetDistance)
            isPatrolling = false;
    }
}
