using Fusion;
using UnityEngine;

public class TeleportTower : SupportTower, IRunnerInteractableTower
{
    [SerializeField] private float _moveCoolDown = 30f;

    public TeleportTower OtherTeleportTower;

    [Networked]
    public TickTimer CoolDown { get; set; }

    protected override void TowerAwake()
    {
        SetId(TowerIDContainer.TELEPORT_TOWER_ID);
    }

    protected override void TowerSpawned()
    {
        TowerManager.Instance.AddTowerID(TowerID);
        TeleportTowerPairManager.Instance.AddTeleportTower(this); // 텔레포트 타워 등록
    }

    protected override void TowerDespawned()
    {
        TowerManager.Instance.RemoveTowerID(TowerID);
        TeleportTowerPairManager.Instance.RemoveTeleportTower(this); // 텔레포트 타워 해제
    }

    public void Interact(PlayerRunner runner)
    {
        if(OtherTeleportTower != null)
        {
            if (CoolDown.ExpiredOrNotRunning(Runner) && OtherTeleportTower.CoolDown.ExpiredOrNotRunning(Runner))
            {
                Vector3 position = OtherTeleportTower.transform.position + Vector3.forward * 2f;

                Debug.Log("텔레포트 요청");
                runner.TeleportTo(position);
            }
        }
    }

    // 쿨타임을 적용하는 함수
    public void SetCoolDown()
    {
        RPC_SetCoolDown();
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
