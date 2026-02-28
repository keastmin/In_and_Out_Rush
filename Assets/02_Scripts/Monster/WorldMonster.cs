#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class WorldMonster : Monster
{
    [SerializeField] protected float patrolMinimumRadius = 5f;
    [SerializeField] protected float patrolMaximumRadius = 10f;

    protected Vector3 patrolPivotPosition;
    protected float patrolRadius;
    protected Vector3 patrolTargetPosition;
    protected bool isPatrolling = false;

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(patrolPivotPosition, 0.1f);
        Gizmos.DrawWireSphere(patrolPivotPosition, patrolRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(patrolTargetPosition, 0.1f);
    }

    void OnDrawGizmos()
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
            var randomTargetPosition = patrolPivotPosition + Random.insideUnitSphere * patrolRadius;
            if (!territory.IsPointInPolygon(randomTargetPosition))
            {
                patrolTargetPosition = randomTargetPosition;
                patrolTargetPosition.y = transform.position.y;
                isPatrolling = true;
            }
        }
        else
        {
            Vector3 direction = (patrolTargetPosition - transform.position).normalized;
            transform.position = transform.position + Time.deltaTime * movementSpeed * direction;

            if (Vector3.Distance(transform.position, patrolTargetPosition) < arrivalThreshold)
            {
                isPatrolling = false; // 목표 위치에 도달하면 다시 순찰 시작
            }
        }
    }
}