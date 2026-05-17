using Fusion;
using UnityEngine;

public class TeleportTower : SupportTower, IRunnerInteractableTower
{
    [SerializeField] private float _moveCoolDown = 30f;
    [SerializeField] private float _teleportExitDistance = 2f;

    public TeleportTower OtherTeleportTower;

    [Networked]
    public TickTimer CoolDown { get; set; }

    protected override void TowerAwake()
    {
        SetId(TowerIDContainer.TELEPORT_TOWER_ID);
    }

    public override void Spawned()
    {
        base.Spawned();

        TowerManager.Instance.AddTowerID(TowerID);
        TeleportTowerPairManager.Instance.AddTeleportTower(this); // 텔레포트 타워 등록
        if (OtherTeleportTower == null && TeleportTowerPairManager.Instance.RegisteredTowerCount >= TeleportTowerPairManager.MaxTeleportTowerCount && HasStateAuthority)
        {
            Runner.Despawn(Object);
        }
    }

    protected override void TowerDespawned()
    {
        TowerManager.Instance.RemoveTowerID(TowerID);
        TeleportTowerPairManager.Instance.RemoveTeleportTower(this); // 텔레포트 타워 해제
    }

    public void Interact(PlayerRunner runner)
    {
        if (runner == null || OtherTeleportTower == null)
            return;

        if (!CoolDown.ExpiredOrNotRunning(Runner) || !OtherTeleportTower.CoolDown.ExpiredOrNotRunning(Runner))
            return;

        runner.TeleportTo(GetExitPosition());
    }

    // 쿨타임을 적용하는 함수
    public void SetCoolDown()
    {
        RPC_SetCoolDown();
    }

    public void SetPairCoolDown()
    {
        SetCoolDown();
        if (OtherTeleportTower != null)
        {
            OtherTeleportTower.SetCoolDown();
        }
    }

    private Vector3 GetExitPosition()
    {
        Vector3 exitDirection = OtherTeleportTower.transform.position - transform.position;
        exitDirection.y = 0f;

        if (exitDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            exitDirection = OtherTeleportTower.transform.forward;
        }

        Vector3 exitPosition = OtherTeleportTower.transform.position + exitDirection.normalized * _teleportExitDistance;
        exitPosition.y = OtherTeleportTower.transform.position.y;
        return exitPosition;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetCoolDown()
    {
        if (HasStateAuthority)
        {
            CoolDown = TickTimer.CreateFromSeconds(Runner, _moveCoolDown);
        }
    }
}
