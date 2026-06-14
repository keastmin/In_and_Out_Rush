using UnityEngine;

public interface IBarrierDestructibleProjectile
{
    Transform ProjectileTransform { get; }
    void DestroyByBarrier();
}
