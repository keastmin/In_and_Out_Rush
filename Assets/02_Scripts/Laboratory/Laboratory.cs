using Fusion;
using UnityEngine;
using System.Collections;
using Dev.Network;

namespace KIM.Dev
{
    public class Laboratory : GridPlaceable, ICanClickObject
    {
        [Header("Laboratory Grid")]
        [SerializeField][Min(0)] private int _defaultRange = 1;
        [SerializeField] private bool _ignoreTerritoryOnSpawn = true;

        protected override bool RequireTerritoryOnSpawn => !_ignoreTerritoryOnSpawn && _requireTerritory;

        private PlayerBuilderUI _builderUI;

        private void Awake()
        {
            if (_buildRange == 0)
            {
                _buildRange = _defaultRange;
            }
        }

        public override void Spawned()
        {
            base.Spawned();
            RegisterToStageBootstrapper();
        }

        private void RegisterToStageBootstrapper()
        {
            if (StageBootstrapper.Instance != null)
            {
                StageBootstrapper.Instance.OnSpawnedLaboratory(this);
                return;
            }

            StartCoroutine(RegisterToStageBootstrapperWhenReady());
        }

        private IEnumerator RegisterToStageBootstrapperWhenReady()
        {
            yield return new WaitUntil(() => StageBootstrapper.Instance != null);
            StageBootstrapper.Instance.OnSpawnedLaboratory(this);
        }

        public void OnLeftMouseDownThisObject()
        {

        }

        // 빌더가 연구소를 통해 강화 UI를 띄우기
        public void OnLeftMouseUpThisObject()
        {
            StageBootstrapper.Instance?.TryOpenLaboratoryUI(this);
        }

        public void OnCancelClickThisObject()
        {
        }

        public void InjectBuilderUI(PlayerBuilderUI builderUI)
        {
            _builderUI = builderUI;
        }

        public bool TryGetBuilderUI(out PlayerBuilderUI builderUI)
        {
            builderUI = _builderUI;
            return builderUI != null;
        }
    }
}