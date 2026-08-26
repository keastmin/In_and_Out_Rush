#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections.Generic;
using Dev.Network;
using KIM.Dev;
using ProjectIO.Monsters;
using UnityEngine;

public class WorldMonster : Monster
{
    private const int MaxPatrolTargetAttempts = 32;
    private const float MaxSafeZonePathSampleDistance = 0.25f;
    private const int ResourcePathHitBufferSize = 64;

    [Header("World Monster Settings")]
    [SerializeField] protected float patrolMinimumRadius = 5f;
    [SerializeField] protected float patrolMaximumRadius = 10f;
    [SerializeField, Min(0f)] private float obstaclePathPadding = 0.25f;

    protected Vector3 patrolPivotPosition;
    protected float patrolRadius;
    protected Vector3 patrolTargetPosition;
    protected bool isPatrolling = false;

    private IReadOnlyList<WorldObstacle> worldObstacles;
    private WorldObstacleBoundsIndex worldObstacleBoundsIndex;
    private readonly RaycastHit[] resourcePathHitBuffer = new RaycastHit[ResourcePathHitBufferSize];
    private readonly Collider[] resourceOverlapBuffer = new Collider[ResourcePathHitBufferSize];
    private float obstaclePathClearance = -1f;

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

    public void SetWorldObstacles(IReadOnlyList<WorldObstacle> worldObstacles)
    {
        this.worldObstacles = worldObstacles;
    }

    public void SetWorldObstacleIndex(WorldObstacleBoundsIndex obstacleBoundsIndex)
    {
        worldObstacleBoundsIndex = obstacleBoundsIndex;
    }

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

            if (TrySelectPatrolTarget(out Vector3 targetPosition))
            {
                patrolTargetPosition = targetPosition;
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
        if (IsMovementPathBlocked(currentPosition, nextPosition))
        {
            isPatrolling = false;
            StopMovement();
            return;
        }

        SetMovementVelocity(direction * (moveDistance / deltaTime));

        if (moveDistance >= targetDistance)
            isPatrolling = false;
    }

    protected bool TrySelectPatrolTarget(out Vector3 targetPosition)
        => TrySelectPatrolTarget(out targetPosition, allowTerritoryExit: false);

    protected bool TrySelectPatrolTarget(out Vector3 targetPosition, bool allowTerritoryExit)
    {
        targetPosition = RigidbodyPosition;
        Vector2 patrolPivotPosition2d = new(patrolPivotPosition.x, patrolPivotPosition.z);

        for (int attempt = 0; attempt < MaxPatrolTargetAttempts; attempt++)
        {
            Vector2 candidatePosition2d = patrolPivotPosition2d + Random.insideUnitCircle * patrolRadius;
            if (allowTerritoryExit && IsPositionInRunnerSafeZone(candidatePosition2d))
                continue;

            Vector3 candidatePosition = new(candidatePosition2d.x, RigidbodyPosition.y, candidatePosition2d.y);
            if (IsMovementPathBlocked(RigidbodyPosition, candidatePosition, allowTerritoryExit))
                continue;

            targetPosition = candidatePosition;
            return true;
        }

        return false;
    }

    protected bool IsWorldObstaclePathBlocked(Vector3 startPosition, Vector3 endPosition)
    {
        if (worldObstacles == null || worldObstacles.Count == 0)
            return false;

        float clearance = ResolveObstaclePathClearance();
        if (worldObstacleBoundsIndex != null)
            return worldObstacleBoundsIndex.IsPathBlocked(startPosition, endPosition, clearance);

        for (int i = 0; i < worldObstacles.Count; i++)
        {
            WorldObstacle obstacle = worldObstacles[i];
            if (obstacle == null)
                continue;

            if (DoesSegmentOverlapBounds(startPosition, endPosition, obstacle.Bounds, clearance))
                return true;
        }

        return false;
    }

    protected bool IsMovementPathBlocked(
        Vector3 startPosition,
        Vector3 endPosition,
        bool allowTerritoryExit = false)
    {
        if (IsSafeZonePathBlocked(startPosition, endPosition, allowTerritoryExit))
            return true;

        float clearance = ResolveObstaclePathClearance();
        return IsWorldObstaclePathBlocked(startPosition, endPosition) ||
               IsResourcePathBlocked(startPosition, endPosition, clearance);
    }

