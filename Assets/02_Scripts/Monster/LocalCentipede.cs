using System.Collections.Generic;
using UnityEngine;

public class LocalCentipede : LocalWorldMonster
{
    [SerializeField] protected Transform head;
    [SerializeField] protected GameObject segmentPrefab;
    [SerializeField] protected int segmentCount = 5;
    [SerializeField] protected float segmentAmplitude = 1.0f;
    [SerializeField] protected float segmentFrequency = 1.0f;
    [SerializeField] protected float moveCountThreshold = 10f;

    List<Transform> segments = new();
    List<Vector3> originalSegmentPositions = new();
    List<Vector3> segmentPositions = new();
    int moveCount = 0;
    Vector3 originalPosition;
    float duration;
    float elapsedTime;

    public override void Initialize()
    {
        base.Initialize();
        InitializeSegments();
    }

    void InitializeSegments()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segment.name = $"Segment_{i}";
            Transform segmentTransform = segment.transform;
            segmentTransform.position = head.position;
            segments.Add(segmentTransform);
            originalSegmentPositions.Add(segmentTransform.position);
            segmentPositions.Add(segmentTransform.position);
        }
    }

    public override void UpdateMonster()
    {
        base.UpdateMonster();
        FollowSegments();
    }

    protected override void Patrol()
    {
        if (isPatrolling == false)
        {
            var randomTargetPosition = patrolPivotPosition + Random.insideUnitSphere * patrolRadius;
            if (!territory.IsPointInPolygon(randomTargetPosition))
            {
                originalPosition = transform.position;
                patrolTargetPosition = randomTargetPosition;
                patrolTargetPosition.y = transform.position.y;
                elapsedTime = 0;
                duration = Vector3.Distance(transform.position, randomTargetPosition) / movementSpeed;
                isPatrolling = true;
            }
        }
        else
        {
            Debug.Log($"{elapsedTime}, {duration}");
            elapsedTime += Time.deltaTime;
            Vector3 direction = (patrolTargetPosition - transform.position).normalized;
            var verticalDirection = Quaternion.AngleAxis(90f, Vector3.up) * direction;
            transform.position = Vector3.Lerp(originalPosition, patrolTargetPosition, Mathf.Min(elapsedTime / duration, 1)) +
                segmentAmplitude * Mathf.Sin(elapsedTime * segmentFrequency) * verticalDirection;

            if (elapsedTime >= duration)
            {
                isPatrolling = false; // 목표 위치에 도달하면 다시 순찰 시작
            }
        }
    }

    protected virtual void FollowSegments()
    {
        moveCount++;
        if (moveCount > moveCountThreshold)
        {
            moveCount = 0;
            Vector3 targetPosition = head.position;
            for (int i = 0; i < segmentCount; i++)
            {
                originalSegmentPositions[i] = segmentPositions[i];
            }
            for (int i = segmentCount - 1; i >= 0; i--)
            {
                if (i == 0)
                {
                    segmentPositions[i] = targetPosition;
                }
                else
                {
                    segmentPositions[i] = segmentPositions[i - 1];
                }
            }
        }
        for (int i = 0; i < segmentCount; i++)
        {
            Transform segment = segments[i];
            Vector3 targetPosition = segmentPositions[i];
            segment.position = Vector3.Lerp(originalSegmentPositions[i], targetPosition, moveCount / moveCountThreshold);
        }
    }
}