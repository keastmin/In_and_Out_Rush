using TMPro;
using UnityEngine;

public class LaboratoryUI : MonoBehaviour
{
    [Header("업그레이드 최대치")]
    [SerializeField] private int _hpUpgradeMaxCount = 4;
    [SerializeField] private int _speedUpgradeMaxCount = 4;
    [SerializeField] private int _staminaUpgradeMaxCount = 4;
    [SerializeField] private int _staminaRecoveryUpgradeMaxCount = 4;
    [SerializeField] private int _weaponUpgradeMaxCount = 4;

    [Header("업그레이드 비용")]
    [SerializeField] private Cost _hpUpgradeCost = new Cost(0, 100);
    [SerializeField] private Cost _speedUpgradeCost = new Cost(0, 100);
    [SerializeField] private Cost _staminaUpgradeCost = new Cost(0, 100);
    [SerializeField] private Cost _staminaRecoveryUpgradeCost = new Cost(0, 50);
    [SerializeField] private Cost _weaponUpgradeCost = new Cost(0, 100);

    [Header("표시용 텍스트")]
    [SerializeField] private TextMeshProUGUI _hpUpText;
    [SerializeField] private TextMeshProUGUI _speedUpText;
    [SerializeField] private TextMeshProUGUI _staminaUpText;
    [SerializeField] private TextMeshProUGUI _staminaRecoveryUpText;
    [SerializeField] private TextMeshProUGUI _weaponUpText;

    [Header("보급")]

    private PlayerRunner _playerRunner; // 플레이어 러너 참조

    // 현재 업그레이드 횟수
    private int _currentHpUpgradeCount = 0;
    private int _currentSpeedUpgradeCount = 0;
    private int _currentStaminaUpgradeCount = 0;
    private int _currentStaminaRecoveryUpgradeCount = 0;
    private int _currentWeaponUpgradeCount = 0;

    // 그리드에 설치된 타워들 정보를 모으고 Grid 참조를 받아와 강화에 적용할 예정

    /// <summary>
    /// 플레이어 러너 변수에 참조를 주입하는 함수
    /// </summary>
    /// <param name="runner">러너 참조</param>
    public void InjectionRunnerReference(PlayerRunner runner)
    {
        _playerRunner = runner;
    }

    // 러너의 체력 업그레이드
    public void OnClickRunnerHPUpButton()
    {
        if(_currentHpUpgradeCount < _hpUpgradeMaxCount)
        {
            Cost cost = GetMultiplyCost(_currentHpUpgradeCount + 1, _hpUpgradeCost);
            if (CanAfford(cost))
            {
                ResourceSystem.Instance.DeductCost(cost);
                _currentHpUpgradeCount++;
                UpgradeTextChange(_hpUpText, "HP Up", _currentHpUpgradeCount);
            }
        }
    }

    // 러너의 속도 업그레이드
    public void OnClickRunnerSpeedUpButton()
    {
        if (_currentSpeedUpgradeCount < _speedUpgradeMaxCount)
        {
            Cost cost = GetMultiplyCost(_currentSpeedUpgradeCount + 1, _speedUpgradeCost);
            if (CanAfford(cost))
            {
                ResourceSystem.Instance.DeductCost(cost);
                _currentSpeedUpgradeCount++;
                UpgradeTextChange(_speedUpText, "Speed Up", _currentSpeedUpgradeCount);
            }
        }
    }

    // 러너의 기력량 업그레이드
    public void OnClickRunnerStaminaUpButton()
    {
        if (_currentStaminaUpgradeCount < _staminaUpgradeMaxCount)
        {
            Cost cost = GetMultiplyCost(_currentStaminaUpgradeCount + 1, _staminaUpgradeCost);
            if (CanAfford(cost))
            {
                ResourceSystem.Instance.DeductCost(cost);
                _currentStaminaUpgradeCount++;
                UpgradeTextChange(_staminaUpText, "Stamina Up", _currentStaminaUpgradeCount);
            }
        }
    }

    // 러너의 기력 회복속도 업그레이드
    public void OnClickRunnerStaminaRecoveryUpButton()
    {
        if (_currentStaminaRecoveryUpgradeCount < _staminaRecoveryUpgradeMaxCount)
        {
            Cost cost = GetMultiplyCost(_currentStaminaRecoveryUpgradeCount + 1, _staminaRecoveryUpgradeCost);
            if (CanAfford(cost))
            {
                ResourceSystem.Instance.DeductCost(cost);
                _currentStaminaRecoveryUpgradeCount++;
                UpgradeTextChange(_staminaRecoveryUpText, "Stamina Recovery Up", _currentStaminaRecoveryUpgradeCount);
            }
        }
    }

    // 러너의 무기 업그레이드
    public void OnClickRunnerWeaponUpButton()
    {
        if (_currentWeaponUpgradeCount < _weaponUpgradeMaxCount)
        {
            Cost cost = GetMultiplyCost(_currentWeaponUpgradeCount + 1, _weaponUpgradeCost);
            if (CanAfford(cost))
            {
                ResourceSystem.Instance.DeductCost(cost);
                _currentWeaponUpgradeCount++;
                UpgradeTextChange(_weaponUpText, "Weapon Up", _currentWeaponUpgradeCount);
            }
        }
    }

    /// <summary>
    /// 횟수를 곱한 비용을 반환하는 함수
    /// </summary>
    /// <param name="count">횟수</param>
    /// <param name="cost">기본 비용</param>
    /// <returns>곱한 비용</returns>
    private Cost GetMultiplyCost(int count, Cost cost)
    {
        return new Cost(cost.Mineral * count, cost.Gas * count);
    }

    /// <summary>
    /// 강화할 자원이 충분한지 확인하는 함수
    /// </summary>
    /// <param name="cost">지불할 비용</param>
    /// <returns>지불 가능 여부</returns>
    private bool CanAfford(Cost cost)
    {
        bool canAfford = false;
        if(ResourceSystem.Instance != null)
        {
            canAfford = ResourceSystem.Instance.IsResourceSufficient(cost);
        }
        return canAfford;
    }

    /// <summary>
    /// 업그레이드 카운트를 UI 텍스트에 표시
    /// </summary>
    /// <param name="textUGUI">변경할 텍스트 UI</param>
    /// <param name="text">변경할 텍스트</param>
    /// <param name="count">업그레이드 횟수</param>
    private void UpgradeTextChange(TextMeshProUGUI textUGUI, string text, int count)
    {
        if (textUGUI != null)
            textUGUI.text = text + "(" + count.ToString() + ")";
    }
}