    private bool IsSafeZonePathBlocked(
        Vector3 startPosition,
        Vector3 endPosition,
        bool allowTerritoryExit)
    {
        bool startsInsideTerritory = IsPositionInTerritory(startPosition);
        if (IsPositionInActiveSanctuary(startPosition) ||
            (!allowTerritoryExit && startsInsideTerritory))
        {
            return true;
        }

        bool hasExitedTerritory = !startsInsideTerritory;
        Vector3 pathOffset = endPosition - startPosition;
        pathOffset.y = 0f;
        int sampleCount = Mathf.Max(
            1,
            Mathf.CeilToInt(pathOffset.magnitude / MaxSafeZonePathSampleDistance));

        for (int sampleIndex = 1; sampleIndex <= sampleCount; sampleIndex++)
        {
            Vector3 samplePosition = Vector3.Lerp(startPosition, endPosition, (float)sampleIndex / sampleCount);
            if (IsPositionInActiveSanctuary(samplePosition))
                return true;

            bool isInsideTerritory = IsPositionInTerritory(samplePosition);
            if (!allowTerritoryExit)
            {
                if (isInsideTerritory)
                    return true;

                continue;
            }

            if (hasExitedTerritory && isInsideTerritory)
                return true;

            hasExitedTerritory |= !isInsideTerritory;
        }

        return allowTerritoryExit && !hasExitedTerritory;
    }

    private bool IsResourcePathBlocked(Vector3 startPosition, Vector3 endPosition, float clearance)
    {
        Vector3 pathOffset = endPosition - startPosition;
        pathOffset.y = 0f;
        float pathLength = pathOffset.magnitude;
        float safeClearance = Mathf.Max(0.01f, clearance);

        if (pathLength > Mathf.Epsilon)
        {
            int hitCount = Physics.SphereCastNonAlloc(
                startPosition,
                safeClearance,
                pathOffset / pathLength,
                resourcePathHitBuffer,
                pathLength,
                Physics.DefaultRaycastLayers,
                QueryTriggerInteraction.Ignore);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                if (IsResourceCollider(resourcePathHitBuffer[hitIndex].collider))
                    return true;
            }
        }

        int overlapCount = Physics.OverlapSphereNonAlloc(
            endPosition,
            safeClearance,
            resourceOverlapBuffer,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        for (int overlapIndex = 0; overlapIndex < overlapCount; overlapIndex++)
        {
            if (IsResourceCollider(resourceOverlapBuffer[overlapIndex]))
                return true;
        }

        return false;
    }

    private static bool IsResourceCollider(Collider collider)
        => collider != null && collider.GetComponentInParent<ResourceVisible>() != null;

    private float ResolveObstaclePathClearance()
    {
        if (obstaclePathClearance >= 0f)
            return obstaclePathClearance;

        float largestPlanarExtent = 0f;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            Collider collider = colliders[i];
            if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger)
                continue;

            Bounds bounds = collider.bounds;
            largestPlanarExtent = Mathf.Max(largestPlanarExtent, bounds.extents.x, bounds.extents.z);
        }

        obstaclePathClearance = largestPlanarExtent + Mathf.Max(0f, obstaclePathPadding);
        return obstaclePathClearance;
    }

    private static bool DoesSegmentOverlapBounds(
        Vector3 startPosition,
        Vector3 endPosition,
        Bounds bounds,
        float clearance)
    {
        Vector2 boundsMin = new(bounds.min.x - clearance, bounds.min.z - clearance);
        Vector2 boundsMax = new(bounds.max.x + clearance, bounds.max.z + clearance);
        Vector2 start = new(startPosition.x, startPosition.z);
        Vector2 direction = new(endPosition.x - startPosition.x, endPosition.z - startPosition.z);
        float minimumT = 0f;
        float maximumT = 1f;

        return ClipSegmentAxis(start.x, direction.x, boundsMin.x, boundsMax.x, ref minimumT, ref maximumT) &&
               ClipSegmentAxis(start.y, direction.y, boundsMin.y, boundsMax.y, ref minimumT, ref maximumT);
    }

    private static bool ClipSegmentAxis(
        float start,
        float direction,
        float min,
        float max,
        ref float minimumT,
        ref float maximumT)
    {
        if (Mathf.Abs(direction) <= Mathf.Epsilon)
            return start >= min && start <= max;

        float inverseDirection = 1f / direction;
        float enter = (min - start) * inverseDirection;
        float exit = (max - start) * inverseDirection;
        if (enter > exit)
            (enter, exit) = (exit, enter);

        minimumT = Mathf.Max(minimumT, enter);
        maximumT = Mathf.Min(maximumT, exit);
        return minimumT <= maximumT;
    }
}
