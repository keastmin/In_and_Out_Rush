using System;
using System.Collections.Generic;
using Dev.Local;
using Dev.Network;
using Fusion;
using ProjectIO.Territory;
using Unity.Profiling;
using UnityEngine;

public class TerritorySystem : NetworkSystemBase
{
    private static readonly ProfilerMarker HandlePlayerPositionChangedMarker =
        new("TerritorySystem.HandlePlayerPositionChanged");
    private static readonly ProfilerMarker ExpandTerritoryMarker =
        new("TerritorySystem.ExpandTerritoryFromCurrentPath");
    private static readonly ProfilerMarker CalculateExpansionMarker =
        new("TerritorySystem.CalculateExpansion");
    private static readonly ProfilerMarker UpdateExpansionMeshMarker =
        new("TerritorySystem.UpdateExpansionMesh");
    private static readonly ProfilerMarker SyncExpansionMarker =
        new("TerritorySystem.SyncExpansionVertices");
    private static readonly ProfilerMarker NotifyExpansionConsumersMarker =
        new("TerritorySystem.NotifyExpansionConsumers");

    const int TerritoryVertexSyncChunkSize = 10;
    const int ExpansionPathSyncBatchSize = 8;
    const float ExpansionPathSyncInterval = 0.05f;
    const float MinExpansionMoveDistanceSqr = 0.01f;

    [Header("Initial Territory")]
    [SerializeField] int circlePointCount;
    [SerializeField] float circleRadius;

    [Header("Expanding")]
    [SerializeField] LineRenderer lineRenderer;
    readonly TerritoryExpansionSession expansionSession = new();
    readonly TerritoryExpansionReplication expansionReplication = new();
    readonly List<Vector2> pendingExpansionPathPoints = new(ExpansionPathSyncBatchSize);
    TerritoryTrailChunkRenderer trailChunkRenderer;
    TickTimer expansionPathSyncTimer;

    public Territory Territory;
    public TerritoryVisible TerritoryVisible;
    public bool IsExpanding => expansionSession.IsExpanding;
    public float ExpansionLineWidth => lineRenderer != null ? lineRenderer.widthMultiplier : 0f;

    public event Action<Territory, TerritorySystem> OnTerritoryExpandedEvent;

    void OnDrawGizmos()
    {
        if (Territory != null)
        {
            Gizmos.color = Color.green;
            foreach (var point in Territory.Vertices)
            { // vector2(x, y) -> vector3(x, 0, y)
                Gizmos.DrawSphere(new Vector3(point.x, 0, point.y), 0.5f);
            }
        }
    }

    public override void SetUp()
    {
        TerritoryVisible = Dev.Network.StageBootstrapper.Instance.TerritoryVisible;
        trailChunkRenderer = gameObject.GetComponent<TerritoryTrailChunkRenderer>();
        if (trailChunkRenderer == null)
            trailChunkRenderer = gameObject.AddComponent<TerritoryTrailChunkRenderer>();
        trailChunkRenderer.Initialize(lineRenderer);

        GenerateInitialTerritory();

        Dev.Network.StageBootstrapper.Instance.PlayerRunner.OnPositionChanged += HandlePlayerPositionChanged;
    }

    void GenerateInitialTerritory()
    {
        var vertices = GenerateCircleTerritory();

        CreateTerritory(vertices);
        TerritoryVisible.name = $"{Runner.name} - Territory";
        TerritoryVisible.SetVertices(vertices);
    }

    void CreateTerritory(List<Vector2> vertices)
    {
        Territory = new Territory();
        Territory.ReplaceVertices(vertices);
    }

    List<Vector2> GenerateCircleTerritory()
    {
        var polygonPoints = new List<Vector2>();

        var twoPI = Mathf.PI * 2;
        var partOfAngle = twoPI / circlePointCount;

        for (int i = 0; i < circlePointCount; i++)
        {
            var angle = (circlePointCount - 1 - i) * partOfAngle;
            var x = circleRadius * Mathf.Cos(angle);
            var y = circleRadius * Mathf.Sin(angle);
            var point = new Vector2(x, y);
            polygonPoints.Add(point);
        }

        return polygonPoints;
    }

    private void StartExpanding()
    {
        expansionSession.Begin();
        trailChunkRenderer?.Begin();
    }

    private void StopExpanding()
    {
        expansionSession.Stop();
        pendingExpansionPathPoints.Clear();
        expansionPathSyncTimer = default;
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
        trailChunkRenderer?.Clear();
    }

