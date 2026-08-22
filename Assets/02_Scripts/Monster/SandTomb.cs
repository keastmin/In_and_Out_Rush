using Fusion;
using UnityEngine;

public class SandTomb : WorldMonster
{
    private const string ActivationRangeName = "Activation Range";
    private const string SuckedIntoRangeName = "Sucked Into Range";
    private const int ActivationRangeSegments = 64;
    private const float MaxHealthDamageRate = 0.10f;
    private const float CurrentHealthDamageRate = 0.30f;
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");

    public enum SandTombState
    {
        Inactive,
        Active,
    }

    public float SpawnExclusionRadius =>
        Mathf.Max(Mathf.Max(0f, _activationRadius), Mathf.Max(0f, _suckedIntoRadius));

    [Header("Sand Tomb Settings")]
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private MeshRenderer _suckedIntoMeshRenderer;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _inactiveMaterial;
    [SerializeField] private float _activationRadius = 4f;
    [SerializeField, Range(0f, 1f)] private float _activationRangeAlpha = 0.22f;
    [SerializeField] private float _explosionDelay = 4f;
    [SerializeField] private float _suckedIntoSpeed = 2f;
    [SerializeField] private float _suckedIntoRadius = 5f;
    [SerializeField, Range(0f, 1f)] private float _suckedIntoRangeAlpha = 0.12f;

    [Networked, OnChangedRender(nameof(ApplyStateVisual))]
    private SandTombState State { get; set; }
    [Networked] private TickTimer ExplosionTimer { get; set; }

    private bool _isExpansionPathSuspended;
    private Mesh _activationRangeMesh;
    private Mesh _suckedIntoRangeMesh;
    private MaterialPropertyBlock _activationRangePropertyBlock;
    private MaterialPropertyBlock _suckedIntoRangePropertyBlock;

    private void Awake()
    {
        ApplyRangeVisuals();
    }

    public override void Spawned()
    {
        base.Spawned();
        ApplyRangeVisuals();
        ApplyStateVisual();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority || playerTransform == null) return;

        float distanceToPlayer = Vector3.Distance(RigidbodyPosition, playerTransform.position);
        if (State == SandTombState.Inactive)
        {
            if (!IsStunned && distanceToPlayer <= _activationRadius)
                Activate();

            return;
        }

        if (ExplosionTimer.Expired(Runner))
        {
            ResumeExpansionPath();
            Explode(distanceToPlayer);
            DestroyMonster();
            return;
        }

        if (IsStunned || IsPlayerInTerritory())
        {
            ResumeExpansionPath();
            return;
        }

        if (IsPlayerOutOfSuckedIntoRadius(distanceToPlayer))
        {
            ResumeExpansionPath();
            return;
        }

