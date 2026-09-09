namespace ProjectIO.RunnerWeapons
{
    public readonly struct RunnerWeaponStatus
    {
        public RunnerWeaponStatus(
            int ammunition,
            int magazineCapacity,
            bool isReloading,
            float reloadProgress01)
        {
            Ammunition = ammunition;
            MagazineCapacity = magazineCapacity;
            IsReloading = isReloading;
            ReloadProgress01 = reloadProgress01;
        }

        public int Ammunition { get; }
        public int MagazineCapacity { get; }
        public bool IsReloading { get; }
        public float ReloadProgress01 { get; }
    }
}
