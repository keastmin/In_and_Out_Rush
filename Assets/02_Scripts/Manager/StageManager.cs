using Fusion;
using System.Collections;
using UnityEngine;

public class StageManager : NetworkBehaviour
{
    [Networked] public PlayerRunner PlayerRunner { get; set; }
    [Networked] public PlayerBuilder PlayerBuilder { get; set; }
    [Networked] public Laboratory Laboratory { get; set; }
    public CinemachineSystem CinemachineSystem { get; private set; }

    public static StageManager Instance { get; private set; }

    [Space(10)]

    [Header("Player")]
    [SerializeField] private PlayerRunner playerRunnerPrefab; // 러너 플레이어 프리팹
    [SerializeField] private PlayerBuilder playerBuilderPrefab; // 빌더 플레이어 프리팹

    [Space(10)]

    [Header("Network Systems")]
    [SerializeField] private NetworkInputSystem networkInputSystemPrefab;
    [SerializeField] private NetworkSystemBase[] systems;
    public ResourceSystem ResourceSystem;

    [Space(10)]

    [Header("Local Systems")]
    [SerializeField] private CinemachineSystem cinemachineSystemPrefab; // 시네머신 시스템 프리팸
    public StageUIController UIController; // 이 게임 스테이지의 UI 컨트롤러
    public HexagonGrid Grid; // 헥사곤 그리드 컴포넌트

    [Space(10)]

    [Header("Territory")]
    public TerritoryView TerritoryView;
    public TrackView TrackView;

    bool _initialized = false;

    public override void Spawned()
    {
        Debug.Log("스폰 작동");
        Instance = this;

        if (!_initialized)
        {
            StartCoroutine(Co_InitAfterNetworkManagerReady());
        }
    }

    // 네트워크 매니저의 스폰을 대기하는 코루틴
    private IEnumerator Co_InitAfterNetworkManagerReady()
    {
        // NetworkManager/Registry 준비 대기
        while (NetworkManager.Instance == null || NetworkManager.Instance.Registry == null)
            yield return null;

        Debug.Log("NetworkManager 인식 성공");

        if (HasStateAuthority)
        {
            SpawnPlayer();
            SpawnNetworkInputSystem();
            SpawnLaboratory();
        }

        foreach (var system in systems)
        {
            system.SetUp();
        }

        // 로컬 시스템 - 시네머신 초기화
        InitCinemachineSystem();

        // 로컬 시스템 - UI 초기화
        InitUIController();

        // 연구소 UI에 러너 참조 주입
        LaboratoryUIInjectionPlayerRunner(UIController.BuilderUI, PlayerRunner);

        // 마지막으로 빌더에게 필요한 참조들 바인드 해주기
        BuilderReferenceBind(
            PlayerBuilder,
            UIController.BuilderUI,
            Grid,
            Laboratory);

        Debug.Log("셋업 완료");

        _initialized = true;
    }

    void SpawnPlayer()
    {
        var runnerPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Runner);
        if(runnerPlayer == PlayerRef.None) PlayerRunner = Runner.Spawn(ResourceManager.Instance.PlayerRunnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity);
        else PlayerRunner = Runner.Spawn(ResourceManager.Instance.PlayerRunnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity, runnerPlayer);
        PlayerRunner.name = $"{Runner.name} - Player Runner";

        var builderPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Builder);
        if(builderPlayer == PlayerRef.None) PlayerBuilder = Runner.Spawn(ResourceManager.Instance.PlayerBuilderPrefab, Vector3.zero, Quaternion.identity);
        else PlayerBuilder = Runner.Spawn(ResourceManager.Instance.PlayerBuilderPrefab, Vector3.zero, Quaternion.identity, builderPlayer);
        PlayerBuilder.name = $"{Runner.name} - Player Builder";

        Debug.Log($"{Runner.name} - Player spawned");
    }

    void SpawnNetworkInputSystem()
    {
        var instance = Runner.Spawn(networkInputSystemPrefab, Vector3.zero, Quaternion.identity);
        instance.name = $"{Runner.name} - NetworkInputSystem";
        Debug.Log($"{Runner.name} - NetworkInputSystem spawned");
    }

    // 정해진 역할군에 따라 시네머신 카메라를 초기화
    void InitCinemachineSystem()
    {
        var instance = Instantiate(cinemachineSystemPrefab); // 시네머신 시스템 인스턴스 생성
        instance.InitCinemachineCamera(NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer], PlayerRunner, PlayerBuilder);
        CinemachineSystem = instance;
    }

    // 정해진 역할군에 따라 표현되는 UI를 초기화, 타워 슬롯 버튼을 설정
    private void InitUIController()
    {
        var playerPosition = NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer]; // 현재 자신의 역할군을 가져옴
        UIController.SetPlayerUI(playerPosition); // 자신의 역할군에 따라 UI를 설정
    }

    // 연구소 스폰
    private void SpawnLaboratory()
    {
        // 연구소 차지 셀 인덱스 범위: [x, y], [x - 1, y], [x, y - 1], [x + 1, y], [x - 1, y + 1], [x, y + 1], [x + 1, y + 1]
        Vector2Int centerIndex = Grid.GetCenterIndex();

        // 연구소 생성
        Vector3 labPos = Grid.GetNearCellPositionFromIndex(centerIndex);
        Laboratory = Runner.Spawn(ResourceManager.Instance.LaboratoryPrefab, labPos, Quaternion.identity);

        // 연구소가 위치한 셀 상태 변경
        Grid.TryOccupyArea(centerIndex, 1, CellState.Laborarytory);
    }

    #region 참조 주입

    // 연구소 UI에 PlayerRunner 참조 주입
    private void LaboratoryUIInjectionPlayerRunner(PlayerBuilderUI builderUI, PlayerRunner runner)
    {
        builderUI.LaboratoryUIInjectionRunner(runner);
    }

    // 빌더에게 필요한 참조 주입
    private void BuilderReferenceBind(PlayerBuilder builder, PlayerBuilderUI builderUI, HexagonGrid hexagonGrid, Laboratory laboratory)
    {
        // 플레이어 빌더에게 참조 주입
        builder.PlayerBuilderReferenceInjection(
            builderUI,
            hexagonGrid,
            laboratory);
    }

    #endregion
}