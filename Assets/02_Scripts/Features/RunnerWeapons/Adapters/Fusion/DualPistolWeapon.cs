using ProjectIO.RunnerWeapons;
using UnityEngine;

public sealed class DualPistolWeapon : RunnerProjectileWeapon
{
    [Header("Dual Pistol Muzzles")]
    [SerializeField] private Transform _leftMuzzle;
    [SerializeField] private Transform _rightMuzzle;

    protected override RunnerWeaponHand ResolveShotHand(int shotSequence)
    {
        return RunnerWeaponRules.GetAlternatingHand(shotSequence);
    }

    protected override Transform ResolveMuzzle(RunnerWeaponHand hand)
    {
        Transform muzzle = hand == RunnerWeaponHand.Left
            ? _leftMuzzle
            : _rightMuzzle;

        return muzzle != null ? muzzle : base.ResolveMuzzle(hand);
    }
}
