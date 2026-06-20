using UnityEngine;

public interface IItemDestructibleProjectile
{
    Transform ProjectileTransform { get; }
    void DestroyByItemEffect();
}
