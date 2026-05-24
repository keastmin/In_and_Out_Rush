using Fusion;
using Dev.Network;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class Tower : GridPlaceable, ICanClickObject
    {
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

        private void Awake()
        {
            OnCancelClickThisObject();
            TowerAwake();
        }

        public override void Spawned()
        {
            base.Spawned();

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
    }
}