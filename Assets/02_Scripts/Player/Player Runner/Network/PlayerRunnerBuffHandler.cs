using System.Collections.Generic;
using UnityEngine;

public class PlayerRunnerBuffHandler
{
    private const float AmplificationSpeedBuffRemainDuration = 90f;
    private const float PercentScale = 0.01f;

    private readonly HashSet<AmplificationTowerBuffParam> _activeAmplificationSources = new();
    private bool _isAmplificationSpeedBuffActive;
    private float _amplificationSpeedBuffRemainTime;
    private float _amplificationSpeedBonusRate;

    public void BuffEnter(PlayerRunner runner, IBuffParam buffParam)
    {
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                _activeAmplificationSources.Add(amplificationTowerBuffParam);
                _isAmplificationSpeedBuffActive = true;
                _amplificationSpeedBuffRemainTime = 0f;
                RecalculateAmplificationSpeedBonusRate();
                break;
            default:
                break;
        }
    }

    public void BuffStay(PlayerRunner runner, IBuffParam buffParam)
    {
        // 버프 지속 로직 구현
    }

    public void BuffExit(PlayerRunner runner, IBuffParam buffParam)
    {
        switch (buffParam)
        {
            case AmplificationTowerBuffParam amplificationTowerBuffParam:
                if (!_activeAmplificationSources.Remove(amplificationTowerBuffParam))
                {
                    break;
                }

                if (_activeAmplificationSources.Count > 0)
                {
                    RecalculateAmplificationSpeedBonusRate();
                }
                else
                {
                    _amplificationSpeedBuffRemainTime = AmplificationSpeedBuffRemainDuration;
                }

                break;
            default:
                break;
        }
    }

    public void Tick(float deltaTime)
    {
        if (!_isAmplificationSpeedBuffActive || _activeAmplificationSources.Count > 0)
        {
            return;
        }

        _amplificationSpeedBuffRemainTime -= deltaTime;
        if (_amplificationSpeedBuffRemainTime > 0f)
        {
            return;
        }

        _isAmplificationSpeedBuffActive = false;
        _amplificationSpeedBuffRemainTime = 0f;
        _amplificationSpeedBonusRate = 0f;
    }

    public float GetMovementSpeed(PlayerRunner runner)
    {
        if (!_isAmplificationSpeedBuffActive)
        {
            return runner.MovementSpeed;
        }

        return runner.MovementSpeed * (1f + _amplificationSpeedBonusRate);
    }

    private void RecalculateAmplificationSpeedBonusRate()
    {
        _amplificationSpeedBonusRate = 0f;
        foreach (var source in _activeAmplificationSources)
        {
            if (source == null)
            {
                continue;
            }

            _amplificationSpeedBonusRate = Mathf.Max(
                _amplificationSpeedBonusRate,
                source.SpeedBonus * PercentScale);
        }
    }
}
