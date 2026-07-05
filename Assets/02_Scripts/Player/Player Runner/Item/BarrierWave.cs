using System.Collections.Generic;
using Fusion;
using KIM.Dev;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class BarrierWave : NetworkBehaviour
{
    private const float DefaultCellSize = 1.6f;
    private const string DomeObjectName = "Barrier Dome";

    [Header("Expansion")]
    [SerializeField, Min(0.01f)] private float duration = 2f;

    [Header("Size")]
    [SerializeField, Min(0f)] private float initialDiameterInTiles = 1f;
    [SerializeField, Min(0f)] private float finalDiameterInTiles = 6f;

    [Header("Dome")]
    [SerializeField, Range(8, 128)] private int domeHorizontalSegments = 48;
    [SerializeField, Range(4, 32)] private int domeVerticalSegments = 10;
    [SerializeField] private Color domeColor = new Color(0.25f, 0.75f, 1f, 0.25f);

    [Networked] private float CurrentDiameter { get; set; }

    private readonly List<IItemDestructibleProjectile> _projectileBuffer = new();
    private Transform _domeTransform;
    private Mesh _domeMesh;
    private Material _domeMaterial;
    private TickTimer _lifeTimer;
    private float _cellSize;
    private float _elapsedTime;
    private bool _initialized;

    public override void Spawned()
    {
        ConfigureDomeRenderer();

        if (!HasStateAuthority)
            return;

        _cellSize = ResolveCellSize();
        CurrentDiameter = initialDiameterInTiles * _cellSize;
    }

    public void Initialize()
    {
        if (!HasStateAuthority)
            return;

        _cellSize = ResolveCellSize();
        _elapsedTime = 0f;
        CurrentDiameter = initialDiameterInTiles * _cellSize;
        _lifeTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0.01f, duration));
        _initialized = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_initialized)
            return;

        if (_lifeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        float deltaTime = Runner.DeltaTime;
        _elapsedTime += deltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / Mathf.Max(0.01f, duration));

        CurrentDiameter = Mathf.Lerp(
            initialDiameterInTiles * _cellSize,
            finalDiameterInTiles * _cellSize,
            progress);

        DestroyIntersectingProjectiles();
    }

    public override void Render()
    {
        DrawDome();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        ReleaseRuntimeVisuals();
        base.Despawned(runner, hasState);
    }

    private void ConfigureDomeRenderer()
    {
        GameObject domeObject = new GameObject(DomeObjectName);
        domeObject.transform.SetParent(transform, false);
        _domeTransform = domeObject.transform;

        MeshFilter meshFilter = domeObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = domeObject.AddComponent<MeshRenderer>();

        _domeMesh = CreateDomeMesh(domeHorizontalSegments, domeVerticalSegments);
        meshFilter.sharedMesh = _domeMesh;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");

        if (shader != null)
        {
            _domeMaterial = new Material(shader)
            {
                color = domeColor
            };
            _domeMaterial.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            if (_domeMaterial.HasProperty("_Cull"))
                _domeMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);

            meshRenderer.sharedMaterial = _domeMaterial;
        }
    }

    private void DrawDome()
    {
        if (_domeTransform == null)
            return;

        float radius = CurrentDiameter * 0.5f;
        _domeTransform.localScale = new Vector3(radius, radius, radius);
    }

    private void DestroyIntersectingProjectiles()
    {
        MonsterProjectileRegistry.CopyActiveProjectilesTo(_projectileBuffer);

        float radius = CurrentDiameter * 0.5f;
        float sqrRadius = radius * radius;

        for (int i = 0; i < _projectileBuffer.Count; i++)
        {
            IItemDestructibleProjectile projectile = _projectileBuffer[i];
            if (projectile?.ProjectileTransform == null)
                continue;

            Vector3 offset = projectile.ProjectileTransform.position - transform.position;
            if (offset.sqrMagnitude <= sqrRadius)
                projectile.DestroyByItemEffect();
        }
    }

    private static float ResolveCellSize()
    {
        return InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;
    }

    private static Mesh CreateDomeMesh(int horizontalSegments, int verticalSegments)
    {
        int safeHorizontalSegments = Mathf.Max(8, horizontalSegments);
        int safeVerticalSegments = Mathf.Max(4, verticalSegments);
        int vertexCount = (safeVerticalSegments + 1) * safeHorizontalSegments;
        var vertices = new Vector3[vertexCount];
        var triangles = new int[safeVerticalSegments * safeHorizontalSegments * 6];

        for (int verticalIndex = 0; verticalIndex <= safeVerticalSegments; verticalIndex++)
        {
            float verticalProgress = verticalIndex / (float)safeVerticalSegments;
            float polarAngle = verticalProgress * Mathf.PI * 0.5f;
            float y = Mathf.Cos(polarAngle);
            float ringRadius = Mathf.Sin(polarAngle);

            for (int horizontalIndex = 0; horizontalIndex < safeHorizontalSegments; horizontalIndex++)
            {
                float angle = horizontalIndex / (float)safeHorizontalSegments * Mathf.PI * 2f;
                int vertexIndex = verticalIndex * safeHorizontalSegments + horizontalIndex;
                vertices[vertexIndex] = new Vector3(
                    Mathf.Cos(angle) * ringRadius,
                    y,
                    Mathf.Sin(angle) * ringRadius);
            }
        }

        int triangleIndex = 0;
        for (int verticalIndex = 0; verticalIndex < safeVerticalSegments; verticalIndex++)
        {
            for (int horizontalIndex = 0; horizontalIndex < safeHorizontalSegments; horizontalIndex++)
            {
                int nextHorizontalIndex = (horizontalIndex + 1) % safeHorizontalSegments;
                int current = verticalIndex * safeHorizontalSegments + horizontalIndex;
                int next = verticalIndex * safeHorizontalSegments + nextHorizontalIndex;
                int below = (verticalIndex + 1) * safeHorizontalSegments + horizontalIndex;
                int belowNext = (verticalIndex + 1) * safeHorizontalSegments + nextHorizontalIndex;

                triangles[triangleIndex++] = current;
                triangles[triangleIndex++] = below;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = next;
                triangles[triangleIndex++] = below;
                triangles[triangleIndex++] = belowNext;
            }
        }

        Mesh mesh = new Mesh
        {
            name = "Runtime Barrier Dome Mesh",
            vertices = vertices,
            triangles = triangles
        };
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    private void ReleaseRuntimeVisuals()
    {
        if (_domeMesh != null)
        {
            Destroy(_domeMesh);
            _domeMesh = null;
        }

        if (_domeMaterial != null)
        {
            Destroy(_domeMaterial);
            _domeMaterial = null;
        }
    }

    private void OnValidate()
    {
        finalDiameterInTiles = Mathf.Max(initialDiameterInTiles, finalDiameterInTiles);
    }
}