    private void ResetExpansionState(Vector2 safePosition)
    {
        StopExpanding();
        expansionSession.Reset(safePosition);
    }

    private void StartLifelineRecovery(Vector2 safePosition)
    {
        ResetExpansionState(safePosition);
        expansionSession.BeginLifelineRecovery(safePosition);
    }

    private void AddExpandingPathPoint(Vector2 point, bool forceCalculationPoint = false)
    {
        expansionSession.AppendPathPoint(point, forceCalculationPoint);
        trailChunkRenderer?.Append(point);
    }

    private void QueueExpansionPathPointForReplication(Vector2 point)
    {
        pendingExpansionPathPoints.Add(point);
    }

    private void FlushExpansionPathPointsIfDue()
    {
        if (pendingExpansionPathPoints.Count >= ExpansionPathSyncBatchSize ||
            expansionPathSyncTimer.ExpiredOrNotRunning(Runner))
        {
            FlushExpansionPathPoints();
        }
    }

    private void FlushExpansionPathPoints()
    {
        if (pendingExpansionPathPoints.Count == 0)
            return;

        RPC_AddExpandingPathPoints(pendingExpansionPathPoints.ToArray());
        pendingExpansionPathPoints.Clear();
        expansionPathSyncTimer = TickTimer.CreateFromSeconds(Runner, ExpansionPathSyncInterval);
    }

    public bool TryGetCurrentExpansionPath(List<Vector3> results)
    {
        if (results == null)
            return false;

        return expansionSession.TryCopyPathTo(results);
    }

