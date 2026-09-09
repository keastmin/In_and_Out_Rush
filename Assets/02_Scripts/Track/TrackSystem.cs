using System;
using System.Collections.Generic;
using Dev;
using Dev.Local;
using Dev.Network;
using Fusion;
using UnityEngine;

public class TrackSystem : Dev.Network.System
{
    [SerializeField] float horizontalRadius;
    [SerializeField] float verticalRadius;
    [SerializeField] float noise;
    [SerializeField] int vertexCount;
    [SerializeField] int smoothingIteration;
    [SerializeField] int noiseVertexCount;
    [SerializeField] float noiseIntensity;

    public Track Track;
    TrackVisible trackVisible;
    public event Action<Vector3[], TrackSystem, object> OnTrackChanged;
    public float TrackLineWidth => trackVisible != null ? trackVisible.LineWidth : 0f;

    void OnDrawGizmosSelected()
    {
        if (Track == null) { return; }

        Gizmos.color = Color.red;
        foreach (var vertex in Track.Vertices)
        {
            Gizmos.DrawSphere(vertex, 0.1f);
        }
    }

    protected override void OnSetUp()
    {
        trackVisible = Dev.Network.StageBootstrapper.Instance.TrackVisible;

        GenerateTrack();
        if (!Object.HasStateAuthority)
        {
            RequestSyncTrack();
        }
    }

    void GenerateTrack()
    {
        expansionLevel++;
        CreateTrack(vertexCount, horizontalRadius, verticalRadius, noise);
        trackVisible.name = $"{Runner.name} - Track";
        trackVisible.GenerateTrackVertices(Track.Vertices);
        trackVisible.GenerateTrackLine(Track.Vertices);

        if (Object != null && Object.HasStateAuthority)
        {
            NotifyTrackChanged();
        }
    }

    void CreateTrack(int vertexCount, float horizontalRadius, float verticalRadius, float noise)
    {
        var trackVertices = new Vector3[vertexCount];

        for (int i = 0; i < vertexCount; i++)
        {
            float angle = Mathf.PI + 2 * Mathf.PI * i / vertexCount;
            float x = Mathf.Cos(angle) * horizontalRadius;
            float z = Mathf.Sin(angle) * verticalRadius;

            x += UnityEngine.Random.Range(-noise, noise);
            z += UnityEngine.Random.Range(-noise, noise);

            var vertex = new Vector3(x, 0, z);
            trackVertices[i] = vertex;
        }

        Track = new Track
        {
            Vertices = trackVertices,
        };
    }

    readonly List<Vector3> temporaryVertices = new();

    void RequestSyncTrack()
    {
        RPC_RequestSyncTrack();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    void RPC_RequestSyncTrack()
    {
        if (!Object.HasStateAuthority) { return; }

        SyncTrack(Track.Vertices);
    }

    void SyncTrack(Vector3[] vertices)
    {
        if (!Object.HasStateAuthority) { return; }
        if (vertices.Length <= 0) { return; }

        for (int i = 0; i < vertices.Length; i += 10)
        {
            if (i + 10 < vertices.Length)
            {
                var segment = new Vector3[10];
                System.Array.Copy(vertices, i, segment, 0, 10);
                RPC_SyncTrackVertices(segment);
            }
            else
            {
                var segment = new Vector3[vertices.Length - i];
                System.Array.Copy(vertices, i, segment, 0, vertices.Length - i);
                RPC_SyncTrackVertices(segment);
            }
        }

        RPC_FinishTrackVertices();
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    void RPC_SyncTrackVertices(Vector3[] vertices)
    {
        if (vertices.Length <= 0) { return; }

        temporaryVertices.AddRange(vertices);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.Proxies)]
    void RPC_FinishTrackVertices()
    {
        var vertices = temporaryVertices.ToArray();

        Track.Vertices = vertices;
        trackVisible.GenerateTrackVertices(vertices);
        trackVisible.GenerateTrackLine(vertices);

        temporaryVertices.Clear();
        NotifyTrackChanged();
    }

    int expansionLevel;
    public void ExpandTrack()
    {
        expansionLevel++;
        // var factor = Mathf.FloorToInt(expansionLevel * Mathf.Pow(2, expansionLevel - 1));
        var factor = expansionLevel;
        CreateTrack(vertexCount * factor, horizontalRadius * factor, verticalRadius * factor, noise);
        var noiseCount = UnityEngine.Random.Range(2, noiseVertexCount);
        for (int i = 0; i < noiseCount; i++)
        {
            var randomIndex = UnityEngine.Random.Range(0, Track.Vertices.Length);
            var intensity = UnityEngine.Random.Range(1f / noiseIntensity, 1f * noiseIntensity);
            Track.Vertices[randomIndex] *= intensity;
        }

        for (int itr = 0; itr < smoothingIteration; itr++)
        {
            var temporaryVertices = new List<Vector3>();
            for (int i = 0; i < Track.Vertices.Length; i++)
            {
                var prevVertex = Track.Vertices[(i - 1 + Track.Vertices.Length) % Track.Vertices.Length];
                var currentVertex = Track.Vertices[i];
                var nextVertex = Track.Vertices[(i + 1) % Track.Vertices.Length];
                var prevDistance = prevVertex.magnitude;
                var currentDistance = currentVertex.magnitude;
                var nextDistance = nextVertex.magnitude;
                var averageDistance = (prevDistance + currentDistance + nextDistance) / 3f;
                var direction = currentVertex.normalized;
                var newVertex = direction * averageDistance;
                temporaryVertices.Add(newVertex);
            }
            Track.Vertices = temporaryVertices.ToArray();
        }

        trackVisible.GenerateTrackVertices(Track.Vertices);
        trackVisible.GenerateTrackLine(Track.Vertices);

        if (Object != null && Object.HasStateAuthority)
        {
            SyncTrack(Track.Vertices);
            NotifyTrackChanged();
        }

        if (!Object.HasStateAuthority)
        {
            RequestSyncTrack();
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            ExpandTrack();
        }
    }

    void NotifyTrackChanged()
    {
        OnTrackChanged?.Invoke(Track?.Vertices, this, this);
    }
}
