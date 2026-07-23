using Dev.Network;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class PlayerBuilderUI : MonoBehaviour
    {
        [SerializeField] private BuilderMainUI _builderMainUI;
        [SerializeField] private TimerUI _timerUI;
        [SerializeField] private GameObject _towerBuildUI;
        [SerializeField] private LaboratoryUI _laboratoryUI;
        [SerializeField] private RadioSignalUI _radioSignalUI;
        [SerializeField] private GameObject _towerSelectUI;
        [SerializeField] private DragSystem _dragSystem;
        [SerializeField] private Slider _runnerHPSlider;
        [SerializeField] private ResourceInfoUI _resourceInfoUI;

        [SerializeField] private TextMeshProUGUI _buildUIText;

        [Header("Properties UI")]
        [SerializeField] private GameObject _randomPropertiesUI;
        [SerializeField] private GameObject[] _selectPropertiesUI;

        [Header("Tower Actions")]
        [SerializeField] private GameObject _towerSellButton;
        [SerializeField] private GameObject _towerMoveButton;

        [Header("Cost Display")]
        [SerializeField] private TextMeshProUGUI[] _towerBuildCostLabels;
        [SerializeField] private TowerData[] _towerBuildCostData;
        [SerializeField] private TextMeshProUGUI _towerMoveCostLabel;
        [SerializeField] private TextMeshProUGUI[] _centerPropertyCostLabels;

        public bool IsLaboratoryUIActive => _laboratoryUI.gameObject.activeSelf;

        private PlayerRunner _playerRunner;
        private string _towerMoveLabel;
        private Cost _displayedMoveCost;
        private bool _hasDisplayedMoveCost;

        private static readonly TowerPropertiesType[] CenterPropertyTypes =
        {
            TowerPropertiesType.Flame,
            TowerPropertiesType.Blitz,
            TowerPropertiesType.Biochemical
        };

        #region Action

        public event Action<TowerData> OnClickTowerBuildButtonAction; // 타워 건설 버튼을 눌렀을 때의 액션
        public event Action<bool> OnClickLaboratoryButtonAction; // 실험실 버튼을 눌렀을 떄의 액션
        public event Action OnClickSellTowerButtonAction; // 타워 판매 버튼을 눌렀을 때의 액션
        public event Action OnClickMoveTowerButtonAction; // 타워 움직임 버튼을 눌렀을 때의 액션
        public event Action<int> OnClickUpgradeTowerButtonAction; // 타워 속성 부여 버튼을 눌렀을 때의 액션

        #endregion

        #region 프로퍼티

        public DragSystem DragSystem => _dragSystem;

        #endregion

        private void Awake()
        {
            DisableAll();
            _runnerHPSlider.value = 1f;
            InitializeCostDisplays();
        }

        private void OnEnable()
        {
            if(_playerRunner != null)
            {
                _playerRunner.OnHPValueChanged -= ChangeHPSlider;
                _playerRunner.OnHPValueChanged += ChangeHPSlider;
            }
        }

        private void OnDisable()
        {
            if (_playerRunner != null)
            {
                _playerRunner.OnHPValueChanged -= ChangeHPSlider;
            }
        }

        private void Start()
        {
            InitializeMainUI();
        }

        public void InitializePlayerBuilderUI(TowerUpgradeManager towerUpgradeManager, ResourceSystem resourceSystem, TimeSystem timeSystem)
        {
            // 타이머 UI 초기화
            _timerUI.InitializeTimerUI(timeSystem);

            // 무전 신호 UI 초기화
            _radioSignalUI.InitializeRadioSignalUI();

            // 연구소 UI 초기화
            _laboratoryUI.InitializeLaboratoryUI(towerUpgradeManager);
            _laboratoryUI.gameObject.SetActive(false);

            // 자원 UI 초기화
            _resourceInfoUI.InitializeResourceInfoUI(resourceSystem);
        }

        #region 클릭 이벤트 메서드

        // 실험실 버튼 클릭 이벤트
        public void OnClickLaboratoryButton(bool isActive)
        {
            OnClickLaboratoryButtonAction?.Invoke(isActive);
        }

        // 타워 선택 버튼 클릭 이벤트
        public void OnClickTowerButton(TowerData data)
        {
            OnClickTowerBuildButtonAction?.Invoke(data);
        }

        // 타워 판매 버튼 클릭 이벤트
        public void OnClickTowerSellButton()
        {
            OnClickSellTowerButtonAction?.Invoke();
        }

        // 타워 이동 버튼 클릭 이벤트
        public void OnClickTowerMoveButton()
        {
            OnClickMoveTowerButtonAction?.Invoke();
        }

        // 타워 속성 부여 버튼 클릭 이벤트
        public void OnClickTowerUpgradeButton(int typeIndex)
        {
            OnClickUpgradeTowerButtonAction?.Invoke(typeIndex);
        }

        #endregion

        #region UI 활성화/비활성화 메서드

        // 실험실 UI 활성화/비활성화
        public void ActivationLaboratoryUI(bool isActive)
        {
            _builderMainUI.gameObject.SetActive(!isActive);
            _laboratoryUI.gameObject.SetActive(isActive);
        }

        // 타워 건설 UI 활성화/비활성화
        public void ActivationTowerBuildUI(bool isActive, string towerBuildInfo = "")
        {
            if (_buildUIText != null)
            {
                _buildUIText.text = towerBuildInfo;
            }

            _builderMainUI.gameObject.SetActive(!isActive);
            _towerBuildUI.SetActive(isActive);
        }

        // 타워 선택 UI 활성화/비활성화
        public void ActivationTowerSelectUI(
            bool isActive,
            TowerType type = TowerType.Attack,
            TowerCapability capabilities = TowerCapability.None,
            Cost moveCost = default)
        {
            _builderMainUI.gameObject.SetActive(!isActive);
            _towerSelectUI.SetActive(isActive);

            RefreshTowerSelectActions(type, capabilities, moveCost);
        }

        public void RefreshTowerSelectActions(
            TowerType type,
            TowerCapability capabilities,
            Cost moveCost = default)
        {
            bool canSell = (capabilities & TowerCapability.Sell) != 0;
            bool canMove = (capabilities & TowerCapability.Move) != 0;
            bool canAssignProperty = (capabilities & TowerCapability.AssignProperty) != 0;

            _towerSellButton?.SetActive(canSell);
            _towerMoveButton?.SetActive(canMove);
            RefreshMoveCost(moveCost);

            switch (type)
            {
                case TowerType.Attack:
                    _randomPropertiesUI?.SetActive(canAssignProperty);
                    SelectPropertiesButtonsActive(false);
                    break;
                case TowerType.Center:
                    _randomPropertiesUI?.SetActive(false);
                    SelectPropertiesButtonsActive(canAssignProperty);
                    break;
                case TowerType.Support:
                    _randomPropertiesUI?.SetActive(false);
                    SelectPropertiesButtonsActive(false);
                    break;
            }
        }

        #endregion

        private void SelectPropertiesButtonsActive(bool isActive)
        {
            foreach (var ui in _selectPropertiesUI)
                ui?.SetActive(isActive);
        }

        private void InitializeCostDisplays()
        {
            int towerCostCount = Mathf.Min(
                _towerBuildCostLabels?.Length ?? 0,
                _towerBuildCostData?.Length ?? 0);

            for (int i = 0; i < towerCostCount; i++)
            {
                TextMeshProUGUI label = _towerBuildCostLabels[i];
                Tower tower = _towerBuildCostData[i]?.Tower;
                if (label == null || tower == null)
                    continue;

                label.text = AppendCost(label.text, tower.Cost);
            }

            int propertyCostCount = Mathf.Min(
                _centerPropertyCostLabels?.Length ?? 0,
                CenterPropertyTypes.Length);

            for (int i = 0; i < propertyCostCount; i++)
            {
                TextMeshProUGUI label = _centerPropertyCostLabels[i];
                if (label == null)
                    continue;

                label.text = AppendCost(
                    label.text,
                    CenterTower.GetCenterPropertyCost(CenterPropertyTypes[i]));
            }

            if (_towerMoveCostLabel != null)
            {
                _towerMoveLabel = _towerMoveCostLabel.text;
            }
        }

        private void RefreshMoveCost(Cost moveCost)
        {
            if (_towerMoveCostLabel == null ||
                (_hasDisplayedMoveCost &&
                 _displayedMoveCost.Mineral == moveCost.Mineral &&
                 _displayedMoveCost.Gas == moveCost.Gas))
            {
                return;
            }

            _displayedMoveCost = moveCost;
            _hasDisplayedMoveCost = true;
            _towerMoveCostLabel.text = AppendCost(_towerMoveLabel, moveCost, "FREE MOVE");
        }

        private static string AppendCost(string label, Cost cost, string freeText = "FREE")
        {
            const string chipStart = "<size=14><mark=#1D222BCC>";
            const string chipEnd = "</mark></size>";

            if (cost.Mineral <= 0 && cost.Gas <= 0)
                return $"{label}\n{chipStart}<color=#6FCF97>{freeText}</color>{chipEnd}";

            string mineral = cost.Mineral > 0
                ? $"<color=#F2C94C>M {cost.Mineral}</color>"
                : string.Empty;
            string gas = cost.Gas > 0
                ? $"<color=#56CCF2>G {cost.Gas}</color>"
                : string.Empty;
            string separator = cost.Mineral > 0 && cost.Gas > 0 ? "  " : string.Empty;

            return $"{label}\n{chipStart}{mineral}{separator}{gas}{chipEnd}";
        }

        /// <summary>
        /// 연구소 UI에 PlayerRunner 참조 주입
        /// </summary>
        /// <param name="runner">PlayerRunner 참조</param>
        public void LaboratoryUIInjectionRunner(PlayerRunner runner)
        {
            if (_laboratoryUI != null)
                _laboratoryUI.InjectionRunnerReference(runner);
        }

        private void DisableAll()
        {
            _builderMainUI.gameObject.SetActive(true);
            _towerBuildUI.gameObject.SetActive(false);
            _laboratoryUI.gameObject.SetActive(false);
            _towerSelectUI.gameObject.SetActive(false);
        }

        private void InitializeMainUI()
        {
            _builderMainUI.gameObject.SetActive(true);
        }

        /// <summary>
        /// 플레이어 러너 참조 받기
        /// </summary>
        /// <param name="playerRunner">플레이어 러너 참조</param>
        public void GetPlayerRunnerReference(PlayerRunner playerRunner)
        {
            _playerRunner = playerRunner;
            _playerRunner.OnHPValueChanged += ChangeHPSlider;
        }

        // 체력 슬라이더 업데이트
        private void ChangeHPSlider(float maxHP, float currentHP)
        {
            _runnerHPSlider.value = currentHP / maxHP;
        }
    }
}
