using UnityEngine;

public class Stalker : WorldMonster
{
    [SerializeField] protected float sensingRange = 5f;
    [SerializeField] protected float attackRange = 2f;
    [SerializeField] protected float attackSpeed = 1f;

    protected bool isChasing;
    float attackElapsedTime = 0f;

    public override void UpdateMonster()
    {
        if (isChasing)
        {
            Chase();
            if (Vector3.Distance(transform.position, attackTargetTransform.position) < attackRange)
            {
                Attack();
            }
        }
        else
        {
            base.UpdateMonster();

            if (playerTransform == null) { return; }
            if (Vector3.Distance(transform.position, playerTransform.position) < sensingRange)
            {
                StartChasing(playerTransform);
            }
        }
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

    public void StartChasing(Transform target)
    {
        attackTargetTransform = target;
        isChasing = true;
    }

    protected virtual void Chase()
    {
        if (attackTargetTransform != null)
        {
            Vector3 direction = (attackTargetTransform.position - transform.position).normalized;
            transform.position += movementSpeed * Time.deltaTime * direction;
            transform.LookAt(attackTargetTransform);
        }
    }
}