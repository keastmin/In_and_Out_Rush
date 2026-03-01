using System.Collections;
using UnityEngine;

namespace Dev.Network
{
    public class StageBootstrapper : Entity
    {
        [SerializeField] private StageManager _stageManager;
        [SerializeField] private ResourceSpawnSystem _resourceSpawnSystem;

        protected override void OnInitialize()
        {
            StartCoroutine(InitializeRoutine());
        }

        private IEnumerator InitializeRoutine()
        {
            yield return new WaitUntil(IsSessionReady); // OK

            if (Object.HasStateAuthority)
                yield return InitializeHost();
            
            yield return new WaitUntil(IsHostInitialized);

            CreateObjects();
            InitializeObjects();
            BindObjects();
            SetUpObjects();
        }

        private bool IsSessionReady()
            => NetworkManager.Instance != null && NetworkManager.Instance.Registry != null;

        private IEnumerator InitializeHost()
        {
            yield return new WaitUntil(() => _stageManager.IsInitialized);

            // Debug.Log("StageBootstrapper: initialize host complete");
        }

        private bool IsHostInitialized()
            => true;

        private void CreateObjects() {}

        private void InitializeObjects()
        {
            _resourceSpawnSystem.SetUp();
        }

        private void BindObjects() {}
        private void SetUpObjects() {}
    }
}