using System.Collections.Generic;

public static class MonsterProjectileRegistry
{
    private static readonly HashSet<IItemDestructibleProjectile> Projectiles = new();

    public static void Register(IItemDestructibleProjectile projectile)
    {
        if (projectile != null)
            Projectiles.Add(projectile);
    }

    public static void Unregister(IItemDestructibleProjectile projectile)
    {
        if (projectile != null)
            Projectiles.Remove(projectile);
    }

    public static void CopyActiveProjectilesTo(List<IItemDestructibleProjectile> destination)
    {
        destination.Clear();
        destination.AddRange(Projectiles);
    }
}
