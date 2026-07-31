using Fusion;
using UnityEngine;

public class SandTomb : WorldMonster
{
    private const string ActivationRangeName = "Activation Range";
    private const int ActivationRangeSegments = 64;

    public enum SandTombState
    {
        Inactive,
        Active,
    }

    [Header("Sand Tomb Settings")]
    [SerializeField] private MeshRenderer _meshRenderer;
    [SerializeField] private Material _activeMaterial;
    [SerializeField] private Material _inactiveMaterial;
    [SerializeField] private float _activationRadius = 4f;
    [SerializeField] private float _activationDuration = 5f;
    [SerializeField] private float _suckedIntoSpeed = 2f;
    [SerializeField] private float _suckedIntoRadius = 5f;
    [SerializeField] private float _attackSpeed = 5f;

    [Networked, OnChangedRender(nameof(ApplyStateVisual))]
    private SandTombState State { get; set; }

    private float _activationTimer = 0f;
    private float _attackElapsedTime = 0f;
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

        if (!HasStateAuthority || IsStunned) return;
        if (playerTransform == null) return;

        var distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        UpdateActivation(distanceToPlayer);

        if (State == SandTombState.Active)
        {
            if (IsPlayerInTerritory()) return;
            if (IsPlayerOutOfSuckedIntoRadius(distanceToPlayer)) return;
            SuckIntoSandTomb();
            Attack();
        }
    }
    
    private void UpdateActivation(float distanceToPlayer)
    {
        if (distanceToPlayer <= _activationRadius)
        {
            if (State == SandTombState.Inactive)
            {
                Debug.Log($"{name} is activated by {playerTransform.name}");
                State = SandTombState.Active;
                _activationTimer = 0f;
            }
        }
        else
        {
            if (State == SandTombState.Active)
            {
                _activationTimer += Runner.DeltaTime;
                if (_activationTimer >= _activationDuration)
                {
                    Debug.Log($"{name} is deactivated due to timeout");
                    State = SandTombState.Inactive;
                    _activationTimer = 0f;
                }
            }
        }
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

    private void Attack()
    {
        _attackElapsedTime += Runner.DeltaTime * _attackSpeed;
        if (_attackElapsedTime >= 1f)
        {
            playerTransform.GetComponent<IDamageable>()?.TakeDamage(1f);
            Debug.Log($"{name} attacks {playerTransform.name}");
            _attackElapsedTime = 0f;
        }
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
