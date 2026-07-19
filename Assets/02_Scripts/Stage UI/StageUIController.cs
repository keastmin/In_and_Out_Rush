using UnityEngine;
using UnityEngine.Events;
using KIM.Dev;
using Dev.Network;

public class StageUIController : MonoBehaviour
{
    public PlayerBuilderUI BuilderUI; // 플레이어 빌더가 보게 될 UI 오브젝트
    public PlayerRunnerUI RunnerUI; // 플레이어 러너가 보게 될 UI 오브젝트

    private PlayerRunner _playerRunner; // 플레이어 러너 참조

    private void Awake()
    {
        SetDisableAllUI(); // 시작할 때 모든 UI 비활성화
    }

    public void InitializeStageUIController(TowerUpgradeManager towerUpgradeManager, ResourceSystem resourceSystem, TimeSystem timeSystem)
    {
        BuilderUI.InitializePlayerBuilderUI(towerUpgradeManager, resourceSystem, timeSystem);
    }

    /// <summary>
    /// 자신의 역할군에 따라 맞는 UI를 활성화
    /// </summary>
    /// <param name="playerPosition">자신의 역할군</param>
    public void SetPlayerUI(PlayerPosition playerPosition)
    {
        switch (playerPosition)
        {
            case PlayerPosition.Builder:
                BuilderUI.gameObject.SetActive(true);
                break;
            case PlayerPosition.Runner:
                RunnerUI.gameObject.SetActive(true);
                break;
        }
    }

    /// <summary>
    /// 모든 UI를 비활성화
    /// </summary>
    public void SetDisableAllUI()
    {
        BuilderUI.gameObject.SetActive(false);
        RunnerUI.gameObject.SetActive(false);
    }

    /// <summary>
    /// 플레이어 러너 참조를 받아서 저장하는 함수
    /// </summary>
    /// <param name="playerRunner">플레이어 러너 참조</param>
    public void GetPlayerRunnerReference(PlayerRunner playerRunner)
    {
        _playerRunner = playerRunner;
        BuilderUI.GetPlayerRunnerReference(playerRunner);
    }
}
