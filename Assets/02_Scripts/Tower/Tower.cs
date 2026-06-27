using Fusion;
using Dev.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class Tower : GridPlaceable, ICanClickObject
    {
        [Header("Capabilities")]
        [SerializeField] private TowerCapability _capabilities = TowerCapability.Sell;
        [SerializeField] private bool _includeLegacyCapabilities = true;

        [Header("타워")]
        [SerializeField] private Cost _cost;
        [SerializeField] private TowerGhost _ghost;
        [SerializeField] private TowerType _type;
        [SerializeField] private string _towerId;

        [Header("선택 시 표시")]
        [SerializeField] protected GameObject _selectedChecker; // 타워 선택 시 표시 오브젝트
        [SerializeField] protected Image _selectedImage; // 타워 선택 시 UI에 표시될 이미지

        public Cost Cost => _cost;
        public TowerGhost Ghost => _ghost;
        public bool IsCenter => (_type == TowerType.Center);
        public TowerType Type => _type;
        public string TowerID => _towerId;
        public TowerUpgradeManager TowerUpgradeManager => _towerUpgradeManager;
        public bool HasProperty => PropertyType != TowerPropertiesType.None;
        public bool IsPropertyRequestPending => _isPropertyRequestPending;
        public TowerCapability AvailableCapabilities => GetAvailableCapabilities();

        [Networked, OnChangedRender(nameof(HandlePropertyChanged))]
        public TowerPropertiesType PropertyType { get; private set; }

        private TowerUpgradeManager _towerUpgradeManager;
        private bool _isPropertyRequestPending;

        private void Awake()
        {
            OnCancelClickThisObject();
            TowerAwake();
        }

        public override void Spawned()
        {
            base.Spawned();

            TowerBuildManager.Instance?.InjectTowerDependencies(this);

            if (PropertyType != TowerPropertiesType.None)
                HandlePropertyChanged();

            if (HasStateAuthority)
            {
                InfiniteGrid.Instance?.HostOnlyReadTowers.Add(this);
            }
        }

        public override void FixedUpdateNetwork()
        {
            TowerFixedUpdateNetwork();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (HasStateAuthority)
                InfiniteGrid.Instance?.HostOnlyReadTowers.Remove(this);

            NotifyBuilderTowerDespawned();
            base.Despawned(runner, hasState);
            TowerDespawned();
        }

        /// <summary>
        /// Tower의 Awake 내부 동작 함수
        /// </summary>
        protected virtual void TowerAwake()
        {
            Debug.Log("Tower Awake");
        }

        protected virtual void TowerFixedUpdateNetwork()
        {
        }

        protected virtual void TowerDespawned()
        {
        }

        protected void SetId(string id)
        {
            _towerId = id;
        }

        public void InitializeTowerUpgradeManager(TowerUpgradeManager towerUpgradeManager)
        {
            _towerUpgradeManager = towerUpgradeManager;
        }

        public bool HasCapability(TowerCapability capability)
        {
            return (AvailableCapabilities & capability) == capability;
        }

        protected bool TryAssignProperty(TowerPropertiesType propertyType)
        {
            if (propertyType == TowerPropertiesType.None ||
                (GetConfiguredCapabilities() & TowerCapability.AssignProperty) == 0 ||
                HasProperty ||
                _isPropertyRequestPending)
            {
                return false;
            }

            _isPropertyRequestPending = true;
            RPC_RequestAssignProperty(propertyType);
            return true;
        }

        protected virtual void OnTowerPropertyChanged(TowerPropertiesType propertyType)
        {
        }

        public void OnLeftMouseDownThisObject()
        {
            _selectedChecker.SetActive(true);
        }

        public void OnLeftMouseUpThisObject()
        {
            var builder = StageBootstrapper.Instance.PlayerBuilder;
            if (builder != null)
            {
                builder.TowerSelected(this);
            }
        }

        public void OnCancelClickThisObject()
        {
            _selectedChecker.SetActive(false);
        }

        private void NotifyBuilderTowerDespawned()
        {
            if (StageBootstrapper.Instance == null)
                return;

            var builder = StageBootstrapper.Instance.PlayerBuilder;
            if (builder == null)
                return;

            builder.OnTowerDespawned(this);
        }

        private TowerCapability GetAvailableCapabilities()
        {
            TowerCapability capabilities = GetConfiguredCapabilities();
            if (HasProperty || _isPropertyRequestPending)
                capabilities &= ~TowerCapability.AssignProperty;

            return capabilities;
        }

        private TowerCapability GetConfiguredCapabilities()
        {
            TowerCapability capabilities = _capabilities;
            if (!_includeLegacyCapabilities)
                return capabilities;

            if (this is ICanDragObject)
                capabilities |= TowerCapability.Move;

            if (this is AttackTower || this is CenterTower)
                capabilities |= TowerCapability.AssignProperty;

            return capabilities;
        }

        private void HandlePropertyChanged()
        {
            _isPropertyRequestPending = false;
            OnTowerPropertyChanged(PropertyType);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestAssignProperty(TowerPropertiesType propertyType)
        {
            if (PropertyType != TowerPropertiesType.None || propertyType == TowerPropertiesType.None)
                return;

            PropertyType = propertyType;
        }
    }
}
