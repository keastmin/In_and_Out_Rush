using Fusion;
using UnityEngine;

public class Monster : NetworkBehaviour, IMonster, IDamageable
{
    [SerializeField] protected Transform attackTargetTransform;
    [SerializeField] protected float health = 10;
    [SerializeField] protected float movementSpeed = 3f;
    [SerializeField] protected float arrivalThreshold = 0.1f;

    [Networked] protected float Health { get; private set; }
    [Networked] private TickTimer StunTimer { get; set; }
    protected float maxHealth;

    protected Territory territory;
    [SerializeField] protected Transform playerTransform;
    protected Rigidbody rigidBody;

    public bool CanAccessNetworkState => Object != null && Object.IsValid && Object.IsInSimulation;
    public float CurrentHealth => CanAccessNetworkState ? Health : 0f;
    public float MaxHealth => maxHealth > 0f ? maxHealth : health;

    public bool TryGetHealthSnapshot(out float currentHealth, out float maximumHealth)
    {
        maximumHealth = MaxHealth;

        if (!CanAccessNetworkState)
        {
            currentHealth = 0f;
            return false;
        }

        currentHealth = Health;
        return true;
    }

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority)
        {
            maxHealth = health;
            Health = maxHealth;
            rigidBody = GetComponent<Rigidbody>();
            Initialize();
        }
    }

    public void SetTerritory(Territory territory) => this.territory = territory;
    public void SetPlayerTransform(Transform playerTransform) => this.playerTransform = playerTransform;
    public Transform GetAttackTargetTransform() => attackTargetTransform;

    public virtual void Initialize() { }

    public virtual void ApplyStatMultiplier(float multiplier)
    {
        if (!CanAccessNetworkState || !Object.HasStateAuthority)
            return;

        float previousMaxHealth = maxHealth > 0f ? maxHealth : health;
        maxHealth = previousMaxHealth * multiplier;
        Health *= multiplier;
        movementSpeed *= multiplier;
    }

    public void TakeDamage(float damage)
    {
        if (CanAccessNetworkState && Object.HasStateAuthority)
        {
            Health -= damage;
            if (Health <= 0)
            {
                DestroyMonster();
            }
        }
    }

    public void ApplyStun(float duration)
    {
        if (!CanAccessNetworkState || !Object.HasStateAuthority || duration <= 0f)
            return;

        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        StopByStun();
    }

    public virtual void DestroyMonster() => Runner.Despawn(Object);

    public override void FixedUpdateNetwork()
    {
        if (!CanAccessNetworkState || !Object.HasStateAuthority) { return; }
        if (IsStunned)
        {
            StopByStun();
            return;
        }

        UpdateMonster();
    }

    public virtual void UpdateMonster() => throw new System.NotImplementedException();

    protected bool IsStunned => StunTimer.IsRunning && !StunTimer.Expired(Runner);

    protected virtual void StopByStun()
    {
        StopMovement();
    }

    protected virtual void StopMovement()
    {
        if (rigidBody != null)
            rigidBody.linearVelocity = Vector3.zero;
    }

    protected bool IsPositionInRunnerSafeZone(Vector3 position)
    {
        Vector2 position2d = new(position.x, position.z);
        return IsPositionInTerritory(position2d) || IsPositionInActiveSanctuary(position);
    }

    protected bool IsPositionInRunnerSafeZone(Vector2 position)
        => IsPositionInTerritory(position) ||
           IsPositionInActiveSanctuary(new Vector3(position.x, transform.position.y, position.y));

    protected bool IsTargetInRunnerSafeZone(Transform target)
        => target != null && IsPositionInRunnerSafeZone(target.position);

    private bool IsPositionInTerritory(Vector2 position)
        => territory != null && territory.IsPointInPolygon(position);

    private static bool IsPositionInActiveSanctuary(Vector3 position)
        => Dev.Network.StageBootstrapper.Instance != null &&
           Dev.Network.StageBootstrapper.Instance.IsPointInActiveSanctuary(position);

    public void OnTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
    {
        var xzPosition = new Vector3(transform.position.x, transform.position.z);
        if (territory.IsPointInPolygon(xzPosition))
        {
            territorySystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
            DestroyMonster();
        }
    }
}
