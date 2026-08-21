using Fusion;
using UnityEngine;

public class SandTomb : WorldMonster
{
    private const string ActivationRangeName = "Activation Range";
    private const int ActivationRangeSegments = 64;
    private const float MaxHealthDamageRate = 0.10f;
    private const float CurrentHealthDamageRate = 0.30f;

    public enum SandTombState
    {
        Inactive,
        Active,
    }

    public float SpawnExclusionRadius =>
        Mathf.Max(Mathf.Max(0f, _activationRadius), Mathf.Max(0f, _suckedIntoRadius));

    [Header("Sand Tomb Settings")]
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _inactiveMaterial;
    [SerializeField] private float _activationRadius = 4f;
    [SerializeField] private float _explosionDelay = 4f;
    [SerializeField] private float _suckedIntoSpeed = 2f;
    [SerializeField] private float _suckedIntoRadius = 5f;

    [Networked, OnChangedRender(nameof(ApplyStateVisual))]
    private SandTombState State { get; set; }
    [Networked] private TickTimer ExplosionTimer { get; set; }

    private bool _isExpansionPathSuspended;
    private Mesh _activationRangeMesh;

    private void Awake()
    {
        ApplyActivationRangeVisual();
    }

    public override void Spawned()
    {
        base.Spawned();
        ApplyActivationRangeVisual();
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
        if (_meshRenderer == null) return;

        Material material = State == SandTombState.Active
            ? _activeMaterial
            : _inactiveMaterial;

        if (material != null)
            _meshRenderer.sharedMaterial = material;
    }

    private void ApplyActivationRangeVisual()
    {
        MeshFilter meshFilter = FindActivationRangeMeshFilter();
        if (meshFilter == null) return;

        UpdateActivationRangeMesh(Mathf.Max(0f, _activationRadius));
        meshFilter.sharedMesh = _activationRangeMesh;

        MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
        if (meshCollider != null)
            meshCollider.sharedMesh = _activationRangeMesh;
    }

    private MeshFilter FindActivationRangeMeshFilter()
    {
        Transform activationRange = transform.Find(ActivationRangeName);
        return activationRange != null
            ? activationRange.GetComponent<MeshFilter>()
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
        if (_activationRangeMesh == null) return;

        if (Application.isPlaying)
            Destroy(_activationRangeMesh);
        else
            DestroyImmediate(_activationRangeMesh);
    }
}
