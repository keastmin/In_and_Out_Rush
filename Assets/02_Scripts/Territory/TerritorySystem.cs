using System;
using System.Collections.Generic;
using Dev.Local;
using Dev.Network;
using Fusion;
using ProjectIO.Territory;
using Unity.Profiling;
using UnityEngine;

public class TerritorySystem : Dev.Network.System
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
    const int ConfirmedTrailFlushSampleCount = 4;
    const float ConfirmedTrailFlushInterval = 0.05f;
    const float TrailLiveHeadSyncInterval = 1f / 30f;
    const float MinExpansionMoveDistanceSqr = 0.01f;

    [Header("Initial Territory")]
    [SerializeField] int circlePointCount;
    [SerializeField] float circleRadius;

    [Header("Expanding")]
    [SerializeField] LineRenderer lineRenderer;
    readonly TerritoryExpansionSession expansionSession = new();
    readonly TerritoryExpansionReplication expansionReplication = new();
    readonly TerritoryTrailShadowRecorder shadowTrailRecorder = new();
    readonly TerritoryTrailReplicationStream trailReplicationStream = new();
    readonly List<Vector2> shadowLegacyPath = new();
    readonly List<FixedTerritoryPoint> ownerPredictedTrail = new();
    readonly List<FixedTerritoryPoint> replicatedTrailPath = new();
    TerritoryTrailChunkRenderer trailChunkRenderer;
    TickTimer confirmedTrailFlushTimer;
    TickTimer trailLiveHeadSyncTimer;
    bool expansionPathSuspended;
    bool ownerPredictionActive;
    bool ownerPredictionSuspended;
    bool hasOwnerPredictionPosition;
    bool stateRunnerIsLocalOwner;
    bool confirmedTrailCommitPending;
    bool territoryRuntimeCleanedUp;
    FixedTerritoryPoint ownerPredictionPreviousPosition;

    public Territory Territory;
    public TerritoryVisible TerritoryVisible;
    public bool IsExpanding => expansionSession.IsExpanding;
    public float ExpansionLineWidth => lineRenderer != null ? lineRenderer.widthMultiplier : 0f;
    private string ShadowLogOwnerName => Runner != null ? Runner.name : name;

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

    protected override void OnSetUp()
    {
        territoryRuntimeCleanedUp = false;
        TerritoryVisible = Dev.Network.StageBootstrapper.Instance.TerritoryVisible;
        trailChunkRenderer = gameObject.GetComponent<TerritoryTrailChunkRenderer>();
        if (trailChunkRenderer == null)
            trailChunkRenderer = gameObject.AddComponent<TerritoryTrailChunkRenderer>();
        trailChunkRenderer.Initialize(lineRenderer);

        GenerateInitialTerritory();

        Dev.Network.StageBootstrapper.Instance.PlayerRunner.OnPositionChanged += HandlePlayerPositionChanged;
    }

    protected override void OnTearDown()
    {
        CleanupTerritoryRuntime();
        base.OnTearDown();
    }

    protected override void OnDispose()
    {
        CleanupTerritoryRuntime();
        base.OnDispose();
    }

    private void CleanupTerritoryRuntime()
    {
        if (territoryRuntimeCleanedUp)
            return;

        territoryRuntimeCleanedUp = true;
        AbortShadowTrail("Territory system teardown", false);
        ClearTrailPresentation();

        if (Dev.Network.StageBootstrapper.Instance != null &&
            Dev.Network.StageBootstrapper.Instance.PlayerRunner != null)
        {
            Dev.Network.StageBootstrapper.Instance.PlayerRunner.OnPositionChanged -= HandlePlayerPositionChanged;
        }
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
        BeginShadowTrail();
    }

    private void StopExpanding()
    {
        if (Object != null && Object.HasStateAuthority)
        {
            if (confirmedTrailCommitPending)
                CommitConfirmedTrail();
            else
                AbortShadowTrail("Legacy expansion stopped before Trail commit");
        }

        expansionPathSuspended = false;
        expansionSession.Stop();
        if (lineRenderer != null)
            lineRenderer.positionCount = 0;
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
        AppendShadowTrailPoint(point);
    }

    private void BeginShadowTrail()
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        if (!shadowTrailRecorder.TryBegin(out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - Shadow Trail begin failed: {reason}");
            return;
        }

        ulong sessionId = shadowTrailRecorder.CurrentSessionId;
        if (!trailReplicationStream.TryBeginOutbound(sessionId, out reason))
        {
            shadowTrailRecorder.TryAbort(out _);
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail begin failed: {reason}");
            return;
        }

        confirmedTrailCommitPending = false;
        confirmedTrailFlushTimer = default;
        trailLiveHeadSyncTimer = default;
        RPC_BeginConfirmedTrail(sessionId);

        if (!stateRunnerIsLocalOwner)
            trailChunkRenderer?.Begin();
    }

    private void AppendShadowTrailPoint(Vector2 point)
    {
        if (Object == null || !Object.HasStateAuthority || !shadowTrailRecorder.IsRecording)
            return;

        int simulationTick = Runner != null ? Runner.Tick.Raw : 0;
        if (!shadowTrailRecorder.TryAppend(point, simulationTick, out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - Shadow Trail sample rejected: {reason}");
            AbortShadowTrail("Authoritative Trail sample rejected");
            return;
        }

        TerritoryTrailSample sample = shadowTrailRecorder.Samples[^1];
        if (!trailReplicationStream.TryAppendOutbound(sample, out reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail sample rejected: {reason}");
            AbortShadowTrail("Confirmed Trail stream rejected an authoritative sample");
            return;
        }

        if (!stateRunnerIsLocalOwner)
            trailChunkRenderer?.Append(sample.Point);

        FlushConfirmedTrail(false);
        SendTrailLiveHeadIfDue(sample);
    }

    private void CommitAndCompareShadowTrail()
    {
        if (Object == null || !Object.HasStateAuthority || !shadowTrailRecorder.IsRecording)
            return;

        if (!expansionSession.TryCopyPathTo(shadowLegacyPath))
        {
            AbortShadowTrail("Legacy path unavailable at shadow commit");
            return;
        }

        if (!shadowTrailRecorder.TryCommit(
                shadowLegacyPath,
                out TerritoryTrailShadowComparison comparison,
                out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - Shadow Trail commit failed: {reason}");
            AbortShadowTrail("Shadow Trail commit failed");
            return;
        }

        confirmedTrailCommitPending = true;

        if (!comparison.IsMatch)
        {
            if (Debug.isDebugBuild)
            {
                Debug.LogWarning(
                    $"{ShadowLogOwnerName} - Shadow Trail mismatch. Session: {shadowTrailRecorder.LastSessionId}, " +
                    $"Legacy: {comparison.LegacyPointCount}, Shadow: {comparison.ShadowSampleCount}, " +
                    $"First mismatch: {comparison.FirstMismatchIndex}");
            }
            return;
        }

        if (Debug.isDebugBuild)
        {
            Debug.Log(
                $"{ShadowLogOwnerName} - Shadow Trail matched. Session: {shadowTrailRecorder.LastSessionId}, " +
                $"Samples: {comparison.ShadowSampleCount}, Fragments: {shadowTrailRecorder.LastFragmentCount}");
        }
    }

    private void CommitConfirmedTrail()
    {
        if (!trailReplicationStream.IsOutboundActive)
        {
            confirmedTrailCommitPending = false;
            ClearTrailPresentation();
            return;
        }

        ulong sessionId = trailReplicationStream.OutboundSessionId;
        if (!FlushConfirmedTrail(true))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail flush failed before commit.");
            AbortShadowTrail("Confirmed Trail flush failed");
            return;
        }
        if (!trailReplicationStream.TryCommitOutbound(out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail commit failed: {reason}");
            AbortShadowTrail("Confirmed Trail commit failed");
            return;
        }

        RPC_CommitConfirmedTrail(sessionId);
        confirmedTrailCommitPending = false;
        confirmedTrailFlushTimer = default;
        trailLiveHeadSyncTimer = default;
        ClearTrailPresentation();
    }

    private void AbortShadowTrail(string cause, bool replicate = true)
    {
        if (Object == null || !Object.HasStateAuthority)
            return;

        ulong sessionId = trailReplicationStream.IsOutboundActive
            ? trailReplicationStream.OutboundSessionId
            : shadowTrailRecorder.CurrentSessionId;
        bool hadOutboundSession = trailReplicationStream.IsOutboundActive;

        if (shadowTrailRecorder.IsRecording &&
            !shadowTrailRecorder.TryAbort(out string shadowReason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - Shadow Trail abort failed: {shadowReason}");
        }

        if (trailReplicationStream.IsOutboundActive &&
            !trailReplicationStream.TryAbortOutbound(out string streamReason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail abort failed: {streamReason}");
        }

        if (replicate && hadOutboundSession)
            RPC_AbortConfirmedTrail(sessionId);

        confirmedTrailCommitPending = false;
        confirmedTrailFlushTimer = default;
        trailLiveHeadSyncTimer = default;
        ClearTrailPresentation();

        if (Debug.isDebugBuild && hadOutboundSession)
            Debug.Log($"{ShadowLogOwnerName} - Trail aborted. Session: {sessionId}, Cause: {cause}");
    }

    private bool FlushConfirmedTrail(bool force)
    {
        if (!trailReplicationStream.IsOutboundActive ||
            trailReplicationStream.PendingOutboundSampleCount == 0)
        {
            return true;
        }

        if (!force &&
            trailReplicationStream.PendingOutboundSampleCount < ConfirmedTrailFlushSampleCount &&
            !confirmedTrailFlushTimer.ExpiredOrNotRunning(Runner))
        {
            return true;
        }

        do
        {
            if (!trailReplicationStream.TryTakeOutboundPacket(
                    out TerritoryTrailPacket packet,
                    out string reason))
            {
                Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail packetization failed: {reason}");
                return false;
            }

            RPC_AppendConfirmedTrail(
                packet.SessionId,
                packet.Sequence,
                packet.FirstSampleSequence,
                TerritoryTrailPacket.Encode(packet));
        }
        while (force && trailReplicationStream.PendingOutboundSampleCount > 0);

        confirmedTrailFlushTimer = TickTimer.CreateFromSeconds(Runner, ConfirmedTrailFlushInterval);
        return true;
    }

    private void SendTrailLiveHeadIfDue(TerritoryTrailSample sample)
    {
        if (!trailReplicationStream.IsOutboundActive ||
            trailReplicationStream.PendingOutboundSampleCount == 0 ||
            !trailLiveHeadSyncTimer.ExpiredOrNotRunning(Runner))
        {
            return;
        }

        RPC_UpdateTrailLiveHead(
            sample.SessionId,
            sample.Sequence,
            sample.Point.X,
            sample.Point.Y);
        trailLiveHeadSyncTimer = TickTimer.CreateFromSeconds(Runner, TrailLiveHeadSyncInterval);
    }

    public bool TryGetCurrentExpansionPath(List<Vector3> results)
    {
        if (results == null)
            return false;

        if (ownerPredictionActive && ownerPredictedTrail.Count > 0)
            return CopyFixedPathTo(ownerPredictedTrail, results);

        if (Object != null &&
            !Object.HasStateAuthority &&
            trailReplicationStream.TryCopyInboundPathTo(replicatedTrailPath))
        {
            return CopyFixedPathTo(replicatedTrailPath, results);
        }

        return expansionSession.TryCopyPathTo(results);
    }

    public bool TryPauseExpandingPath(Vector3 position)
    {
        if (!Object.HasStateAuthority ||
            !expansionSession.IsExpanding ||
            expansionSession.IsRecoveringFromLifeline ||
            expansionPathSuspended)
        {
            return false;
        }

        Vector2 pausePosition = new(position.x, position.z);
        if (expansionSession.HasMovedEnough(pausePosition, 0.0001f))
        {
            AddExpandingPathPoint(pausePosition, true);
            FlushConfirmedTrail(true);
        }

        expansionSession.SetPreviousPosition(pausePosition);
        expansionPathSuspended = true;
        ReplicateTrailSuspension(true, pausePosition);
        return true;
    }

    public void ResumePausedExpandingPath(Vector3 position, PlayerRunner playerRunner)
    {
        if (!Object.HasStateAuthority || !expansionPathSuspended)
            return;

        expansionPathSuspended = false;
        if (!expansionSession.IsExpanding || Territory == null)
            return;

        Vector2 currentPosition = new(position.x, position.z);
        if (Territory.IsPointInPolygon(currentPosition))
        {
            if (expansionSession.PlayerPathCount > 1)
            {
                AddExpandingPathPoint(currentPosition, true);
                ExpandTerritoryFromCurrentPath();
            }

            StopExpanding();
            RPC_StopExpanding();
            expansionSession.SetPreviousPosition(currentPosition);
            return;
        }

        if (!expansionSession.HasMovedEnough(currentPosition, 0.0001f))
        {
            expansionSession.SetPreviousPosition(currentPosition);
            ReplicateTrailSuspension(false, currentPosition);
            return;
        }

        if (CheckPlayerRunnerCrossedOwnPath(currentPosition, true, playerRunner))
            return;

        AddExpandingPathPoint(currentPosition, true);
        FlushConfirmedTrail(true);
        expansionSession.SetPreviousPosition(currentPosition);
        ReplicateTrailSuspension(false, currentPosition);
    }

    public void HandlePlayerPositionChanged(Vector3 position, PlayerRunner playerRunner, object sender) // 러너만
    {
        using (HandlePlayerPositionChangedMarker.Auto())
        {
        var currentPosition = new Vector2(position.x, position.z);

        HandleOwnerTrailPrediction(currentPosition, playerRunner);

        if (!Object.HasStateAuthority)
            return;

        stateRunnerIsLocalOwner = IsInputAuthority(playerRunner);

        if (expansionPathSuspended)
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
                AddExpandingPathPoint(currentPosition, true);
                FlushConfirmedTrail(true);
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
                }

                expansionSession.SetPreviousPosition(currentPosition);
            }
        }
        }
    }

    private void HandleOwnerTrailPrediction(Vector2 currentPosition, PlayerRunner playerRunner)
    {
        if (!IsInputAuthority(playerRunner) || Territory == null)
            return;

        FixedTerritoryPoint currentPoint = FixedTerritoryPoint.FromWorld(
            currentPosition.x,
            currentPosition.y);
        if (!hasOwnerPredictionPosition)
        {
            ownerPredictionPreviousPosition = currentPoint;
            hasOwnerPredictionPosition = true;
            return;
        }

        if (playerRunner.IsDead)
        {
            ClearTrailPresentation();
            ownerPredictionPreviousPosition = currentPoint;
            return;
        }

        if (ownerPredictionSuspended)
            return;

        bool isInTerritory = Territory.IsPointInPolygon(currentPosition);
        if (isInTerritory)
        {
            if (ownerPredictionActive)
                ClearTrailPresentation();

            ownerPredictionPreviousPosition = currentPoint;
            return;
        }

        if (!ownerPredictionActive)
        {
            ownerPredictedTrail.Clear();
            trailChunkRenderer?.Begin();
            AppendOwnerPredictedPoint(ownerPredictionPreviousPosition);
            AppendOwnerPredictedPoint(currentPoint);
            ownerPredictionActive = true;
        }
        else if (HasMovedEnough(ownerPredictedTrail[^1], currentPoint))
        {
            AppendOwnerPredictedPoint(currentPoint);
        }

        ownerPredictionPreviousPosition = currentPoint;
    }

    private void AppendOwnerPredictedPoint(FixedTerritoryPoint point)
    {
        if (ownerPredictedTrail.Count > 0 && ownerPredictedTrail[^1] == point)
            return;

        ownerPredictedTrail.Add(point);
        trailChunkRenderer?.Append(point);
    }

    private void ReplicateTrailSuspension(bool suspended, Vector2 position)
    {
        if (!trailReplicationStream.IsOutboundActive)
            return;

        FixedTerritoryPoint fixedPosition = FixedTerritoryPoint.FromWorld(position.x, position.y);
        if (stateRunnerIsLocalOwner)
        {
            RebuildOwnerPrediction(shadowTrailRecorder.Samples);
            ownerPredictionSuspended = suspended;
            ownerPredictionPreviousPosition = fixedPosition;
            hasOwnerPredictionPosition = true;
        }
        else
        {
            trailChunkRenderer?.ClearLiveHead();
        }

        RPC_SetConfirmedTrailSuspended(
            trailReplicationStream.OutboundSessionId,
            suspended,
            fixedPosition.X,
            fixedPosition.Y);
    }

    private void RebuildOwnerPrediction(IReadOnlyList<TerritoryTrailSample> samples)
    {
        ownerPredictedTrail.Clear();
        if (samples != null)
        {
            for (int index = 0; index < samples.Count; index++)
            {
                FixedTerritoryPoint point = samples[index].Point;
                if (ownerPredictedTrail.Count == 0 || ownerPredictedTrail[^1] != point)
                    ownerPredictedTrail.Add(point);
            }
        }

        trailChunkRenderer?.Rebuild(ownerPredictedTrail);
        ownerPredictionActive = ownerPredictedTrail.Count > 0;
    }

    private void ClearTrailPresentation()
    {
        ownerPredictedTrail.Clear();
        ownerPredictionActive = false;
        ownerPredictionSuspended = false;
        trailChunkRenderer?.Clear();
    }

    private static bool CopyFixedPathTo(
        IReadOnlyList<FixedTerritoryPoint> source,
        List<Vector3> results)
    {
        results.Clear();
        for (int index = 0; index < source.Count; index++)
        {
            FixedTerritoryPoint point = source[index];
            results.Add(new Vector3((float)point.WorldX, 0f, (float)point.WorldY));
        }

        return results.Count > 0;
    }

    private static bool HasMovedEnough(FixedTerritoryPoint previous, FixedTerritoryPoint current)
    {
        double deltaX = current.WorldX - previous.WorldX;
        double deltaY = current.WorldY - previous.WorldY;
        return deltaX * deltaX + deltaY * deltaY >= MinExpansionMoveDistanceSqr;
    }

    private static bool IsInputAuthority(PlayerRunner playerRunner)
        => playerRunner != null &&
           playerRunner.Object != null &&
           playerRunner.Object.HasInputAuthority;

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
        ownerPredictionPreviousPosition = FixedTerritoryPoint.FromWorld(safePosition.x, safePosition.y);
        hasOwnerPredictionPosition = true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    public void RPC_AddExpandingPathPoints(Vector2[] points)
    {
        if (points == null)
            return;

        for (int i = 0; i < points.Length; i++)
            AddExpandingPathPoint(points[i]);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_BeginConfirmedTrail(ulong sessionId)
    {
        if (!trailReplicationStream.TryBeginInbound(sessionId, out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail receive begin failed: {reason}");
            return;
        }

        if (!IsLocalTrailOwner())
            trailChunkRenderer?.Begin();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_AppendConfirmedTrail(
        ulong sessionId,
        uint packetSequence,
        uint firstSampleSequence,
        int[] payload)
    {
        if (!TerritoryTrailPacket.TryDecode(
                sessionId,
                packetSequence,
                firstSampleSequence,
                payload,
                out TerritoryTrailPacket packet,
                out string reason) ||
            !trailReplicationStream.TryAppendInbound(packet, out reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail packet rejected: {reason}");
            return;
        }

        if (IsLocalTrailOwner())
            return;

        for (int index = 0; index < packet.Samples.Count; index++)
            trailChunkRenderer?.Append(packet.Samples[index].Point);

        if (trailReplicationStream.HasLiveHead)
            trailChunkRenderer?.SetLiveHead(trailReplicationStream.LiveHead);
        else
            trailChunkRenderer?.ClearLiveHead();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Unreliable)]
    private void RPC_UpdateTrailLiveHead(
        ulong sessionId,
        uint sampleSequence,
        int x,
        int y)
    {
        if (IsLocalTrailOwner())
            return;

        var point = new FixedTerritoryPoint(x, y);
        if (trailReplicationStream.TryUpdateLiveHead(
                sessionId,
                sampleSequence,
                point,
                out _))
        {
            trailChunkRenderer?.SetLiveHead(point);
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_SetConfirmedTrailSuspended(
        ulong sessionId,
        bool suspended,
        int x,
        int y)
    {
        if (!trailReplicationStream.IsInboundActive ||
            trailReplicationStream.InboundSessionId != sessionId)
        {
            return;
        }

        if (IsLocalTrailOwner())
        {
            RebuildOwnerPrediction(trailReplicationStream.ConfirmedSamples);
            ownerPredictionSuspended = suspended;
            ownerPredictionPreviousPosition = new FixedTerritoryPoint(x, y);
            hasOwnerPredictionPosition = true;
        }
        else
        {
            trailChunkRenderer?.ClearLiveHead();
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_CommitConfirmedTrail(ulong sessionId)
    {
        if (!trailReplicationStream.IsInboundActive ||
            trailReplicationStream.InboundSessionId != sessionId)
        {
            return;
        }

        if (!trailReplicationStream.TryCommitInbound(sessionId, out string reason))
        {
            Debug.LogWarning($"{ShadowLogOwnerName} - confirmed Trail receive commit failed: {reason}");
            if (trailReplicationStream.IsInboundActive &&
                trailReplicationStream.InboundSessionId == sessionId)
            {
                trailReplicationStream.TryAbortInbound(sessionId, out _);
            }
        }

        ClearTrailPresentation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_AbortConfirmedTrail(ulong sessionId)
    {
        if (!trailReplicationStream.IsInboundActive ||
            trailReplicationStream.InboundSessionId != sessionId)
        {
            return;
        }

        if (trailReplicationStream.TryAbortInbound(sessionId, out _))
            ClearTrailPresentation();
    }

    private bool IsLocalTrailOwner()
    {
        PlayerRunner playerRunner = Dev.Network.StageBootstrapper.Instance != null
            ? Dev.Network.StageBootstrapper.Instance.PlayerRunner
            : null;
        return IsInputAuthority(playerRunner);
    }

    private void ExpandTerritoryFromCurrentPath()
    {
        using (ExpandTerritoryMarker.Auto())
        {
        CommitAndCompareShadowTrail();
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

            AbortShadowTrail("Player crossed own path");

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
