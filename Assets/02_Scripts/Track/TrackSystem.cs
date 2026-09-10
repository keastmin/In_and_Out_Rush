using System;
using Dev;
using Dev.Local;
using Dev.Network;
using Fusion;
using ProjectIO.Tracks;
using UnityEngine;

public sealed class TrackSystem : Dev.Network.System
{
    private const float TrackLengthToMapDiameterRatio = 1f / 8f;
    private const float FallbackWorldBoundaryRadius = 500f;

    [SerializeField] private float horizontalRadius = 16f;
    [SerializeField] private float verticalRadius = 8f;
    [SerializeField] private float noise = 0.1f;
    [SerializeField] private int vertexCount = 30;

    [Networked] private int TrackStageValue { get; set; }
    [Networked] private int EllipseSeed { get; set; }
    [Networked] private int PrimaryAxisValue { get; set; }
    [Networked] private NetworkBool ReversePrimary { get; set; }
    [Networked] private NetworkBool ReverseSecondary { get; set; }
    [Networked] private Vector3 TrackCenter { get; set; }
    [Networked] private float TrackLineLength { get; set; }
    [Networked, OnChangedRender(nameof(HandleTrackRevisionChanged))]
    private int TrackRevision { get; set; }

    private TrackVisible trackVisible;
    private int appliedRevision = -1;

    public Track Track { get; private set; }

    public TrackStage Stage => Track != null
        ? Track.Stage
        : (TrackStage)Mathf.Clamp(TrackStageValue, (int)TrackStage.InitialEllipse, (int)TrackStage.PerpendicularLines);

    public event Action<Track, TrackSystem, object> OnTrackChanged;

    public float TrackLineWidth => trackVisible != null ? trackVisible.LineWidth : 0f;

    void OnDrawGizmosSelected()
    {
        if (Track == null)
        {
            return;
        }

        Gizmos.color = Color.red;
        for (int pathIndex = 0; pathIndex < Track.Paths.Count; pathIndex++)
        {
            TrackPath path = Track.Paths[pathIndex];
            for (int vertexIndex = 0; vertexIndex < path.Vertices.Length; vertexIndex++)
            {
                Gizmos.DrawSphere(path.Vertices[vertexIndex], 0.1f);
            }
        }
    }

    protected override void OnSetUp()
    {
        StageBootstrapper bootstrapper = StageBootstrapper.Instance;
        trackVisible = bootstrapper != null ? bootstrapper.TrackVisible : null;

        if (Object != null && Object.HasStateAuthority && TrackRevision <= 0)
        {
            TrackStageValue = (int)TrackStage.InitialEllipse;
            EllipseSeed = UnityEngine.Random.Range(1, int.MaxValue);
            PrimaryAxisValue = (int)TrackAxis.Horizontal;
            ReversePrimary = false;
            ReverseSecondary = false;
            TrackCenter = bootstrapper != null ? bootstrapper.WorldCenter : transform.position;
            float worldRadius = bootstrapper != null
                ? bootstrapper.WorldBoundaryRadius
                : FallbackWorldBoundaryRadius;
            TrackLineLength = Mathf.Max(0f, worldRadius * 2f * TrackLengthToMapDiameterRatio);
            TrackRevision = 1;
        }

        ApplyReplicatedTrack();
    }

    protected override void OnTearDown()
    {
        Track = null;
        appliedRevision = -1;
    }

    public override void Render()
    {
        base.Render();
        if (TrackRevision > 0 && appliedRevision != TrackRevision)
        {
            ApplyReplicatedTrack();
        }
    }

    public bool ExpandTrack()
    {
        if (Object == null || !Object.HasStateAuthority)
        {
            Debug.LogWarning("Only the TrackSystem State Authority can transform the track.", this);
            return false;
        }

        TrackStage currentStage = (TrackStage)Mathf.Clamp(
            TrackStageValue,
            (int)TrackStage.InitialEllipse,
            (int)TrackStage.PerpendicularLines);
        if (currentStage == TrackStage.PerpendicularLines)
        {
            return false;
        }

        if (currentStage == TrackStage.InitialEllipse)
        {
            PrimaryAxisValue = UnityEngine.Random.Range(0, 4);
            ReversePrimary = UnityEngine.Random.Range(0, 2) == 1;
        }
        else
        {
            ReverseSecondary = UnityEngine.Random.Range(0, 2) == 1;
        }

        TrackStageValue = (int)currentStage + 1;
        TrackRevision++;
        ApplyReplicatedTrack();
        return true;
    }

    private void HandleTrackRevisionChanged()
    {
        ApplyReplicatedTrack();
    }

    private void ApplyReplicatedTrack()
    {
        if (TrackRevision <= 0)
        {
            return;
        }

        TrackStage stage = (TrackStage)Mathf.Clamp(
            TrackStageValue,
            (int)TrackStage.InitialEllipse,
            (int)TrackStage.PerpendicularLines);
        TrackAxis primaryAxis = (TrackAxis)Mathf.Clamp(PrimaryAxisValue, 0, 3);
        Track = new Track(
            stage,
            TrackGeometryGenerator.CreatePaths(
                stage,
                TrackCenter,
                vertexCount,
                horizontalRadius,
                verticalRadius,
                noise,
                EllipseSeed,
                TrackLineLength,
                primaryAxis,
                ReversePrimary,
                ReverseSecondary));
        appliedRevision = TrackRevision;

        if (trackVisible != null)
        {
            if (Runner != null)
            {
                trackVisible.name = $"{Runner.name} - Track";
            }

            trackVisible.RenderTrack(Track);
        }

        OnTrackChanged?.Invoke(Track, this, this);
    }
}
