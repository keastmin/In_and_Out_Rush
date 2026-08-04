using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace KIM.Dev
{
    public class TestModeGameSceneSetup : MonoBehaviour
    {
        [Header("테스트 환경 세팅")]
        [SerializeField] private string _testSessionName = "Test";
        [SerializeField] private PlayerPosition _playerPosition = PlayerPosition.Builder; // 플레이어 역할군

        [Space(10)]
        [Header("네트워크 환경 구성 프리팹")]
        [SerializeField] private NetworkRunner _runnerPrefab; // 러너 프리팹
        [SerializeField] private NetworkManager _networkManagerPrefab; // 네트워크 매니저 프리팹
        [SerializeField] private ResourceManager _resourceManagerPrefab; // 리소스 매니저 프리팹

        private bool _isBuildingEnvironment;

        private void Awake()
        {
            if (NetworkManager.Instance == null)
            {
                BuildTestModeEnvironment();
            }
        }

        // 테스트 모드 환경 구축
        public async void BuildTestModeEnvironment()
        {
            if (_isBuildingEnvironment)
                return;

            _isBuildingEnvironment = true;

            if (_runnerPrefab == null || _networkManagerPrefab == null || _resourceManagerPrefab == null)
            {
                Debug.LogError(
                    $"{nameof(TestModeGameSceneSetup)} 프리팹 참조가 비어 있습니다. " +
                    $"Runner={_runnerPrefab}, NetworkManager={_networkManagerPrefab}, ResourceManager={_resourceManagerPrefab}",
                    this);
                _isBuildingEnvironment = false;
                return;
            }
            // 리소스 매니저 생성
            Instantiate(_resourceManagerPrefab);

            // 러너 생성
            NetworkRunner runner = Instantiate(_runnerPrefab);

            // 러너의 NetworkEvents 컴포넌트를 찾아 PlayerJoined에 이벤트 리스너 추가
            NetworkEvents networkEvents = runner.GetComponent<NetworkEvents>();
            if (networkEvents == null)
            {
                Debug.LogError("테스트 모드 러너에 NetworkEvents 컴포넌트가 없습니다.", runner);
                _isBuildingEnvironment = false;
                return;
            }

            networkEvents.PlayerJoined.AddListener((joinedRunner, player) =>
            {
                if (!joinedRunner.IsServer || joinedRunner.LocalPlayer != player || NetworkManager.Instance != null)
                    return;

                NetworkManager networkManager = joinedRunner.Spawn(_networkManagerPrefab);

                if (networkManager == null)
                {
                    Debug.LogError(
                        "Network Manager 스폰에 실패했습니다.",
                        _networkManagerPrefab);
                    return;
                }

                networkManager.SetTestModeVariable(true, _playerPosition);
            });

            // 입력을 제공하도록 설정
            runner.ProvideInput = true;

            var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

            // 게임 시작 인자 설정
            try
            {
                var result = await runner.StartGame(new StartGameArgs
                {
                    GameMode = GameMode.Host, // 호스트 모드로 시작
                    PlayerCount = 2, // 최대 2명으로 설정
                    SessionName = _testSessionName, // 룸 코드 적용
                    Scene = sceneRef,
                    SceneManager = runner.GetComponent<NetworkSceneManagerDefault>() // 기본 씬 매니저 사용
                });

                // 룸 생성 성공
                if (result.Ok)
                {
                    Debug.Log("테스트 룸 생성 성공");
                }
            }
            finally
            {
                _isBuildingEnvironment = false;
            }
        }
    }

}
