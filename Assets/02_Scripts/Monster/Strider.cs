using Fusion;
using KIM.Dev;
using UnityEngine;

public sealed class Strider : WorldMonster
{
    private const float DefaultCellSize = 1.6f;
    private const int MaxDirectionSelectionAttempts = 16;
    private const float PositionEpsilonSqr = 0.0001f;

    private enum MovementState
    {
        Resting,
        Sliding,
    }

    [Header("Strider Settings")]
    [SerializeField, Min(0f)] private float _slideDistanceInTiles = 3f;
    [SerializeField, Min(0f)] private float _slideSpeedMultiplier = 1.5f;
    [SerializeField, Min(0f)] private float _restDuration = 1.5f;

    [Networked] private MovementState State { get; set; }
    [Networked] private Vector3 SlideTarget { get; set; }
    [Networked] private Vector3 SlideDirection { get; set; }
    [Networked] private TickTimer RestTimer { get; set; }

    private void Reset()
    {
        health = 2500f;
        movementSpeed = 3f;
    }

    public override void Initialize()
    {
        base.Initialize();

        if (!CanAccessNetworkState || !Object.HasStateAuthority)
            return;

        State = MovementState.Resting;
        SlideTarget = RigidbodyPosition;
        SlideDirection = Vector3.zero;
        RestTimer = default;
        isPatrolling = false;
        patrolTargetPosition = RigidbodyPosition;
        StopMovement();
    }

    public override void UpdateMonster()
    {
        if (!CanAccessNetworkState || !Object.HasStateAuthority)
            return;

        if (State == MovementState.Sliding)
        {
            UpdateSlide();
            return;
        }

        UpdateRest();
    }

    private void UpdateRest()
    {
        StopMovement();

        if (!RestTimer.ExpiredOrNotRunning(Runner))
            return;

        if (TryBeginSlide())
        {
            UpdateSlide();
            return;
        }

        BeginRest();
    }

    private bool TryBeginSlide()
    {
        float slideDistance = ResolveSlideDistance();
        if (slideDistance <= Mathf.Epsilon)
            return false;

        for (int attempt = 0; attempt < MaxDirectionSelectionAttempts; attempt++)
        {
            Vector2 randomDirection = Random.insideUnitCircle;
            if (randomDirection.sqrMagnitude <= Mathf.Epsilon)
                continue;

            randomDirection.Normalize();
            Vector3 direction = new(randomDirection.x, 0f, randomDirection.y);
            Vector3 targetPosition = RigidbodyPosition + direction * slideDistance;
            targetPosition.y = RigidbodyPosition.y;

            if (IsPositionInRunnerSafeZone(targetPosition))
                continue;

            if (IsWorldObstaclePathBlocked(RigidbodyPosition, targetPosition))
                continue;

            State = MovementState.Sliding;
            SlideDirection = direction;
            SlideTarget = targetPosition;
            isPatrolling = true;
            patrolTargetPosition = targetPosition;
            FaceSlideDirection();
            return true;
        }

        return false;
    }

    private void UpdateSlide()
    {
        float slideSpeed = Mathf.Max(0f, movementSpeed) * Mathf.Max(0f, _slideSpeedMultiplier);
        if (slideSpeed <= Mathf.Epsilon)
        {
            BeginRest();
            return;
        }

        FaceSlideDirection();

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            BeginRest();
            return;
        }

        Vector3 currentPosition = RigidbodyPosition;
        Vector3 nextPosition = Vector3.MoveTowards(
            currentPosition,
            SlideTarget,
            slideSpeed * deltaTime);

        if (IsPositionInRunnerSafeZone(nextPosition))
        {
            BeginRest();
            return;
        }

        if (IsWorldObstaclePathBlocked(currentPosition, nextPosition))
        {
            BeginRest();
            return;
        }

        Vector3 displacement = nextPosition - currentPosition;
        if (displacement.sqrMagnitude <= PositionEpsilonSqr)
        {
            BeginRest();
            return;
        }

        SetMovementVelocity(displacement / deltaTime);

        if ((SlideTarget - nextPosition).sqrMagnitude <= PositionEpsilonSqr)
            BeginRest(false);
    }

    private void BeginRest(bool stopMovement = true)
    {
        State = MovementState.Resting;
        SlideTarget = RigidbodyPosition;
        SlideDirection = Vector3.zero;
        isPatrolling = false;
        patrolTargetPosition = RigidbodyPosition;
        if (stopMovement)
            StopMovement();

        RestTimer = _restDuration > 0f
            ? TickTimer.CreateFromSeconds(Runner, _restDuration)
            : default;
    }

    private float ResolveSlideDistance()
    {
        float cellSize = InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;

        if (cellSize <= Mathf.Epsilon)
            cellSize = DefaultCellSize;

        return Mathf.Max(0f, _slideDistanceInTiles) * cellSize;
    }

    private void FaceSlideDirection()
    {
        if (SlideDirection.sqrMagnitude > PositionEpsilonSqr)
            SetRigidbodyRotation(Quaternion.LookRotation(SlideDirection, Vector3.up));
    }
}
