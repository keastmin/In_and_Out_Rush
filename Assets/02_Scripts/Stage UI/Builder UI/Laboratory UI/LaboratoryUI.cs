using System;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public enum RunnerLaboratoryUpgradeType
    {
        Health,
        MoveSpeed,
        Stamina,
        StaminaRecovery,
        Weapon
    }

    public readonly struct RunnerLaboratoryUpgradeRequest
    {
        public RunnerLaboratoryUpgradeRequest(
            RunnerLaboratoryUpgradeType type,
            int nextLevel,
            float amount,
            Cost cost)
        {
            Type = type;
            NextLevel = nextLevel;
            Amount = amount;
            Cost = cost;
        }

        public RunnerLaboratoryUpgradeType Type { get; }
        public int NextLevel { get; }
        public float Amount { get; }
        public Cost Cost { get; }
    }

    public interface IRunnerLaboratoryUpgradeReceiver
    {
        bool TryRequestLaboratoryUpgrade(RunnerLaboratoryUpgradeRequest request);
    }

    public class LaboratoryUI : MonoBehaviour
    {
        [SerializeField] private TowerUpgradeUI _towerUpgradeUI;
        [SerializeField] private RunnerUpgradeUI _runnerUpgradeUI;
        [SerializeField] private RunnerSupplyUI _runnerSupplyUI;

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

        [Header("표시용 텍스트")]
        [SerializeField] private TextMeshProUGUI _hpUpText;
        [SerializeField] private TextMeshProUGUI _speedUpText;
        [SerializeField] private TextMeshProUGUI _staminaUpText;
        [SerializeField] private TextMeshProUGUI _staminaRecoveryUpText;
        [SerializeField] private TextMeshProUGUI _weaponUpText;

        [Header("보급")]
        [SerializeField] private GameObject[] _supplyQueueSlot;

        private PlayerRunner _playerRunner; // 플레이어 러너 참조

        // 현재 업그레이드 횟수
        private int _currentHpUpgradeCount = 0;
        private int _currentSpeedUpgradeCount = 0;
        private int _currentStaminaUpgradeCount = 0;
        private int _currentStaminaRecoveryUpgradeCount = 0;
        private int _currentWeaponUpgradeCount = 0;

        // 보급 슬롯
        private int _maxSlotCount = 0; // 최대 슬롯 수
        private int _currSlotFillCount = 0; // 현재 슬롯이 차있는 수
        private TextMeshProUGUI[] _supplyQueueTexts; // 보급 슬롯의 텍스트들 저장
        private const string _noneText = "none"; // 슬롯이 비었을 때의 텍스트
        private const string _weaponText = "weapon"; // 무기 텍스트
        private const string _skillText = "skill"; // 스킬 텍스트
        private const string _itemText = "item"; // 아이템 텍스트

        private void Awake()
        {
            // 보급 슬롯 텍스트 초기화
            SupplyQueueTextInit();
        }

        private void Start()
        {
            // 슬롯의 텍스트를 초기화하는 이벤트 연결
            SupplyTowerManager.Instance.OnUIRevertAction += RevertSupplySlotText;
        }

        public void InitializeLaboratoryUI(TowerUpgradeManager towerUpgradeManager)
        {
            _towerUpgradeUI?.InitializeTowerUpgradeUI(towerUpgradeManager);
            _runnerUpgradeUI?.InitializeRunnerUpgradeUI();
            _runnerSupplyUI?.InitializeRunnerSupplyUI();
        }

        /// <summary>
        /// 플레이어 러너 변수에 참조를 주입하는 함수
        /// </summary>
        /// <param name="runner">러너 참조</param>
        public void InjectionRunnerReference(PlayerRunner runner)
        {
            _playerRunner = runner;
            _runnerUpgradeUI?.InjectionRunnerReference(runner);
        }

        #region 버튼 이벤트 함수

        // 러너의 체력 업그레이드
        public void OnClickRunnerHPUpButton()
        {
            if (_runnerUpgradeUI != null)
            {
                _runnerUpgradeUI.OnClickRunnerHPUpButton();
                return;
            }

            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Health,
                ref _currentHpUpgradeCount,
                _hpUpgradeMaxCount,
                _hpUpgradeCost,
                _hpUpgradeAmount,
                _hpUpText,
                "HP Up");
        }

        // 러너의 속도 업그레이드
        public void OnClickRunnerSpeedUpButton()
        {
            if (_runnerUpgradeUI != null)
            {
                _runnerUpgradeUI.OnClickRunnerSpeedUpButton();
                return;
            }

            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.MoveSpeed,
                ref _currentSpeedUpgradeCount,
                _speedUpgradeMaxCount,
                _speedUpgradeCost,
                _speedUpgradeAmount,
                _speedUpText,
                "Speed Up");
        }

        // 러너의 기력량 업그레이드
        public void OnClickRunnerStaminaUpButton()
        {
            if (_runnerUpgradeUI != null)
            {
                _runnerUpgradeUI.OnClickRunnerStaminaUpButton();
                return;
            }

            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Stamina,
                ref _currentStaminaUpgradeCount,
                _staminaUpgradeMaxCount,
                _staminaUpgradeCost,
                _staminaUpgradeAmount,
                _staminaUpText,
                "Stamina Up");
        }

        // 러너의 기력 회복속도 업그레이드
        public void OnClickRunnerStaminaRecoveryUpButton()
        {
            if (_runnerUpgradeUI != null)
            {
                _runnerUpgradeUI.OnClickRunnerStaminaRecoveryUpButton();
                return;
            }

            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.StaminaRecovery,
                ref _currentStaminaRecoveryUpgradeCount,
                _staminaRecoveryUpgradeMaxCount,
                _staminaRecoveryUpgradeCost,
                _staminaRecoveryUpgradeAmount,
                _staminaRecoveryUpText,
                "Stamina Recovery Up");
        }

        // 러너의 무기 업그레이드
        public void OnClickRunnerWeaponUpButton()
        {
            if (_runnerUpgradeUI != null)
            {
                _runnerUpgradeUI.OnClickRunnerWeaponUpButton();
                return;
            }

            TryRequestRunnerUpgrade(
                RunnerLaboratoryUpgradeType.Weapon,
                ref _currentWeaponUpgradeCount,
                _weaponUpgradeMaxCount,
                _weaponUpgradeCost,
                _weaponUpgradeAmount,
                _weaponUpText,
                "Weapon Up");
        }

        // 스킬 보급품 구매
        public void OnClickSkillSupplyButton()
        {
            if (_runnerSupplyUI != null)
            {
                _runnerSupplyUI.OnClickSkillSupplyButton();
                return;
            }

            if (_currSlotFillCount >= _maxSlotCount) return;

            SupplyTowerManager.Instance.FillSupplyList(SupplyTowerManager.SKILL_SUPPLY_NUM);
            ChangeSupplyText(_currSlotFillCount, _skillText);
            _currSlotFillCount++;
        }

        // 무기 보급품 구매
        public void OnClickWeaponSupplyButton()
        {
            if (_runnerSupplyUI != null)
            {
                _runnerSupplyUI.OnClickWeaponSupplyButton();
                return;
            }

            if (_currSlotFillCount >= _maxSlotCount) return;

            SupplyTowerManager.Instance.FillSupplyList(SupplyTowerManager.WEAPON_SUPPLY_NUM);
            ChangeSupplyText(_currSlotFillCount, _weaponText);
            _currSlotFillCount++;
        }

        // 아이템 보급품 구매
        public void OnClickItemSupplyButton()
        {
            if (_runnerSupplyUI != null)
            {
                _runnerSupplyUI.OnClickItemSupplyButton();
                return;
            }

            if (_currSlotFillCount >= _maxSlotCount) return;

            SupplyTowerManager.Instance.FillSupplyList(SupplyTowerManager.ITEM_SUPPLY_NUM);
            ChangeSupplyText(_currSlotFillCount, _itemText);
            _currSlotFillCount++;
        }

        #endregion

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
            if (ResourceSystem.Instance != null)
            {
                canAfford = ResourceSystem.Instance.IsResourceSufficient(cost);
            }
            return canAfford;
        }

        private void TryRequestRunnerUpgrade(
            RunnerLaboratoryUpgradeType type,
            ref int currentUpgradeCount,
            int maxUpgradeCount,
            Cost baseCost,
            float amount,
            TextMeshProUGUI textUGUI,
            string text)
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
            UpgradeTextChange(textUGUI, text, currentUpgradeCount);
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

        /// <summary>
        /// 보급 큐 슬롯들의 텍스트 초기화
        /// </summary>
        private void SupplyQueueTextInit()
        {
            _maxSlotCount = _supplyQueueSlot.Length;
            _supplyQueueTexts = new TextMeshProUGUI[_maxSlotCount];
            for (int i = 0; i < _maxSlotCount; i++)
            {
                _supplyQueueTexts[i] = _supplyQueueSlot[i].GetComponentInChildren<TextMeshProUGUI>();

                if (_supplyQueueTexts[i] != null)
                    _supplyQueueTexts[i].text = _noneText;
            }
        }

        /// <summary>
        /// 슬롯의 텍스트를 바꾸는 함수
        /// </summary>
        /// <param name="index">텍스트를 바꿔야 하는 인덱스</param>
        /// <param name="text">바꿔야 하는 텍스트</param>
        private void ChangeSupplyText(int index, string text)
        {
            _supplyQueueTexts[index].text = text;
        }

        /// <summary>
        /// 모든 슬롯의 텍스트를 none으로 초기화 하고 슬롯이 채워진 수를 초기화하는 함수
        /// </summary>
        private void RevertSupplySlotText()
        {
            for (int i = 0; i < _currSlotFillCount; i++)
            {
                ChangeSupplyText(i, _noneText);
            }
            _currSlotFillCount = 0;
        }
    }
}
