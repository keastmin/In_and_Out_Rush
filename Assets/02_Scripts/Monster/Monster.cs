using Fusion;
using UnityEngine;

public class Monster : NetworkBehaviour, IMonster, IDamageable
{
    [SerializeField] protected Transform attackTargetTransform;
    [SerializeField] protected float health = 10;
    [SerializeField] protected float movementSpeed = 3f;
    [SerializeField] protected float arrivalThreshold = 0.1f;

    [Networked] protected float Health { get; private set; }

    protected Territory territory;
    [SerializeField] protected Transform playerTransform;
    protected Rigidbody rigidBody;

    public override void Spawned()
    {
        base.Spawned();
        if (Object.HasStateAuthority)
        {
            Health = health;
            rigidBody = GetComponent<Rigidbody>();
            Initialize();
        }
    }

    public void SetTerritory(Territory territory) => this.territory = territory;
    public void SetPlayerTransform(Transform playerTransform) => this.playerTransform = playerTransform;
    public Transform GetAttackTargetTransform() => attackTargetTransform;

    public virtual void Initialize() { }

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

    public virtual void DestroyMonster() => Runner.Despawn(Object);

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) { return; }
        UpdateMonster();
    }

    public virtual void UpdateMonster() => throw new System.NotImplementedException();

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