    public void HandlePlayerPositionChanged(Vector3 position, PlayerRunner playerRunner, object sender) // 러너만
    {
        using (HandlePlayerPositionChangedMarker.Auto())
        {
        var currentPosition = new Vector2(position.x, position.z);

        if (!Object.HasStateAuthority)
            return;

        bool isInTerritory = Territory.IsPointInPolygon(currentPosition);

        if (expansionSession.IsRecoveringFromLifeline)
        {
            expansionSession.TryFinishLifelineRecovery(isInTerritory, currentPosition);
            return;
        }

        if (isInTerritory)
        {
            if (expansionSession.IsExpanding)
            {
                if (expansionSession.PlayerPathCount > 1)
                {
                    AddExpandingPathPoint(currentPosition, true);
                    QueueExpansionPathPointForReplication(currentPosition);
                    FlushExpansionPathPoints();
                    if (Object.HasStateAuthority)
                    {
                        ExpandTerritoryFromCurrentPath();
                    }
                }
                StopExpanding();
                RPC_StopExpanding();
                Debug.Log("다시 들어옴");
            }
            expansionSession.SetPreviousPosition(currentPosition);
        }
        else
        {
            if (!expansionSession.IsExpanding)
            {
                Debug.Log("나감");
                StartExpanding();
                RPC_StartExpanding();
                AddExpandingPathPoint(expansionSession.PreviousPosition, true);
                QueueExpansionPathPointForReplication(expansionSession.PreviousPosition);
                AddExpandingPathPoint(currentPosition, true);
                QueueExpansionPathPointForReplication(currentPosition);
                FlushExpansionPathPoints();
                expansionSession.SetPreviousPosition(currentPosition);
                return;
            }

            if (expansionSession.HasMovedEnough(currentPosition, MinExpansionMoveDistanceSqr))
            {
                bool shouldAddTurnPoint = true;

                // 러너가 자신이 지나온 길을 다시 밟으면 게임 오버
                if (CheckPlayerRunnerCrossedOwnPath(currentPosition, shouldAddTurnPoint, playerRunner))
                    return;

                if (shouldAddTurnPoint)
                {
                    AddExpandingPathPoint(currentPosition);
                    QueueExpansionPathPointForReplication(currentPosition);
                    FlushExpansionPathPointsIfDue();
                }

                expansionSession.SetPreviousPosition(currentPosition);
            }
        }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    public void RPC_StartExpanding()
    {
        StartExpanding();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    public void RPC_StopExpanding()
    {
        StopExpanding();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_ResetExpansionAfterLifeline(Vector2 safePosition)
    {
        StartLifelineRecovery(safePosition);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    public void RPC_AddExpandingPathPoints(Vector2[] points)
    {
        if (points == null)
            return;

        for (int i = 0; i < points.Length; i++)
            AddExpandingPathPoint(points[i]);
    }

    private void ExpandTerritoryFromCurrentPath()
    {
        using (ExpandTerritoryMarker.Auto())
        {
        Debug.Log($"{Runner.name} - Expanding territory with path: {expansionSession.CalculationPathCount}");

        TerritoryMeshData meshData;
        using (CalculateExpansionMarker.Auto())
        {
            if (!expansionSession.TryExpand(Territory, out meshData))
            {
                Debug.LogWarning($"{Runner.name} - Territory expansion rejected. Path point count: {expansionSession.CalculationPathCount}");
                return;
            }
        }

        using (UpdateExpansionMeshMarker.Auto())
            TerritoryVisible.SetMeshData(meshData);

        using (SyncExpansionMarker.Auto())
            SyncTerritoryVertices(Territory.Vertices);

        if (Object.HasStateAuthority)
        {
            using (NotifyExpansionConsumersMarker.Auto())
                OnTerritoryExpandedEvent?.Invoke(Territory, this); // 호스트만
        }
        }
    }

    private void SyncTerritoryVertices(List<Vector2> vertices)
    {
        if (!Object.HasStateAuthority || vertices == null || vertices.Count <= 0)
            return;

        RPC_BeginTerritoryVertices();

        for (int i = 0; i < vertices.Count; i += TerritoryVertexSyncChunkSize)
        {
            int chunkLength = Mathf.Min(TerritoryVertexSyncChunkSize, vertices.Count - i);
            var chunk = new Vector2[chunkLength];
            for (int j = 0; j < chunkLength; j++)
                chunk[j] = vertices[i + j];

            RPC_SyncTerritoryVertices(chunk);
        }

        RPC_FinishTerritoryVertices();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_BeginTerritoryVertices()
    {
        expansionReplication.BeginVertices();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_SyncTerritoryVertices(Vector2[] vertices)
    {
        expansionReplication.AppendVertices(vertices);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_FinishTerritoryVertices()
    {
        if (!expansionReplication.TryConsumeVertices(out List<Vector2> receivedVertices))
            return;

        Territory.ReplaceVertices(receivedVertices);
        TerritoryVisible.SetVertices(Territory.Vertices);
    }

    // 플레이어 러너가 이전 경로를 밟았는지 확인하고 밟았다면 게임 오버 처리
    private bool CheckPlayerRunnerCrossedOwnPath(Vector2 currPos, bool includesPendingTurnPoint, PlayerRunner playerRunner)
    {
        if (expansionSession.IsIntersected) { return true; }
        if (CheckCurrPathCrossPrevPath(currPos))
        {
            if (!Object.HasStateAuthority)
                return false;

            if (playerRunner != null && playerRunner.TryActivateLifeline(out Vector3 returnPosition))
            {
                Vector2 safePosition = new Vector2(returnPosition.x, returnPosition.z);
                StartLifelineRecovery(safePosition);
                RPC_ResetExpansionAfterLifeline(safePosition);
                Debug.Log("Lifeline activated. Player returned to laboratory.");
                return true;
            }

            // GameOver
            Debug.Log("Game Over! Player crossed own path.");
            expansionSession.MarkIntersected();
            playerRunner?.Kill();
            return true;
        }

        return false;
    }

    // 플레이어 러너의 현재 경로가 이전 경로와 교차했는지 확인
    private bool CheckCurrPathCrossPrevPath(Vector2 currPos)
    {
        return expansionSession.CrossesOwnPath(currPos);
        /*
        int count = playerPath.Count;

        if (count < 3)
        {
            if (!includesPendingTurnPoint || count < 2)
                return false;
        }

        Vector2 prevPos = previousPosition;
        if (Vector2.SqrMagnitude(currPos - prevPos) <= 0.0001f)
            return false;

        if (Application.isPlaying)
        {
            return trailSegmentIndex.Intersects(
                currPos,
                prevPos,
                playerPath[^2],
                playerPath[^1]);
        }

        int checkedSegmentCount = includesPendingTurnPoint ? count - 1 : count - 2;
        for (int i = 0; i < checkedSegmentCount; i++) // 현재 경로와 직전 인접 선분은 제외
        {
            Vector2 pos1 = playerPath[i];
            Vector2 pos2 = playerPath[i + 1];

            if (Geometry.SegmentIntersection(currPos, prevPos, pos1, pos2, true, out Vector2 intersection))
            {
                Debug.Log($"Intersection at: {intersection}");
                return true;
            }
        }

        // 마지막 선분
        return false;
        */
    }
}
