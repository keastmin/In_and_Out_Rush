using UnityEngine;

public class Stalker : WorldMonster
{
    [Header("Stalker Settings")]
    [SerializeField] private float _sensingRange = 5f;
    [SerializeField] private float _attackRange = 2f;
    [SerializeField] private float _attackSpeed = 1f;

    private bool _isChasing;
    private float _attackElapsedTime = 0f;

    public override void UpdateMonster()
    {
        if (_isChasing)
        {
            Chase();
            if (Vector3.Distance(transform.position, attackTargetTransform.position) < _attackRange)
            {
                Attack();
            }
        }
        else
        {
            base.UpdateMonster();

            if (playerTransform == null) { return; }
            if (Vector3.Distance(transform.position, playerTransform.position) < _sensingRange)
            {
                StartChasing(playerTransform);
            }
        }
    }

    void Attack()
    {
        _attackElapsedTime += Time.deltaTime * _attackSpeed;
        if (_attackElapsedTime >= 1f)
        {
            playerTransform.GetComponent<IDamageable>()?.TakeDamage(1f);
            Debug.Log($"{name} attacks {playerTransform.name}");
            _attackElapsedTime = 0f;
        }
    }

    public void StartChasing(Transform target)
    {
        attackTargetTransform = target;
        _isChasing = true;
    }

    protected virtual void Chase()
    {
        if (attackTargetTransform != null)
        {
            var attackTargetPosition = attackTargetTransform.position;
            var attackTargetPosition2d = new Vector2(attackTargetPosition.x, attackTargetPosition.z);
            if (territory.IsPointInPolygon(attackTargetPosition2d))
            {
                _isChasing = false;
                return;
            }
            Vector3 direction = (attackTargetTransform.position - transform.position).normalized;
            transform.position += movementSpeed * Time.deltaTime * direction;
            transform.LookAt(attackTargetTransform);
        }
    }
}