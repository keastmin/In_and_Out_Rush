using UnityEngine;
using UnityEngine.UI;

public class RunnerPlayerUI : MonoBehaviour
{
    [SerializeField] private Image _healthBar;
    [SerializeField] private Image _staminaBar;
    [SerializeField] private Image _skillIcon;

    public void SetHealthBarRatio(float ratio)
        => _healthBar.fillAmount = Mathf.Clamp01(ratio);

    public void SetStaminaBarRatio(float ratio)
        => _staminaBar.fillAmount = Mathf.Clamp01(ratio);

    public void SetSkillIcon(Sprite icon)
        => _skillIcon.sprite = icon;
}