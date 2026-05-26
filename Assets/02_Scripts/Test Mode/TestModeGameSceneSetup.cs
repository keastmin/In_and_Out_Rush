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
            // 리소스 매니저 생성
            Instantiate(_resourceManagerPrefab);

            // 러너 생성
            NetworkRunner runner = Instantiate(_runnerPrefab);

            // 러너의 NetworkEvents 컴포넌트를 찾아 PlayerJoined에 이벤트 리스너 추가
            runner.GetComponent<NetworkEvents>().PlayerJoined.AddListener((runner, player) =>
            {
                if (runner.IsServer && runner.LocalPlayer == player)
                {
                    NetworkManager networkManager = runner.Spawn(_networkManagerPrefab); // 네트워크 매니저 스폰
                    networkManager.SetTestModeVariable(true, _playerPosition); // 테스트 모드 활성화
                }
            });

            // 입력을 제공하도록 설정
            runner.ProvideInput = true;

            var sceneRef = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex);

            // 게임 시작 인자 설정
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
    }

}