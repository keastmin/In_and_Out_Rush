using UnityEngine;

public class LocalMonster : MonoBehaviour, IMonster, IDamageable
{
    [SerializeField] protected Transform attackTargetTransform;
    [SerializeField] protected float health = 10;
    [SerializeField] protected float movementSpeed = 3f;
    [SerializeField] protected float arrivalThreshold = 0.1f;

    protected Territory territory;
    [SerializeField] protected Transform playerTransform;

    public void SetTerritory(Territory territory) => this.territory = territory;
    public void SetPlayerTransform(Transform playerTransform) => this.playerTransform = playerTransform;
    public Transform GetAttackTargetTransform() => attackTargetTransform;

    public virtual void Initialize() { }

    public void TakeDamage(float damage)
    {
        health -= damage;
        if (health <= 0)
        {
            DestroyMonster();
        }
    }

    public virtual void DestroyMonster() => Destroy(gameObject);

    protected virtual void Update() => UpdateMonster();
    public virtual void UpdateMonster() =>  throw new System.NotImplementedException();

    public void OnTerritoryExpanded(Territory territory, LocalTerritorySystem territorySystem)
    {
        var xzPosition = new Vector3(transform.position.x, transform.position.z);
        if (territory.IsPointInPolygon(xzPosition))
        {
            territorySystem.OnTerritoryExpandedEvent -= OnTerritoryExpanded;
            DestroyMonster();
        }
    }
}