using System.Collections;
using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public partial class StageBootstrapper : Entity
    {
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

            CreateObjects(); // 오브젝트 스폰
            InitializeObjects(); // 스폰된 오브젝트를 초기화
            BindObjects(); // 참조 연결
            SetUpObjects(); // 참조 연결 부재로 인해 보류된 초기화 로직 실행, 후순위 초기화 로직 실행
        }

        private bool IsSessionReady()
            => NetworkManager.Instance != null && NetworkManager.Instance.Registry != null;

        private IEnumerator InitializeHost()
        {
            yield return new WaitUntil(() => _stageManager.IsInitialized);

            YOUInitializeHost();
            KIMInitializeHost();
        }

        private bool IsHostInitialized()
            => true;

        private void CreateObjects() 
        {
            YOUCreateObjects();
            KIMCreateObjects();
        }

        private void InitializeObjects()
        {
            YOUInitializeObjects();
            KIMInitializeObjects();
        }

        private void BindObjects()
        {
            YOUBindObjects();
            KIMBindObjects();
        }

        private void SetUpObjects()
        {
            YOUSetUpObjects();
            KIMSetUpObjects();
        }
    }
}