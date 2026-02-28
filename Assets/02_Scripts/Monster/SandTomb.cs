using UnityEngine;

public class SandTomb : WorldMonster
{
    [SerializeField] float suckedIntoSpeed = 2f;
    [SerializeField] float suckedIntoRadius = 5f;
    [SerializeField] protected float attackSpeed = 5f;

    float attackElapsedTime = 0f;

    public override void FixedUpdateNetwork()
    {
        base.FixedUpdateNetwork();
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
            playerTransform.GetComponent<IDamageable>()?.TakeDamage(1f);
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