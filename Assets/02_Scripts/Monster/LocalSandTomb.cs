using UnityEngine;

public class LocalSandTomb : LocalWorldMonster
{
    [SerializeField] protected float suckedIntoSpeed = 2f;
    [SerializeField] protected float suckedIntoRadius = 5f;
    [SerializeField] protected float attackSpeed = 5f;

    float attackElapsedTime = 0f;

    protected override void Update()
    {
        var isInRange = SuckIntoSandTomb();
        if (isInRange) Attack();
    }

    bool SuckIntoSandTomb()
    {
        if (playerTransform == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTransform.position);
        if (distanceToPlayer <= suckedIntoRadius)
        {
            Vector3 direction = (transform.position - playerTransform.position).normalized;
            playerTransform.position += suckedIntoSpeed * Time.deltaTime * direction;
            return true;
        }
        return false;
    }

    void Attack()
    {
        attackElapsedTime += Time.deltaTime * attackSpeed;
        if (attackElapsedTime >= 1f)
        {
            playerTransform.GetComponent<LocalRunner>().Health -= 1;
            Debug.Log($"{name} attacks {playerTransform.name}");
            attackElapsedTime = 0f;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, suckedIntoRadius);
    }
}