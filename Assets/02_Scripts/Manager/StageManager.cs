using Fusion;
using Grid;
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
    [SerializeField] private PlayerRunner playerRunnerPrefab;
    [SerializeField] private PlayerBuilder playerBuilderPrefab;

    [Space(10)]

    [Header("Network Systems")]
    [SerializeField] private NetworkInputSystem networkInputSystemPrefab;
    [SerializeField] private NetworkSystemBase[] systems;
    public ResourceSystem ResourceSystem;

    [Space(10)]

    [Header("Local Systems")]
    [SerializeField] private CinemachineSystem cinemachineSystemPrefab;
    public StageUIController UIController;

    [Space(10)]

    [Header("Territory")]
    public TerritoryView TerritoryView;
    public TrackView TrackView;

    private bool _initialized = false;

    public override void Spawned()
    {
        Debug.Log("StageManager Spawned");
        Instance = this;

        if (!_initialized)
        {
            StartCoroutine(Co_InitAfterNetworkManagerReady());
        }
    }

    private IEnumerator Co_InitAfterNetworkManagerReady()
    {
        while (NetworkManager.Instance == null || NetworkManager.Instance.Registry == null)
            yield return null;

        Debug.Log("NetworkManager ready");

        if (HasStateAuthority)
        {
            SpawnPlayer();
            SpawnNetworkInputSystem();
            SpawnLaboratory();
        }

        while (PlayerRunner == null || PlayerBuilder == null || Laboratory == null)
            yield return null;

        foreach (var system in systems)
        {
            system.SetUp();
        }

        InitCinemachineSystem();
        InitUIController();

        LaboratoryUIInjectionPlayerRunner(UIController.BuilderUI, PlayerRunner);
        BuilderReferenceBind(PlayerBuilder, UIController.BuilderUI, Laboratory);

        Debug.Log("StageManager init complete");
        _initialized = true;
    }

    private void SpawnPlayer()
    {
        var runnerPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Runner);
        if (runnerPlayer == PlayerRef.None)
            PlayerRunner = Runner.Spawn(ResourceManager.Instance.PlayerRunnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity);
        else
            PlayerRunner = Runner.Spawn(ResourceManager.Instance.PlayerRunnerPrefab, Vector3.zero - (Vector3.forward * 4f), Quaternion.identity, runnerPlayer);
        PlayerRunner.name = $"{Runner.name} - Player Runner";

        var builderPlayer = NetworkManager.Instance.Registry.GetPlayerRefFromPosition(PlayerPosition.Builder);
        if (builderPlayer == PlayerRef.None)
            PlayerBuilder = Runner.Spawn(ResourceManager.Instance.PlayerBuilderPrefab, Vector3.zero, Quaternion.identity);
        else
            PlayerBuilder = Runner.Spawn(ResourceManager.Instance.PlayerBuilderPrefab, Vector3.zero, Quaternion.identity, builderPlayer);
        PlayerBuilder.name = $"{Runner.name} - Player Builder";

        Debug.Log($"{Runner.name} - Player spawned");
    }

    private void SpawnNetworkInputSystem()
    {
        var instance = Runner.Spawn(networkInputSystemPrefab, Vector3.zero, Quaternion.identity);
        instance.name = $"{Runner.name} - NetworkInputSystem";
        Debug.Log($"{Runner.name} - NetworkInputSystem spawned");
    }

    private void SpawnLaboratory()
    {
        if (GridManager.Instance == null)
        {
            Debug.LogError("GridManager.Instance is null. Failed to spawn Laboratory.");
            return;
        }

        Vector3 labPos = GridManager.Instance.GetCenterCellWorldPosition();
        Laboratory = Runner.Spawn(ResourceManager.Instance.LaboratoryPrefab, labPos, Quaternion.identity);
        Laboratory.name = $"{Runner.name} - Laboratory";
    }

    private void InitCinemachineSystem()
    {
        var instance = Instantiate(cinemachineSystemPrefab);
        instance.InitCinemachineCamera(NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer], PlayerRunner, PlayerBuilder);
        CinemachineSystem = instance;
    }

    private void InitUIController()
    {
        var playerPosition = NetworkManager.Instance.Registry.RefToPosition[Runner.LocalPlayer];
        UIController.SetPlayerUI(playerPosition);
    }

    private void LaboratoryUIInjectionPlayerRunner(PlayerBuilderUI builderUI, PlayerRunner runner)
    {
        builderUI.LaboratoryUIInjectionRunner(runner);
    }

    private void BuilderReferenceBind(PlayerBuilder builder, PlayerBuilderUI builderUI, Laboratory laboratory)
    {
        builder.PlayerBuilderReferenceInjection(builderUI, laboratory);
    }
}
