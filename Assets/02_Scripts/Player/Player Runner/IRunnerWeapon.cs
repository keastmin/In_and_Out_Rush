using System;
using ProjectIO.RunnerWeapons;
using UnityEngine;

public interface IRunnerWeapon
{
    RunnerWeaponStatus Status { get; }

    event Action<RunnerWeaponStatus> StatusChanged;
    event Action<RunnerWeaponHand, Vector3> ShotPresented;

    void TryFire(PlayerRunner owner, Vector3 targetPosition, bool isRunning);
    void TryReload(PlayerRunner owner);
}
