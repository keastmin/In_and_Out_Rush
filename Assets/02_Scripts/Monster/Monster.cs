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
        if (!Object.HasStateAuthority)
            return;

        float previousMaxHealth = maxHealth > 0f ? maxHealth : health;
        maxHealth = previousMaxHealth * multiplier;
        Health *= multiplier;
        movementSpeed *= multiplier;
    }

    public void TakeDamage(float damage)
    {
        if (Object.HasStateAuthority)
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
        if (!Object.HasStateAuthority || duration <= 0f)
            return;

        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        StopByStun();
    }

    public virtual void DestroyMonster() => Runner.Despawn(Object);

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) { return; }
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
        if (rigidBody != null)
            rigidBody.linearVelocity = Vector3.zero;
    }

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
