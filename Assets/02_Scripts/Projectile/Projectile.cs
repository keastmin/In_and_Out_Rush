using Unity.VisualScripting;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] float destroyTime = 5f; // 발사체가 파괴되는 시간
    Transform targetTransform;
    float speed;
    int damage;

    void Start()
    {
        Destroy(gameObject, destroyTime); // Destroy the projectile after 5 seconds
    }

    public void SetTarget(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    public void SetSpeed(float speed)
    {
        this.speed = speed;
    }

    public void SetDamage(int damage)
    {
        this.damage = damage;
    }

    void Update()
    {
        if (targetTransform != null)
        {
            FollowTarget();
        }
        if (targetTransform.IsUnityNull())
        {
            Destroy(gameObject); // Destroy the projectile if the target is null
        }
    }

    void FollowTarget()
    {
        Vector3 direction = (targetTransform.position - transform.position).normalized;
        transform.position += Time.deltaTime * speed * direction;
        transform.LookAt(targetTransform);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<LocalMonster>(out var monster))
        {
            monster.TakeDamage(damage);
            Destroy(gameObject); // Destroy the projectile after hitting the target
        }
    }
}