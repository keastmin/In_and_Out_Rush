using System.Collections.Generic;

public static class MonsterProjectileRegistry
{
    private static readonly HashSet<IBarrierDestructibleProjectile> Projectiles = new();

    public static void Register(IBarrierDestructibleProjectile projectile)
    {
        if (projectile != null)
            Projectiles.Add(projectile);
    }

    public static void Unregister(IBarrierDestructibleProjectile projectile)
    {
        if (projectile != null)
            Projectiles.Remove(projectile);
    }

    public static void CopyActiveProjectilesTo(List<IBarrierDestructibleProjectile> destination)
    {
        destination.Clear();
        destination.AddRange(Projectiles);
    }
}
