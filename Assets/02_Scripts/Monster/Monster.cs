using Fusion;
using Unity.Profiling;
using UnityEngine;

public class Monster : NetworkBehaviour, IMonster, IDamageable
{
    private static readonly ProfilerMarker FixedUpdateMarker = new("Monster.FixedUpdateNetwork");

    [SerializeField] protected Transform attackTargetTransform;
    [SerializeField] protected float health = 10;
    [SerializeField] protected float movementSpeed = 3f;
    [SerializeField] protected float arrivalThreshold = 0.1f;

    [Networked] protected float Health { get; private set; }
    [Networked] private TickTimer StunTimer { get; set; }
    protected float maxHealth;

    protected Territory territory;
    private TerritorySystem territoryExpansionSystem;
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
        rigidBody = GetComponent<Rigidbody>();

        if (Object.HasStateAuthority)
        {
            maxHealth = health;
            Health = maxHealth;
            Initialize();
        }
    }

    public void SetTerritory(Territory territory) => this.territory = territory;
    public void SetPlayerTransform(Transform playerTransform) => this.playerTransform = playerTransform;
    public Transform GetAttackTargetTransform() => attackTargetTransform;

    public void RegisterTerritoryExpansion(TerritorySystem territorySystem)
    {
        UnregisterTerritoryExpansion();

        if (territorySystem == null)
            return;

        territoryExpansionSystem = territorySystem;
        territoryExpansionSystem.OnTerritoryExpandedEvent += OnTerritoryExpanded;
    }

    private void UnregisterTerritoryExpansion()
    {
        if (territoryExpansionSystem == null)
            return;

        territoryExpansionSystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
        territoryExpansionSystem = null;
    }

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

    public virtual void DestroyMonster()
    {
        UnregisterTerritoryExpansion();

        if (Runner != null && Object != null && Object.IsValid)
            Runner.Despawn(Object);
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        UnregisterTerritoryExpansion();
        base.Despawned(runner, hasState);
    }

    public override void FixedUpdateNetwork()
    {
        using (FixedUpdateMarker.Auto())
        {
            if (!CanAccessNetworkState || !Object.HasStateAuthority) { return; }

            if (IsStunned)
            {
                StopByStun();
                return;
            }

            UpdateMonster();
        }
    }

    public virtual void UpdateMonster() => throw new System.NotImplementedException();

    protected bool IsStunned => StunTimer.IsRunning && !StunTimer.Expired(Runner);

    protected virtual void StopByStun()
    {
        StopMovement();
    }

    protected virtual void StopMovement()
    {
        if (rigidBody == null)
            return;

        rigidBody.linearVelocity = Vector3.zero;
        rigidBody.angularVelocity = Vector3.zero;
    }

    protected Vector3 RigidbodyPosition
        => rigidBody != null ? rigidBody.position : transform.position;

    protected void SetMovementVelocity(Vector3 velocity)
    {
        if (rigidBody != null)
            rigidBody.linearVelocity = velocity;
    }

    protected void SetRigidbodyRotation(Quaternion rotation)
    {
        if (rigidBody != null)
            rigidBody.rotation = rotation;
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

    protected bool IsPositionOutsideTerritory(Vector3 position)
        => territory != null && !IsPositionInTerritory(new Vector2(position.x, position.z));

    protected bool IsPositionInTerritory(Vector2 position)
        => territory != null && territory.IsPointInPolygon(position);

    protected virtual bool ShouldDestroyInsideTerritory => false;

    private bool TryDestroyInsideTerritory()
    {
        if (!ShouldDestroyInsideTerritory)
            return false;

        var xzPosition = new Vector2(RigidbodyPosition.x, RigidbodyPosition.z);
        if (!IsPositionInTerritory(xzPosition))
            return false;

        DestroyMonster();
        return true;
    }

    private static bool IsPositionInActiveSanctuary(Vector3 position)
        => Dev.Network.StageBootstrapper.Instance != null &&
           Dev.Network.StageBootstrapper.Instance.IsPointInActiveSanctuary(position);

    public void OnTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
    {
        if (!CanAccessNetworkState)
        {
            UnregisterTerritoryExpansion();
            return;
        }

        var xzPosition = new Vector2(RigidbodyPosition.x, RigidbodyPosition.z);
        if (territory.IsPointInPolygon(xzPosition))
        {
            DestroyMonster();
        }
    }
}
