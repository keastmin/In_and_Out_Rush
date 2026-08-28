using Fusion;
using ProjectIO.RunnerWeapons;
using UnityEngine;

public sealed class AssaultRifleWeapon : RunnerProjectileWeapon
{
    [Header("Assault Rifle Accuracy")]
    [SerializeField, Min(0f)] private float _runningSpreadDegrees = 8f;

    [Networked] private NetworkRNG SpreadRandom { get; set; }

    public override void Spawned()
    {
        base.Spawned();

        if (!HasStateAuthority)
            return;

        int seed = unchecked((Runner.Tick.Raw * 397) ^ Object.InputAuthority.RawEncoded);
        SpreadRandom = new NetworkRNG(seed == 0 ? 1 : seed);
    }

    protected override Vector3 ModifyShotDirection(
        PlayerRunner owner,
        Vector3 directShotDirection,
        bool isRunning)
    {
        if (!isRunning || _runningSpreadDegrees <= 0f)
            return directShotDirection;

        NetworkRNG random = SpreadRandom;
        float spreadDegrees = RunnerWeaponRules.GetRunningSpreadDegrees(
            true,
            random.NextSingle(),
            random.NextSingle(),
            _runningSpreadDegrees);
        SpreadRandom = random;

        return Quaternion.AngleAxis(spreadDegrees, Vector3.up) * directShotDirection;
    }
}