        PauseExpansionPath();
        SuckIntoSandTomb();
    }

    public override void DestroyMonster()
    {
        ResumeExpansionPath();
        base.DestroyMonster();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (runner != null && runner.IsServer)
            ResumeExpansionPath();

        base.Despawned(runner, hasState);
    }

    private void Activate()
    {
        Debug.Log($"{name} is activated by {playerTransform.name}");
        State = SandTombState.Active;
        ExplosionTimer = TickTimer.CreateFromSeconds(Runner, Mathf.Max(0f, _explosionDelay));
    }

    private void PauseExpansionPath()
    {
        if (_isExpansionPathSuspended || territoryExpansionSystem == null)
            return;

        _isExpansionPathSuspended = territoryExpansionSystem.TryPauseExpandingPath(playerTransform.position);
    }

    private void ResumeExpansionPath()
    {
        if (!_isExpansionPathSuspended)
            return;

        territoryExpansionSystem?.ResumePausedExpandingPath(
            playerTransform.position,
            playerTransform.GetComponent<PlayerRunner>());
        _isExpansionPathSuspended = false;
    }

    private void Explode(float distanceToPlayer)
    {
        if (IsPlayerInTerritory() || IsPlayerOutOfSuckedIntoRadius(distanceToPlayer))
            return;

        PlayerRunner runner = playerTransform.GetComponent<PlayerRunner>();
        if (runner == null)
            return;

        float damage = runner.MaxHealth * MaxHealthDamageRate + runner.Health * CurrentHealthDamageRate;
        runner.TakeDamage(damage);
        Debug.Log($"{name} exploded on {runner.name} for {damage:F2} damage.");
    }

    private void ApplyStateVisual()
    {
        _activationRangePropertyBlock ??= new MaterialPropertyBlock();
        _suckedIntoRangePropertyBlock ??= new MaterialPropertyBlock();

        Material material = State == SandTombState.Active
            ? _activeMaterial
            : _inactiveMaterial;

        ApplyStateVisual(
            _meshRenderer,
            _activationRangePropertyBlock,
            material,
            _activationRangeAlpha);
        ApplyStateVisual(
            _suckedIntoMeshRenderer,
            _suckedIntoRangePropertyBlock,
            material,
            _suckedIntoRangeAlpha);
    }

    private static void ApplyStateVisual(
        MeshRenderer meshRenderer,
        MaterialPropertyBlock propertyBlock,
        Material material,
        float alpha)
    {
        if (meshRenderer == null) return;

        if (material != null)
            meshRenderer.sharedMaterial = material;

        Material appliedMaterial = meshRenderer.sharedMaterial;
        if (appliedMaterial == null) return;

        Color color = appliedMaterial.HasProperty(BaseColorProperty)
            ? appliedMaterial.GetColor(BaseColorProperty)
            : Color.white;
        color.a = Mathf.Clamp01(alpha);

        propertyBlock.Clear();
        propertyBlock.SetColor(BaseColorProperty, color);
        meshRenderer.SetPropertyBlock(propertyBlock);
    }

    private void ApplyRangeVisuals()
    {
        ApplyActivationRangeVisual();
        ApplySuckedIntoRangeVisual();
    }

    private void ApplyActivationRangeVisual()
    {
        MeshFilter meshFilter = FindRangeMeshFilter(ActivationRangeName);
        if (meshFilter == null) return;

        UpdateActivationRangeMesh(Mathf.Max(0f, _activationRadius));
        meshFilter.sharedMesh = _activationRangeMesh;

        MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
        if (meshCollider != null)
            meshCollider.sharedMesh = _activationRangeMesh;
    }

    private void ApplySuckedIntoRangeVisual()
    {
        MeshFilter meshFilter = FindRangeMeshFilter(SuckedIntoRangeName);
        if (meshFilter == null) return;

        float activationRadius = Mathf.Max(0f, _activationRadius);
        float suckedIntoRadius = Mathf.Max(activationRadius, _suckedIntoRadius);
        UpdateSuckedIntoRangeMesh(activationRadius, suckedIntoRadius);
        meshFilter.sharedMesh = _suckedIntoRangeMesh;
    }

    private MeshFilter FindRangeMeshFilter(string rangeName)
    {
        Transform range = transform.Find(rangeName);
        return range != null
            ? range.GetComponent<MeshFilter>()
            : null;
    }

    private void UpdateActivationRangeMesh(float radius)
    {
        if (_activationRangeMesh == null)
        {
            _activationRangeMesh = new Mesh
            {
                name = "SandTomb Activation Range",
            };
        }

        Vector3[] vertices = new Vector3[ActivationRangeSegments + 1];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[ActivationRangeSegments * 3];

        vertices[0] = Vector3.zero;
        normals[0] = Vector3.back;
        uvs[0] = new Vector2(0.5f, 0.5f);

        for (int i = 0; i < ActivationRangeSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ActivationRangeSegments;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            int vertexIndex = i + 1;

            vertices[vertexIndex] = new Vector3(x * radius, y * radius, 0f);
            normals[vertexIndex] = Vector3.back;
            uvs[vertexIndex] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);

            int triangleIndex = i * 3;
            triangles[triangleIndex] = 0;
            triangles[triangleIndex + 1] = i == ActivationRangeSegments - 1 ? 1 : vertexIndex + 1;
            triangles[triangleIndex + 2] = vertexIndex;
        }

        _activationRangeMesh.Clear();
        _activationRangeMesh.vertices = vertices;
        _activationRangeMesh.normals = normals;
        _activationRangeMesh.uv = uvs;
        _activationRangeMesh.triangles = triangles;
        _activationRangeMesh.RecalculateBounds();
    }

    private void UpdateSuckedIntoRangeMesh(float innerRadius, float outerRadius)
    {
        if (_suckedIntoRangeMesh == null)
        {
            _suckedIntoRangeMesh = new Mesh
            {
                name = "SandTomb Sucked Into Range",
            };
        }

        Vector3[] vertices = new Vector3[ActivationRangeSegments * 2];
        Vector3[] normals = new Vector3[vertices.Length];
        Vector2[] uvs = new Vector2[vertices.Length];
        int[] triangles = new int[ActivationRangeSegments * 6];
        float uvScale = outerRadius > 0f ? 1f / outerRadius : 0f;

        for (int i = 0; i < ActivationRangeSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / ActivationRangeSegments;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);
            int innerVertexIndex = i * 2;
            int outerVertexIndex = innerVertexIndex + 1;

            vertices[innerVertexIndex] = new Vector3(x * innerRadius, y * innerRadius, 0f);
            vertices[outerVertexIndex] = new Vector3(x * outerRadius, y * outerRadius, 0f);
            normals[innerVertexIndex] = Vector3.back;
            normals[outerVertexIndex] = Vector3.back;
            uvs[innerVertexIndex] = new Vector2(
                x * innerRadius * uvScale * 0.5f + 0.5f,
                y * innerRadius * uvScale * 0.5f + 0.5f);
            uvs[outerVertexIndex] = new Vector2(x * 0.5f + 0.5f, y * 0.5f + 0.5f);

            int nextIndex = (i + 1) % ActivationRangeSegments;
            int nextInnerVertexIndex = nextIndex * 2;
            int nextOuterVertexIndex = nextInnerVertexIndex + 1;
            int triangleIndex = i * 6;

            triangles[triangleIndex] = innerVertexIndex;
            triangles[triangleIndex + 1] = nextOuterVertexIndex;
            triangles[triangleIndex + 2] = outerVertexIndex;
            triangles[triangleIndex + 3] = innerVertexIndex;
            triangles[triangleIndex + 4] = nextInnerVertexIndex;
            triangles[triangleIndex + 5] = nextOuterVertexIndex;
        }

        _suckedIntoRangeMesh.Clear();
        _suckedIntoRangeMesh.vertices = vertices;
        _suckedIntoRangeMesh.normals = normals;
        _suckedIntoRangeMesh.uv = uvs;
        _suckedIntoRangeMesh.triangles = triangles;
        _suckedIntoRangeMesh.RecalculateBounds();
    }

    private bool IsPlayerInTerritory()
    {
        var playerPosition2d = new Vector2(playerTransform.position.x, playerTransform.position.z);
        return territory.IsPointInPolygon(playerPosition2d);
    }

    private bool IsPlayerOutOfSuckedIntoRadius(float distanceToPlayer)
        => distanceToPlayer > _suckedIntoRadius;

    private void SuckIntoSandTomb()
    {
        Vector3 direction = (transform.position - playerTransform.position).normalized;
        playerTransform.position += _suckedIntoSpeed * Runner.DeltaTime * direction;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _activationRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _suckedIntoRadius);
    }

    private void OnDestroy()
    {
        DestroyMesh(ref _activationRangeMesh);
        DestroyMesh(ref _suckedIntoRangeMesh);
    }

    private static void DestroyMesh(ref Mesh mesh)
    {
        if (mesh == null) return;

        if (Application.isPlaying)
            UnityEngine.Object.Destroy(mesh);
        else
            UnityEngine.Object.DestroyImmediate(mesh);

        mesh = null;
    }
}
