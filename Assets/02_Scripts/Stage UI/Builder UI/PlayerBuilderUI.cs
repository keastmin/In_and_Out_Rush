using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderUI : MonoBehaviour
    {
        [SerializeField] private BuilderMainUI _builderMainUI;
        [SerializeField] private GameObject _towerBuildUI;
        [SerializeField] private LaboratoryUI _laboratoryUI;
        [SerializeField] private GameObject _towerSelectUI;
        [SerializeField] private DragSystem _dragSystem;

        [SerializeField] private TextMeshProUGUI _buildUIText;

        [Header("Properties UI")]
        [SerializeField] private GameObject _randomPropertiesUI;
        [SerializeField] private GameObject[] _selectPropertiesUI;

        public bool IsLaboratoryUIActive => _laboratoryUI.gameObject.activeSelf;

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
        }

        private void Start()
        {
            InitializeMainUI();
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
        public void ActivationTowerSelectUI(bool isActive, TowerType type = TowerType.Attack)
        {
            _builderMainUI.gameObject.SetActive(!isActive);
            _towerSelectUI.SetActive(isActive);

            switch (type)
            {
                case TowerType.Attack:
                    _randomPropertiesUI?.SetActive(true);
                    SelectPropertiesButtonsActive(false);
                    break;
                case TowerType.Center:
                    _randomPropertiesUI?.SetActive(false);
                    SelectPropertiesButtonsActive(true);
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
    }
}