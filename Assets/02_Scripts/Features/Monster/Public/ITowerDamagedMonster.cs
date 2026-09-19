namespace ProjectIO.Monsters
{
    public interface ITowerDamagedMonster
    {
        int Priority { get; }
        void TakeTowerDamage(float damage);
    }
}
