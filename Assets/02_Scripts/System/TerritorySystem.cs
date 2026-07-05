using System;
using System.Collections.Generic;
using Dev.Local;
using Dev.Network;
using Fusion;
using UnityEngine;

public class TerritorySystem : NetworkSystemBase
{
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
    }

    private void StopExpanding()
    {
        playerPath.Clear();
        lineRenderer.positionCount = 0;
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

    private void AddExpandingPathPoint(Vector2 point)
    {
        playerPath.Add(point);
        lineRenderer.positionCount = playerPath.Count + 1;
        var converted = playerPath.ConvertAll(p => new Vector3(p.x, 0, p.y));
        converted.Add(new Vector3(point.x, 0, point.y));
        lineRenderer.SetPositions(converted.ToArray());
    }

    public bool TryGetCurrentExpansionPath(List<Vector3> results)
    {
        if (results == null)
            return false;

        results.Clear();
        if (!isExpanding || lineRenderer == null || lineRenderer.positionCount < 2)
            return false;

        for (int i = 0; i < lineRenderer.positionCount; i++)
            results.Add(lineRenderer.GetPosition(i));

        return results.Count >= 2;
    }

    Vector2 toward;
    public void HandlePlayerPositionChanged(Vector3 position, PlayerRunner playerRunner, object sender) // 러너만
    {
        var currentPosition = new Vector2(position.x, position.z);
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
                    AddExpandingPathPoint(currentPosition);
                    if (Object.HasStateAuthority)
                    {
                        var playerPathArray = playerPath.ToArray();
                        RPC_ExpandTerritory(playerPathArray);
                    }
                }
                StopExpanding();
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
                AddExpandingPathPoint(previousPosition);
                AddExpandingPathPoint(currentPosition);
            }

            if (Vector2.SqrMagnitude(currentPosition - previousPosition) > 0.01f)
            {
                if (playerPath.Count >= 2)
                {
                    toward = (currentPosition - previousPosition).normalized;
                    var dir = Vector2.Dot(toward, (currentPosition - playerPath[^1]).normalized);
                    if (dir < 1 - 0.01f)
                    {
                        AddExpandingPathPoint(previousPosition);
                    }
                }

                // 러너가 자신이 지나온 길을 다시 밟으면 게임 오버
                if (CheckPlayerRunnerCrossedOwnPath(currentPosition, playerRunner))
                    return;

                if (lineRenderer.positionCount > 0)
                {
                    lineRenderer.SetPosition(lineRenderer.positionCount - 1, new Vector3(currentPosition.x, 0, currentPosition.y));
                }
                previousPosition = currentPosition;
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_StartExpanding()
    {
        isExpanding = true;
        playerPath.Clear();
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_StopExpanding()
    {
        playerPath.Clear();
        lineRenderer.positionCount = 0;
        isExpanding = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_ResetExpansionAfterLifeline(Vector2 safePosition)
    {
        StartLifelineRecovery(safePosition);
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_AddExpandingPathPoint(Vector2 point)
    {
        playerPath.Add(point);
        lineRenderer.positionCount = playerPath.Count + 1;
        var converted = playerPath.ConvertAll(p => new Vector3(p.x, 0, p.y));
        converted.Add(new Vector3(point.x, 0, point.y));
        lineRenderer.SetPositions(converted.ToArray());
    }

    [Rpc(RpcSources.All, RpcTargets.All, Channel = RpcChannel.Reliable)]
    public void RPC_ExpandTerritory(Vector2[] playerPathArray)
    {
        Debug.Log($"{Runner.name} - Expanding territory with path: {playerPathArray.Length}");

        var playerPathFromHost = new List<Vector2>(playerPathArray);
        Territory.Expand(playerPathFromHost);
        TerritoryVisible.SetVertices(Territory.Vertices);

        if (Object.HasStateAuthority)
        {
            OnTerritoryExpandedEvent?.Invoke(Territory, this); // 호스트만
        }
    }

    // 플레이어 러너가 이전 경로를 밟았는지 확인하고 밟았다면 게임 오버 처리
    private bool CheckPlayerRunnerCrossedOwnPath(Vector2 currPos, PlayerRunner playerRunner)
    {
        if (isIntersected) { return true; }
        if (CheckCurrPathCrossPrevPath(currPos))
        {
            if (!Object.HasStateAuthority)
                return true;

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
    private bool CheckCurrPathCrossPrevPath(Vector2 currPos)
    {
        int count = playerPath.Count;

        if (count < 3)
            return false;

        Vector2 prevPos = previousPosition;
        if (Vector2.SqrMagnitude(currPos - prevPos) <= 0.0001f)
            return false;

        for (int i = 0; i < count - 2; i++) // 마지막 두 점은 현재 경로이므로 제외
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
