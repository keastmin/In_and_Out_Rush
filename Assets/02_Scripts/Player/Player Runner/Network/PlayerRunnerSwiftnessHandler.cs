using DG.Tweening;
using UnityEngine;

public class PlayerRunnerSwiftnessHandler
{
    private bool _isSwiftnessActive;
    private int _remainingSlideCount;

    public bool IsSwiftnessActive => _isSwiftnessActive;
    public int RemainingSlideCount => _remainingSlideCount;

    public void DecrementRemainingSlideCount() => _remainingSlideCount--;

    public void StartSwiftness(PlayerRunner runner, ParticleSystem swiftnessParticleEffect)
    {
        if (_isSwiftnessActive) return;

        _isSwiftnessActive = true;
        runner.WeaponAttackSpeedScaler *= 1.5f;
        runner.WeaponReloadSpeedScaler *= 1.5f;
        _remainingSlideCount = 3;
        Debug.Log("스위프트니스 상태 시작");
        swiftnessParticleEffect.Play();

        DOTween.Sequence()
            .AppendInterval(10.0f)
            .AppendCallback(() =>
            {
                runner.WeaponAttackSpeedScaler /= 1.5f;
                runner.WeaponReloadSpeedScaler /= 1.5f;
                _isSwiftnessActive = false;
                _remainingSlideCount = 0;
                swiftnessParticleEffect.Stop();
                Debug.Log("스위프트니스 상태 종료");
            });
    }
}
