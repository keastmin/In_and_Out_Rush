using System;
using System.Collections.Generic;
using Dev.Local;
using Dev.Network;
using Fusion;
using ProjectIO.Territory;
using UnityEngine;

public class TerritorySystem : NetworkSystemBase
{
    const int TerritoryVertexSyncChunkSize = 10;
    const float MinExpansionMoveDistanceSqr = 0.01f;
    const float CalculationPointDistanceSqr = 0.04f;
    const float CalculationTurnDotThreshold = 0.995f;
    const float CalculationTurnLateralDistance = 0.08f;

    [Header("Initial Territory")]
    [SerializeField] int circlePointCount;
    [SerializeField] float circleRadius;

    [Header("Expanding")]
    [SerializeField] LineRenderer lineRenderer;
    [SerializeField] bool isExpanding;
    [SerializeField] Vector2 previousPosition;
    [SerializeField] List<Vector2> playerPath;
    bool isIntersected = false;
    bool isRecoveringFromLifeline = false;
    readonly List<Vector2> temporaryTerritoryVertices = new();
    readonly List<Vector2> calculationPath = new();
    readonly TerritoryTrailSegmentIndex trailSegmentIndex = new();
    TerritoryTrailChunkRenderer trailChunkRenderer;

    public Territory Territory;
    public TerritoryVisible TerritoryVisible;
    public bool IsExpanding => isExpanding;
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
        Territory = new() { Vertices = vertices };
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
        isExpanding = true;
        playerPath.Clear();
        calculationPath.Clear();
        trailSegmentIndex.Clear();
        trailChunkRenderer?.Begin();
    }

    private void StopExpanding()
    {
        playerPath.Clear();
        calculationPath.Clear();
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
        trailSegmentIndex.Clear();
        trailChunkRenderer?.Clear();
        isExpanding = false;
    }

    private void ResetExpansionState(Vector2 safePosition)
    {
        StopExpanding();
        previousPosition = safePosition;
        isIntersected = false;
    }

    private void StartLifelineRecovery(Vector2 safePosition)
    {
        ResetExpansionState(safePosition);
        isRecoveringFromLifeline = true;
    }

    private void AddExpandingPathPoint(Vector2 point, bool forceCalculationPoint = false)
    {
        if (playerPath.Count > 0)
            trailSegmentIndex.Add(playerPath[^1], point);

        playerPath.Add(point);
        AddCalculationPathPoint(point, forceCalculationPoint);
        trailChunkRenderer?.Append(point);
    }

    private void AddCalculationPathPoint(Vector2 point, bool force)
    {
        int count = calculationPath.Count;
        if (force || count < 2)
        {
            calculationPath.Add(point);
            return;
        }

        Vector2 lastPoint = calculationPath[^1];
        if (Vector2.SqrMagnitude(point - lastPoint) >= CalculationPointDistanceSqr)
        {
            calculationPath.Add(point);
            return;
        }

        Vector2 previousPoint = calculationPath[^2];
        Vector2 previousSegment = lastPoint - previousPoint;
        Vector2 currentSegment = point - lastPoint;
        if (Vector2.SqrMagnitude(previousSegment) <= 0.0001f ||
            Vector2.SqrMagnitude(currentSegment) <= 0.0001f)
            return;

        float directionDot = Vector2.Dot(
            previousSegment.normalized,
            currentSegment.normalized);
        if (directionDot >= CalculationTurnDotThreshold)
            return;

        float lateralDistance = Mathf.Abs(
            previousSegment.x * (lastPoint.y - point.y) -
            (previousPoint.x - point.x) * previousSegment.y) /
            Mathf.Sqrt(Vector2.SqrMagnitude(previousSegment));
        if (lateralDistance >= CalculationTurnLateralDistance)
            calculationPath.Add(point);
    }

    public bool TryGetCurrentExpansionPath(List<Vector3> results)
    {
        if (results == null)
            return false;

        results.Clear();
        if (!isExpanding || playerPath.Count < 2)
            return false;

        for (int i = 0; i < playerPath.Count; i++)
        {
            Vector2 point = playerPath[i];
            results.Add(new Vector3(point.x, 0f, point.y));
        }

        return results.Count >= 2;
    }

    public void HandlePlayerPositionChanged(Vector3 position, PlayerRunner playerRunner, object sender) // 러너만
    {
        var currentPosition = new Vector2(position.x, position.z);

        if (!Object.HasStateAuthority)
            return;

        bool isInTerritory = Territory.IsPointInPolygon(currentPosition);

        if (isRecoveringFromLifeline)
        {
            if (isInTerritory)
            {
                ResetExpansionState(currentPosition);
                isRecoveringFromLifeline = false;
            }

            return;
        }

        if (isInTerritory)
        {
            if (isExpanding)
            {
                if (playerPath.Count > 1)
                {
                    AddExpandingPathPoint(currentPosition, true);
                    RPC_AddExpandingPathPoint(currentPosition);
                    if (Object.HasStateAuthority)
                    {
                        ExpandTerritoryFromCurrentPath();
                    }
                }
                StopExpanding();
                RPC_StopExpanding();
                Debug.Log("다시 들어옴");
            }
            previousPosition = currentPosition;
        }
        else
        {
            if (!isExpanding)
            {
                Debug.Log("나감");
                StartExpanding();
                RPC_StartExpanding();
                AddExpandingPathPoint(previousPosition, true);
                RPC_AddExpandingPathPoint(previousPosition);
                AddExpandingPathPoint(currentPosition, true);
                RPC_AddExpandingPathPoint(currentPosition);
                previousPosition = currentPosition;
                return;
            }

            if (Vector2.SqrMagnitude(currentPosition - previousPosition) > MinExpansionMoveDistanceSqr)
            {
                bool shouldAddTurnPoint = true;

                // 러너가 자신이 지나온 길을 다시 밟으면 게임 오버
                if (CheckPlayerRunnerCrossedOwnPath(currentPosition, shouldAddTurnPoint, playerRunner))
                    return;

                if (shouldAddTurnPoint)
                {
                    AddExpandingPathPoint(currentPosition);
                    RPC_AddExpandingPathPoint(currentPosition);
                }

                previousPosition = currentPosition;
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
    public void RPC_AddExpandingPathPoint(Vector2 point)
    {
        AddExpandingPathPoint(point);
    }

    private void ExpandTerritoryFromCurrentPath()
    {
        Debug.Log($"{Runner.name} - Expanding territory with path: {playerPath.Count}");

        if (!Territory.TryExpand(playerPath))
        {
            Debug.LogWarning($"{Runner.name} - Territory expansion rejected. Path point count: {playerPath.Count}");
            return;
        }

        TerritoryVisible.SetVertices(Territory.Vertices);
        SyncTerritoryVertices(Territory.Vertices);

        if (Object.HasStateAuthority)
        {
            OnTerritoryExpandedEvent?.Invoke(Territory, this); // 호스트만
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
        temporaryTerritoryVertices.Clear();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_SyncTerritoryVertices(Vector2[] vertices)
    {
        if (vertices == null || vertices.Length <= 0)
            return;

        temporaryTerritoryVertices.AddRange(vertices);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_FinishTerritoryVertices()
    {
        if (temporaryTerritoryVertices.Count <= 0)
            return;

        Territory.Vertices.Clear();
        Territory.Vertices.AddRange(temporaryTerritoryVertices);
        TerritoryVisible.SetVertices(Territory.Vertices);
        temporaryTerritoryVertices.Clear();
    }

    // 플레이어 러너가 이전 경로를 밟았는지 확인하고 밟았다면 게임 오버 처리
    private bool CheckPlayerRunnerCrossedOwnPath(Vector2 currPos, bool includesPendingTurnPoint, PlayerRunner playerRunner)
    {
        if (isIntersected) { return true; }
        if (CheckCurrPathCrossPrevPath(currPos, includesPendingTurnPoint))
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
            isIntersected = true;
            playerRunner?.Kill();
            return true;
        }

        return false;
    }

    // 플레이어 러너의 현재 경로가 이전 경로와 교차했는지 확인
    private bool CheckCurrPathCrossPrevPath(Vector2 currPos, bool includesPendingTurnPoint)
    {
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
    }
}
