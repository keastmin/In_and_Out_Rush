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
    private static readonly ProfilerMarker UpdateExpansionMeshMarker =
        new("TerritorySystem.UpdateExpansionMesh");
    private static readonly ProfilerMarker BackgroundExpansionScheduleMarker =
        new("TerritorySystem.BackgroundExpansionSchedule");
    private static readonly ProfilerMarker BackgroundExpansionPublishMarker =
        new("TerritorySystem.BackgroundExpansionPublish");
    private static readonly ProfilerMarker ExpansionTransferFlushMarker =
        new("TerritorySystem.ExpansionTransferFlush");
    private static readonly ProfilerMarker NotifyExpansionConsumersMarker =
        new("TerritorySystem.NotifyExpansionConsumers");
    private static readonly ProfilerMarker ChunkDeltaPacketizeMarker =
        new("TerritorySystem.ChunkDeltaPacketize");
    private static readonly ProfilerMarker ChunkTransferFlushMarker =
        new("TerritorySystem.ChunkTransferFlush");
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
    readonly TerritoryChunkReplicationStream chunkReplicationStream = new();
    TerritoryChunkStore territoryChunkShadowStore = new();
    TerritoryBackgroundExpansionWorker backgroundExpansionWorker;
    readonly List<Vector2> shadowLegacyPath = new();
    readonly TerritoryAppendOnlyBlockList<FixedTerritoryPoint> ownerPredictedTrail = new();
    readonly List<FixedTerritoryPoint> replicatedTrailPath = new();
    readonly List<FixedTerritoryPoint> territoryChunkPolygon = new();
    TerritoryTrailChunkRenderer trailChunkRenderer;
    TickTimer confirmedTrailFlushTimer;
    TickTimer trailLiveHeadSyncTimer;
    bool expansionPathSuspended;
    bool ownerPredictionActive;
    bool ownerPredictionSuspended;
    bool hasOwnerPredictionPosition;
    bool stateRunnerIsLocalOwner;
    bool confirmedTrailCommitPending;
    bool backgroundExpansionSchedulePending;
    ulong backgroundExpansionPendingSessionId;
    ulong territoryExpansionRevision;
    bool expansionCalculationPending;
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
        territoryChunkShadowStore = new TerritoryChunkStore();
        backgroundExpansionWorker = Object == null || Object.HasStateAuthority
            ? new TerritoryBackgroundExpansionWorker()
            : null;
        if (Object == null || Object.HasStateAuthority)
            chunkReplicationStream.Reset();
        territoryExpansionRevision = 1UL;
        expansionCalculationPending = false;
        expansionReplication.Reset(territoryExpansionRevision);
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
        chunkReplicationStream.Reset();
        expansionReplication.Reset(1UL);
        expansionCalculationPending = false;
        backgroundExpansionWorker?.Dispose();
        backgroundExpansionWorker = null;

        if (Dev.Network.StageBootstrapper.Instance != null &&
            Dev.Network.StageBootstrapper.Instance.PlayerRunner != null)
        {
            Dev.Network.StageBootstrapper.Instance.PlayerRunner.OnPositionChanged -= HandlePlayerPositionChanged;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (territoryRuntimeCleanedUp || Object == null)
            return;

        if (Object.HasStateAuthority)
        {
            FlushTerritoryChunkTransfers();
            FlushTerritoryExpansionTransfers();
        }
    }

    public override void Render()
    {
        base.Render();
        if (territoryRuntimeCleanedUp || Object == null || !Object.HasStateAuthority ||
            backgroundExpansionWorker == null)
        {
            return;
        }

        using (BackgroundExpansionPublishMarker.Auto())
        {
            if (!backgroundExpansionWorker.TryTakeCompleted(
                    out TerritoryBackgroundExpansionResult result))
                return;

            if (!result.IsSuccess)
            {
                expansionCalculationPending = false;
                RPC_SetTerritoryExpansionPending(false);
                Debug.LogWarning(
                    $"{ShadowLogOwnerName} - background Territory expansion rejected: " +
                    result.FailureReason);
                return;
            }

            string applyReason = null;
            if (result.SourceRevision != territoryExpansionRevision ||
                !TryApplyCompletedExpansion(result.Presentation, true, out applyReason))
            {
                expansionCalculationPending = false;
                RPC_SetTerritoryExpansionPending(false);
                Debug.LogWarning(
                    $"{ShadowLogOwnerName} - completed Territory expansion was discarded: " +
                    (applyReason ?? "stale source revision"));
                return;
            }

            territoryExpansionRevision = result.Presentation.Revision;
            expansionCalculationPending = false;
            if (!expansionReplication.TryEnqueue(
                    result.Presentation,
                    result.Packets,
                    out string replicationReason))
            {
                RPC_SetTerritoryExpansionPending(false);
                Debug.LogWarning(
                    $"{ShadowLogOwnerName} - Territory expansion replication rejected: " +
                    replicationReason);
                return;
            }

            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"{ShadowLogOwnerName} - background Territory expansion published. " +
                    $"Revision: {territoryExpansionRevision}, Worker ms: " +
                    $"{result.Elapsed.TotalMilliseconds:F2}, Worker thread: {result.WorkerThreadId}");
            }
        }
    }

    void GenerateInitialTerritory()
    {
        var vertices = GenerateCircleTerritory();

        CreateTerritory(vertices);
        CommitTerritoryChunkShadow("initial Territory");
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
        backgroundExpansionSchedulePending = false;
        backgroundExpansionPendingSessionId = 0UL;
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

        backgroundExpansionSchedulePending = true;
        backgroundExpansionPendingSessionId = shadowTrailRecorder.LastSessionId;

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
            backgroundExpansionSchedulePending = false;
            backgroundExpansionPendingSessionId = 0UL;
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
        backgroundExpansionSchedulePending = false;
        backgroundExpansionPendingSessionId = 0UL;
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
        backgroundExpansionSchedulePending = false;
        backgroundExpansionPendingSessionId = 0UL;
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

        if (expansionCalculationPending)
        {
            expansionSession.SetPreviousPosition(currentPosition);
            return;
        }

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
        if (expansionCalculationPending)
        {
            if (ownerPredictionActive)
                ClearTrailPresentation();
            ownerPredictionPreviousPosition = currentPoint;
            hasOwnerPredictionPosition = true;
            return;
        }
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
            if (!backgroundExpansionSchedulePending || backgroundExpansionWorker == null)
            {
                Debug.LogWarning(
                    $"{Runner.name} - Territory expansion was not scheduled. " +
                    $"Path point count: {shadowLegacyPath.Count}");
                return;
            }

            ulong sessionId = backgroundExpansionPendingSessionId;
            backgroundExpansionSchedulePending = false;
            backgroundExpansionPendingSessionId = 0UL;
            using (BackgroundExpansionScheduleMarker.Auto())
            {
                if (!TerritoryBackgroundExpansionWorkItem.TryCreate(
                        sessionId,
                        territoryExpansionRevision,
                        Territory.Vertices,
                        shadowLegacyPath,
                        out TerritoryBackgroundExpansionWorkItem item,
                        out string reason) ||
                    !backgroundExpansionWorker.TrySchedule(item, out reason))
                {
                    Debug.LogWarning(
                        $"{Runner.name} - Territory background expansion schedule failed: {reason}");
                    return;
                }
            }

            expansionCalculationPending = true;
            RPC_SetTerritoryExpansionPending(true);
            if (Debug.isDebugBuild)
            {
                Debug.Log(
                    $"{Runner.name} - Territory expansion scheduled in background. " +
                    $"Revision: {territoryExpansionRevision}, Path points: {shadowLegacyPath.Count}");
            }
        }
    }

    private bool TryApplyCompletedExpansion(
        TerritoryExpansionPresentationData presentation,
        bool notifyConsumers,
        out string reason)
    {
        if (presentation == null || Territory == null || TerritoryVisible == null)
        {
            reason = "Territory expansion publication is not initialized.";
            return false;
        }

        var vertices = new List<Vector2>(presentation.Vertices.Count);
        var triangles = new List<int>(presentation.Triangles.Count);
        for (int i = 0; i < presentation.Vertices.Count; i++)
            vertices.Add(presentation.Vertices[i]);
        for (int i = 0; i < presentation.Triangles.Count; i++)
            triangles.Add(presentation.Triangles[i]);

        var meshData = new TerritoryMeshData(vertices, triangles);
        using (UpdateExpansionMeshMarker.Auto())
        {
            if (!TerritoryVisible.SetMeshData(meshData))
            {
                reason = "Unity Territory mesh update failed.";
                return false;
            }
        }

        Territory.ReplaceVertices(vertices);
        expansionCalculationPending = false;
        if (notifyConsumers)
        {
            using (NotifyExpansionConsumersMarker.Auto())
                OnTerritoryExpandedEvent?.Invoke(Territory, this);
        }

        reason = null;
        return true;
    }

    private bool CommitTerritoryChunkShadow(string cause)
    {
        if (Object == null || !Object.HasStateAuthority ||
            Territory == null || Territory.Vertices == null)
        {
            return false;
        }

        territoryChunkPolygon.Clear();
        try
        {
            for (int i = 0; i < Territory.Vertices.Count; i++)
            {
                Vector2 vertex = Territory.Vertices[i];
                territoryChunkPolygon.Add(FixedTerritoryPoint.FromWorld(vertex.x, vertex.y));
            }
        }
        catch (ArgumentOutOfRangeException exception)
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Chunk Territory shadow conversion failed. " +
                $"Cause: {cause}, Reason: {exception.Message}");
            return false;
        }

        ulong baseRevision = territoryChunkShadowStore.Current.Revision;
        if (!territoryChunkShadowStore.TryCommit(
                baseRevision,
                territoryChunkPolygon,
                out TerritoryChunkCommitResult result,
                out string reason))
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Chunk Territory shadow commit failed. " +
                $"Base revision: {baseRevision}, Cause: {cause}, Reason: {reason}");
            return false;
        }

        string replicationReason;
        bool enqueued;
        using (ChunkDeltaPacketizeMarker.Auto())
            enqueued = chunkReplicationStream.TryEnqueueDelta(result, out replicationReason);

        if (!enqueued)
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Chunk Territory delta enqueue failed. " +
                $"Revision: {result.Revision}, Cause: {cause}, Reason: {replicationReason}");
        }

        if (Debug.isDebugBuild)
        {
            Debug.Log(
                $"{ShadowLogOwnerName} - Chunk Territory shadow committed. " +
                $"Revision: {result.Revision}, Changed Chunks: {result.ChangedChunks.Count}, " +
                $"Cause: {cause}");
        }

        return true;
    }

    private void FlushTerritoryChunkTransfers()
    {
        using var _ = ChunkTransferFlushMarker.Auto();
        int dataPacketsSent = 0;
        while (chunkReplicationStream.TryTakeOutbound(
                   TerritoryChunkReplicationStream.MaximumDataPacketsPerTick - dataPacketsSent,
                   out TerritoryChunkReplicationStream.OutboundMessage message))
        {
            SendBroadcastTerritoryChunkMessage(message);

            if (message.Type == TerritoryChunkReplicationStream.OutboundMessageType.Data)
                dataPacketsSent++;
        }
    }

    private void SendBroadcastTerritoryChunkMessage(
        TerritoryChunkReplicationStream.OutboundMessage message)
    {
        switch (message.Type)
        {
            case TerritoryChunkReplicationStream.OutboundMessageType.Begin:
                RPC_BeginTerritoryChunkDelta(
                    message.BaseRevision,
                    message.Revision,
                    message.PacketCount);
                break;
            case TerritoryChunkReplicationStream.OutboundMessageType.Data:
                RPC_AppendTerritoryChunkDelta(
                    message.BaseRevision,
                    message.Revision,
                    message.PacketSequence,
                    message.Words);
                break;
            case TerritoryChunkReplicationStream.OutboundMessageType.Complete:
                RPC_CompleteTerritoryChunkDelta(
                    message.BaseRevision,
                    message.Revision);
                break;
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_BeginTerritoryChunkDelta(
        ulong baseRevision,
        ulong revision,
        int packetCount)
    {
        HandleTerritoryChunkTransferBegin(baseRevision, revision, packetCount);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_AppendTerritoryChunkDelta(
        ulong baseRevision,
        ulong revision,
        uint packetSequence,
        int[] words)
    {
        HandleTerritoryChunkTransferData(
            baseRevision,
            revision,
            packetSequence,
            words);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_CompleteTerritoryChunkDelta(
        ulong baseRevision,
        ulong revision)
    {
        HandleTerritoryChunkTransferComplete(baseRevision, revision);
    }

    private void HandleTerritoryChunkTransferBegin(
        ulong baseRevision,
        ulong revision,
        int packetCount)
    {
        if (territoryRuntimeCleanedUp)
            return;

        if (!chunkReplicationStream.TryBeginInbound(
                baseRevision,
                revision,
                packetCount,
                out string reason))
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Chunk Territory transfer begin rejected. " +
                $"Revision: {revision}, Reason: {reason}");
        }
    }

    private void HandleTerritoryChunkTransferData(
        ulong baseRevision,
        ulong revision,
        uint packetSequence,
        int[] words)
    {
        if (territoryRuntimeCleanedUp)
            return;

        chunkReplicationStream.TryAppendInbound(
            baseRevision,
            revision,
            packetSequence,
            words,
            out _);
    }

    private void HandleTerritoryChunkTransferComplete(
        ulong baseRevision,
        ulong revision)
    {
        if (territoryRuntimeCleanedUp)
            return;

        if (!chunkReplicationStream.TryCompleteInbound(
                baseRevision,
                revision,
                out TerritoryChunkSnapshot snapshot,
                out string reason))
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Chunk Territory transfer terminal rejected. " +
                $"Revision: {revision}, Reason: {reason}");
            return;
        }

        if (Debug.isDebugBuild)
        {
            Debug.Log(
                $"{ShadowLogOwnerName} - Chunk Territory replica applied. " +
                $"Revision: {snapshot.Revision}, Chunks: {snapshot.Chunks.Count}");
        }
    }

    private void FlushTerritoryExpansionTransfers()
    {
        using var _ = ExpansionTransferFlushMarker.Auto();
        int dataPacketsSent = 0;
        while (expansionReplication.TryTakeOutbound(
                   TerritoryExpansionReplication.MaximumDataPacketsPerTick - dataPacketsSent,
                   out TerritoryExpansionReplication.OutboundMessage message))
        {
            switch (message.Type)
            {
                case TerritoryExpansionReplication.OutboundMessageType.Begin:
                    RPC_BeginTerritoryExpansionResult(
                        message.SourceRevision,
                        message.Revision,
                        message.VertexCount,
                        message.TriangleCount,
                        message.PacketCount);
                    break;
                case TerritoryExpansionReplication.OutboundMessageType.Data:
                    RPC_AppendTerritoryExpansionResult(
                        message.SourceRevision,
                        message.Revision,
                        message.PacketSequence,
                        message.Words);
                    dataPacketsSent++;
                    break;
                case TerritoryExpansionReplication.OutboundMessageType.Complete:
                    RPC_CompleteTerritoryExpansionResult(
                        message.SourceRevision,
                        message.Revision);
                    break;
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_SetTerritoryExpansionPending(bool pending)
    {
        expansionCalculationPending = pending;
        if (pending)
            ClearTrailPresentation();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_BeginTerritoryExpansionResult(
        ulong sourceRevision,
        ulong revision,
        int vertexCount,
        int triangleCount,
        int packetCount)
    {
        if (!expansionReplication.TryBeginInbound(
                sourceRevision,
                revision,
                vertexCount,
                triangleCount,
                packetCount,
                out string reason))
        {
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Territory expansion receive begin failed: {reason}");
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_AppendTerritoryExpansionResult(
        ulong sourceRevision,
        ulong revision,
        uint packetSequence,
        int[] words)
    {
        expansionReplication.TryAppendInbound(
            sourceRevision,
            revision,
            packetSequence,
            words,
            out _);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies, Channel = RpcChannel.Reliable)]
    private void RPC_CompleteTerritoryExpansionResult(
        ulong sourceRevision,
        ulong revision)
    {
        if (!expansionReplication.TryCompleteInbound(
                sourceRevision,
                revision,
                out TerritoryExpansionPresentationData presentation,
                out string reason))
        {
            expansionCalculationPending = false;
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - Territory expansion receive terminal failed: {reason}");
            return;
        }

        if (!TryApplyCompletedExpansion(presentation, false, out string applyReason))
        {
            expansionCalculationPending = false;
            Debug.LogWarning(
                $"{ShadowLogOwnerName} - replicated Territory expansion apply failed: {applyReason}");
            return;
        }

        territoryExpansionRevision = presentation.Revision;
        expansionCalculationPending = false;
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
