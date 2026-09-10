#if UNITY_EDITOR
using UnityEditor;
#endif
using System;
using Dev;
using Dev.Network;
using Fusion.Addons.Physics;
using KIM.Dev;
using ProjectIO.Monsters;
using ProjectIO.Tracks;
using UnityEngine;

public class TrackMonster : Monster, IFogOfWarAlwaysVisible
{
    [SerializeField] private float _completionDamage = 10f;
    [SerializeField, Min(1f)] private float _outsideTerritorySpeedMultiplier = 1.5f;

    protected Track track;
    protected int currentPathIndex;
    protected int currentPointIndex;
    private int _priority;
    private bool _isInternalized;
    private int _spawnOrder;
    private TrackMonsterSpawnType _spawnType;
    private NetworkRigidbody3D _networkRigidbody;
    private bool _completionHandled;

    public int Priority => _priority;
    public bool IsInternalized => _isInternalized;
    public float CompletionDamage => _completionDamage;
    public int SpawnOrder => _spawnOrder;
    public TrackMonsterSpawnType SpawnType => _spawnType;
    public event Action<TrackMonster> OnDestroyed;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = Color.white;
        Handles.Label(transform.position + Vector3.up * 0.5f, name);
    }
#endif

    public void SetTrack(Track track) => this.track = track;

    public override void Initialize()
    {
        currentPathIndex = 0;
        currentPointIndex = 1;
        _completionHandled = false;
        TryGetComponent(out _networkRigidbody);
    }

    public override void ApplyStatMultiplier(float multiplier)
    {
        base.ApplyStatMultiplier(multiplier);
        if (!CanAccessNetworkState || !Object.HasStateAuthority)
            return;

        _completionDamage *= multiplier;
    }

    public void SetTrackMonsterPriority(int priority)
    {
        _priority = priority;
    }

    public void SetInternalized(bool isInternalized)
    {
        _isInternalized = isInternalized;
    }

    public void SetSpawnType(TrackMonsterSpawnType spawnType)
    {
        _spawnType = spawnType;
    }

    public void ApplyMovementSpeedMultiplier(float multiplier)
    {
        if (!CanAccessNetworkState || !Object.HasStateAuthority || multiplier <= 0f)
            return;

        movementSpeed *= multiplier;
    }

    public void SetSpawnOrder(int spawnOrder)
    {
        _spawnOrder = spawnOrder;
    }

    public override void UpdateMonster() => FollowTrack();

    protected virtual void FollowTrack()
    {
        if (territory == null && StageBootstrapper.Instance != null)
        {
            TerritorySystem territorySystem = StageBootstrapper.Instance.TerritorySystem;
            if (territorySystem != null && territorySystem.Territory != null)
            {
                territory = territorySystem.Territory;
            }
        }

        if (track == null ||
            track.Paths == null ||
            track.Paths.Count == 0 ||
            currentPathIndex < 0 ||
            currentPathIndex >= track.Paths.Count)
        {
            StopMovement();
            return;
        }

        TrackPath path = track.Paths[currentPathIndex];
        if (path == null || path.Vertices == null || path.Vertices.Length < 2)
        {
            StopMovement();
            return;
        }

        currentPointIndex = Mathf.Clamp(currentPointIndex, 1, path.Vertices.Length - 1);
        Vector3 target = path.Vertices[currentPointIndex];
        Vector3 moveDir = target - RigidbodyPosition;
        moveDir.y = 0f;
        float distance = moveDir.magnitude;

        if (distance < arrivalThreshold)
        {
            StopMovement();

            if (currentPointIndex == path.Vertices.Length - 1)
            {
                HandlePathCompleted();
                return;
            }

            currentPointIndex++;
            return;
        }

        float deltaTime = Runner.DeltaTime;
        if (deltaTime <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        float moveDistance = Mathf.Min(
            Mathf.Max(0f, EffectiveMovementSpeed) * deltaTime,
            distance);
        if (moveDistance <= Mathf.Epsilon)
        {
            StopMovement();
            return;
        }

        Vector3 direction = moveDir / distance;
        SetMovementVelocity(direction * (moveDistance / deltaTime));
        SetRigidbodyRotation(Quaternion.LookRotation(direction, Vector3.up));
    }

    private float EffectiveMovementSpeed
        => IsPositionOutsideTerritory(RigidbodyPosition)
            ? movementSpeed * Mathf.Max(1f, _outsideTerritorySpeedMultiplier)
            : movementSpeed;

    private void HandlePathCompleted()
    {
        bool hasNextPath = currentPathIndex + 1 < track.Paths.Count;
        bool shouldLoopAtFinalPath =
            track.Stage == TrackStage.PerpendicularLines &&
            IsElite;
        TrackTraversalAction action = TrackTraversalPolicy.Resolve(hasNextPath, shouldLoopAtFinalPath);

        if (action == TrackTraversalAction.TransferToNextPath)
        {
            TeleportToPathStart(currentPathIndex + 1);
            return;
        }

        if (action == TrackTraversalAction.LoopToFirstPath)
        {
            TeleportToPathStart(0);
            return;
        }

        CompleteTrack();
    }

    private bool IsElite =>
        _spawnType == TrackMonsterSpawnType.ElitePredator ||
        _spawnType == TrackMonsterSpawnType.EliteWalker;

    private void TeleportToPathStart(int pathIndex)
    {
        if (pathIndex < 0 || pathIndex >= track.Paths.Count)
        {
            CompleteTrack();
            return;
        }

        TrackPath targetPath = track.Paths[pathIndex];
        if (targetPath == null || targetPath.Vertices == null || targetPath.Vertices.Length < 2)
        {
            CompleteTrack();
            return;
        }

        if (_networkRigidbody == null)
        {
            TryGetComponent(out _networkRigidbody);
        }

        if (_networkRigidbody == null)
        {
            Debug.LogError($"{name} requires NetworkRigidbody3D for track path transfers.", this);
            StopMovement();
            return;
        }

        StopMovement();
        currentPathIndex = pathIndex;
        currentPointIndex = 1;
        _networkRigidbody.Teleport(targetPath.Vertices[0], transform.rotation);
    }

    private void CompleteTrack()
    {
        if (_completionHandled || !CanAccessNetworkState || !Object.HasStateAuthority)
            return;

        _completionHandled = true;

        var runner = StageBootstrapper.Instance != null ? StageBootstrapper.Instance.PlayerRunner : null;
        if (!_isInternalized && runner != null)
        {
            runner.TakeTrackCompletionDamage(_completionDamage);
            Debug.Log($"{name} completed the track and dealt {_completionDamage} damage to PlayerRunner.");
        }

        DestroyMonster();
    }

    public override void DestroyMonster()
    {
        OnDestroyed?.Invoke(this);
        base.DestroyMonster();
    }
}
