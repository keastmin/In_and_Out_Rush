using System.Collections.Generic;
using UnityEngine;

public class Centipede : WorldMonster
{
    [Header("Centipede Settings")]
    public Transform head;
    public GameObject segmentPrefab;
    [SerializeField] protected int segmentCount = 5;
    [SerializeField] protected float segmentAmplitude = 1.0f;
    [SerializeField] protected float segmentFrequency = 1.0f;
    [SerializeField] protected float segmentSpacing = 1.0f; // 세그먼트 간 거리 (월드 단위)

    readonly List<Transform> segments = new();
    // 헤드가 지나온 경로 기록 (인덱스 0이 가장 오래된 위치, 마지막이 현재 위치)
    readonly List<Vector3> positionHistory = new();

    // 너무 촘촘하게 기록하면 리스트가 폭증하므로 최소 기록 거리 설정
    const float MinRecordDistance = 0.02f;

    public override void Spawned()
    {
        base.Spawned();
        InitializeSegments();
    }

    void InitializeSegments()
    {
        for (int i = 0; i < segmentCount; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segment.name = $"Segment_{i}";
            segments.Add(segment.transform);
        }
        positionHistory.Add(head.position);
    }

    Vector3 originalPosition;
    Vector3 progressivePosition;
    float elapsedTime;
    float distance;

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();
        Gizmos.color = Color.red;
        if (isPatrolling)
        {
            Gizmos.DrawLine(originalPosition + Vector3.up * 0.1f, progressivePosition + Vector3.up * 0.1f);
        }
    }
#endif

    protected override void Patrol()
    {
        if (isPatrolling == false)
        {
            var randomTargetPosition = patrolPivotPosition + Random.insideUnitSphere * patrolRadius;
            if (!territory.IsPointInPolygon(new Vector2(randomTargetPosition.x, randomTargetPosition.z)))
            {
                originalPosition = transform.position;
                progressivePosition = transform.position;
                patrolTargetPosition = randomTargetPosition;
                patrolTargetPosition.y = transform.position.y;
                elapsedTime = 0;
                distance = Vector3.Distance(transform.position, randomTargetPosition);
                isPatrolling = true;
                // Debug.Log("!!: " + distance);
            }
        }
        else
        {
            elapsedTime += Runner.DeltaTime; // Fusion 고정 틱 델타타임 사용
            Vector3 direction = (patrolTargetPosition - originalPosition).normalized;
            progressivePosition += movementSpeed * Runner.DeltaTime * direction;
            // Debug.Log("??: " + Vector3.Distance(progressivePosition, originalPosition));
            if (distance < Vector3.Distance(progressivePosition, originalPosition))
            {
                isPatrolling = false;
                return;
            }
            var verticalDirection = Quaternion.AngleAxis(90f, Vector3.up) * direction;
            var verticalMovement = segmentAmplitude * Mathf.Sin(elapsedTime * segmentFrequency) * verticalDirection;
            rigidBody.linearVelocity = progressivePosition + verticalMovement - transform.position;
            // TODO: 안닿게 하려면 길찾기 알고리즘이 필요함
            // if (territory.IsPointInPolygon(new Vector2(transform.position.x, transform.position.z)))
            // {
            //     isPatrolling = false;
            // }
        }
    }

    void LateUpdate() => FollowSegments();

    protected virtual void FollowSegments()
    {
        // 헤드가 일정 거리 이상 이동했을 때만 경로에 기록 (프레임률 무관)
        Vector3 headPos = head.position;
        if (Vector3.Distance(headPos, positionHistory[^1]) >= MinRecordDistance)
        {
            positionHistory.Add(headPos);
        }

        // 각 세그먼트를 경로상의 고정 거리 위치에 배치
        for (int i = 0; i < segments.Count; i++)
        {
            segments[i].position = GetPositionAlongHistory((i + 1) * segmentSpacing);
        }

        // 더 이상 필요 없는 오래된 경로 기록 정리
        TrimHistory(segmentCount * segmentSpacing);
    }

    // 헤드로부터 targetDistance만큼 경로를 거슬러 올라간 위치를 반환
    Vector3 GetPositionAlongHistory(float targetDistance)
    {
        float accumulated = 0f;
        for (int i = positionHistory.Count - 1; i > 0; i--)
        {
            float segDist = Vector3.Distance(positionHistory[i], positionHistory[i - 1]);
            if (accumulated + segDist >= targetDistance)
            {
                // 두 기록점 사이에서 선형 보간
                float t = (targetDistance - accumulated) / segDist;
                return Vector3.Lerp(positionHistory[i], positionHistory[i - 1], t);
            }
            accumulated += segDist;
        }
        // 히스토리가 아직 충분히 쌓이지 않은 경우 (초기 스폰 직후)
        return positionHistory[0];
    }

    // maxDistance 이상 멀어진 오래된 기록을 제거
    void TrimHistory(float maxDistance)
    {
        float accumulated = 0f;
        for (int i = positionHistory.Count - 1; i > 0; i--)
        {
            accumulated += Vector3.Distance(positionHistory[i], positionHistory[i - 1]);
            if (accumulated > maxDistance + segmentSpacing)
            {
                if (i > 1)
                    positionHistory.RemoveRange(0, i - 1);
                break;
            }
        }
    }
}