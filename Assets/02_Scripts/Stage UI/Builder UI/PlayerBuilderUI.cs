using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class PlayerBuilderUI : MonoBehaviour
    {
        [SerializeField] private BuilderMainUI _builderMainUI;
        [SerializeField] private GameObject _towerBuildUI;
        [SerializeField] private LaboratoryUI _laboratoryUI;
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

        public bool IsLaboratoryUIActive => _laboratoryUI.gameObject.activeSelf;

        private PlayerRunner _playerRunner;

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

        public void InitializePlayerBuilderUI(TowerUpgradeManager towerUpgradeManager, ResourceSystem resourceSystem)
        {
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
            TowerCapability capabilities = TowerCapability.None)
        {
            _builderMainUI.gameObject.SetActive(!isActive);
            _towerSelectUI.SetActive(isActive);

            RefreshTowerSelectActions(type, capabilities);
        }

        public void RefreshTowerSelectActions(TowerType type, TowerCapability capabilities)
        {
            bool canSell = (capabilities & TowerCapability.Sell) != 0;
            bool canMove = (capabilities & TowerCapability.Move) != 0;
            bool canAssignProperty = (capabilities & TowerCapability.AssignProperty) != 0;

            _towerSellButton?.SetActive(canSell);
            _towerMoveButton?.SetActive(canMove);

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
