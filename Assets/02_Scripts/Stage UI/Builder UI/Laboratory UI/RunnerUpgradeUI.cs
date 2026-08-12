using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class RunnerUpgradeUI : MonoBehaviour
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

        [Header("업그레이드 적용값")]
        [SerializeField] private float _hpUpgradeAmount = 10f;
        [SerializeField] private float _speedUpgradeAmount = 1f;
        [SerializeField] private float _staminaUpgradeAmount = 10f;
        [SerializeField] private float _staminaRecoveryUpgradeAmount = 1f;
        [SerializeField] private float _weaponUpgradeAmount = 0.1f;

        [Header("레벨 텍스트")]
        [SerializeField] private TextMeshProUGUI _hpLevelText;
        [SerializeField] private TextMeshProUGUI _speedLevelText;
        [SerializeField] private TextMeshProUGUI _staminaLevelText;
        [SerializeField] private TextMeshProUGUI _staminaRecoveryLevelText;
        [SerializeField] private TextMeshProUGUI _weaponLevelText;

        [Header("비용 텍스트")]
        [SerializeField] private TextMeshProUGUI _hpMineralCostText;
        [SerializeField] private TextMeshProUGUI _hpGasCostText;
        [SerializeField] private TextMeshProUGUI _speedMineralCostText;
        [SerializeField] private TextMeshProUGUI _speedGasCostText;
        [SerializeField] private TextMeshProUGUI _staminaMineralCostText;
        [SerializeField] private TextMeshProUGUI _staminaGasCostText;
        [SerializeField] private TextMeshProUGUI _staminaRecoveryMineralCostText;
        [SerializeField] private TextMeshProUGUI _staminaRecoveryGasCostText;
        [SerializeField] private TextMeshProUGUI _weaponMineralCostText;
        [SerializeField] private TextMeshProUGUI _weaponGasCostText;

        private PlayerRunner _playerRunner;
        private int _currentHpUpgradeCount;
        private int _currentSpeedUpgradeCount;
        private int _currentStaminaUpgradeCount;
        private int _currentStaminaRecoveryUpgradeCount;
        private int _currentWeaponUpgradeCount;

        public void InitializeRunnerUpgradeUI()
        {
            RefreshAllUpgradeDisplays();
        }

        public void InjectionRunnerReference(PlayerRunner runner)
        {
            _playerRunner = runner;
        }

        public void OnClickRunnerHPUpButton()
        {
            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Health,
                ref _currentHpUpgradeCount,
                _hpUpgradeMaxCount,
                _hpUpgradeCost,
                _hpUpgradeAmount,
                _hpLevelText,
                _hpMineralCostText,
                _hpGasCostText);
        }

        public void OnClickRunnerSpeedUpButton()
        {
            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.MoveSpeed,
                ref _currentSpeedUpgradeCount,
                _speedUpgradeMaxCount,
                _speedUpgradeCost,
                _speedUpgradeAmount,
                _speedLevelText,
                _speedMineralCostText,
                _speedGasCostText);
        }

        public void OnClickRunnerStaminaUpButton()
        {
            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Stamina,
                ref _currentStaminaUpgradeCount,
                _staminaUpgradeMaxCount,
                _staminaUpgradeCost,
                _staminaUpgradeAmount,
                _staminaLevelText,
                _staminaMineralCostText,
                _staminaGasCostText);
        }

        public void OnClickRunnerStaminaRecoveryUpButton()
        {
            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.StaminaRecovery,
                ref _currentStaminaRecoveryUpgradeCount,
                _staminaRecoveryUpgradeMaxCount,
                _staminaRecoveryUpgradeCost,
                _staminaRecoveryUpgradeAmount,
                _staminaRecoveryLevelText,
                _staminaRecoveryMineralCostText,
                _staminaRecoveryGasCostText);
        }

        public void OnClickRunnerWeaponUpButton()
        {
            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Weapon,
                ref _currentWeaponUpgradeCount,
                _weaponUpgradeMaxCount,
                _weaponUpgradeCost,
                _weaponUpgradeAmount,
                _weaponLevelText,
                _weaponMineralCostText,
                _weaponGasCostText);
        }

        private void RefreshAllUpgradeDisplays()
        {
            RefreshUpgradeDisplay(_currentHpUpgradeCount, _hpUpgradeMaxCount, _hpUpgradeCost, _hpLevelText, _hpMineralCostText, _hpGasCostText);
            RefreshUpgradeDisplay(_currentSpeedUpgradeCount, _speedUpgradeMaxCount, _speedUpgradeCost, _speedLevelText, _speedMineralCostText, _speedGasCostText);
            RefreshUpgradeDisplay(_currentStaminaUpgradeCount, _staminaUpgradeMaxCount, _staminaUpgradeCost, _staminaLevelText, _staminaMineralCostText, _staminaGasCostText);
            RefreshUpgradeDisplay(_currentStaminaRecoveryUpgradeCount, _staminaRecoveryUpgradeMaxCount, _staminaRecoveryUpgradeCost, _staminaRecoveryLevelText, _staminaRecoveryMineralCostText, _staminaRecoveryGasCostText);
            RefreshUpgradeDisplay(_currentWeaponUpgradeCount, _weaponUpgradeMaxCount, _weaponUpgradeCost, _weaponLevelText, _weaponMineralCostText, _weaponGasCostText);
        }

        private void TryRequestRunnerUpgrade(
            RunnerLaboratoryUpgradeType type,
            ref int currentUpgradeCount,
            int maxUpgradeCount,
            Cost baseCost,
            float amount,
            TextMeshProUGUI levelText,
            TextMeshProUGUI mineralCostText,
            TextMeshProUGUI gasCostText)
        {
            if (currentUpgradeCount >= maxUpgradeCount)
                return;

            int nextLevel = currentUpgradeCount + 1;
            Cost cost = GetMultiplyCost(nextLevel, baseCost);
            if (!CanAfford(cost))
                return;

            var request = new RunnerLaboratoryUpgradeRequest(type, nextLevel, amount, cost);
            if (!TrySendRunnerUpgradeRequest(request))
                return;

            currentUpgradeCount = nextLevel;
            RefreshUpgradeDisplay(currentUpgradeCount, maxUpgradeCount, baseCost, levelText, mineralCostText, gasCostText);
        }

        private static Cost GetMultiplyCost(int count, Cost cost)
        {
            return new Cost(cost.Mineral * count, cost.Gas * count);
        }

        private static void RefreshUpgradeDisplay(
            int currentUpgradeCount,
            int maxUpgradeCount,
            Cost baseCost,
            TextMeshProUGUI levelText,
            TextMeshProUGUI mineralCostText,
            TextMeshProUGUI gasCostText)
        {
            if (levelText != null)
                levelText.text = "Lv. " + currentUpgradeCount;

            if (currentUpgradeCount >= maxUpgradeCount)
            {
                SetCostText(mineralCostText, "MAX");
                SetCostText(gasCostText, "MAX");
                return;
            }

            Cost nextCost = GetMultiplyCost(currentUpgradeCount + 1, baseCost);
            SetCostText(mineralCostText, nextCost.Mineral.ToString());
            SetCostText(gasCostText, nextCost.Gas.ToString());
        }

        private static void SetCostText(TextMeshProUGUI text, string value)
        {
            if (text != null)
                text.text = value;
        }

        private static bool CanAfford(Cost cost)
        {
            return ResourceSystem.Instance != null && ResourceSystem.Instance.IsResourceSufficient(cost);
        }

        private bool TrySendRunnerUpgradeRequest(RunnerLaboratoryUpgradeRequest request)
        {
            if (_playerRunner == null)
            {
                Debug.LogWarning("연구소 러너 업그레이드 요청 실패: PlayerRunner 참조가 없습니다.");
                return false;
            }

            if (!_playerRunner.TryGetComponent<IRunnerLaboratoryUpgradeReceiver>(out var receiver))
            {
                Debug.LogWarning($"연구소 러너 업그레이드 요청 실패: Runner가 {nameof(IRunnerLaboratoryUpgradeReceiver)}를 구현하지 않았습니다. Type: {request.Type}, Level: {request.NextLevel}");
                return false;
            }

            return receiver.TryRequestLaboratoryUpgrade(request);
        }
    }
}
