using Fusion;
using UnityEngine;

public class SandTomb : WorldMonster
{
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

    public override void Spawned()
    {
        base.Spawned();
        ApplyStateVisual();
    }

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();

        if (!HasStateAuthority) return;
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
}
