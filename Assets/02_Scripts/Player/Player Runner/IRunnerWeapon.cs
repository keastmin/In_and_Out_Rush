using UnityEngine;

public interface IRunnerWeapon
{
    void TryFire(PlayerRunner owner, Vector3 targetPosition);
}
