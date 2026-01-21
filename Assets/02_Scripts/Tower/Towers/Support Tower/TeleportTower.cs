using Fusion;
using UnityEngine;

public class TeleportTower : SupportTower, IRunnerInteractableTower
{
    [SerializeField] private float _moveCoolDown = 30f;

    [Networked]
    public NetworkObject OtherTeleportTower { get; set; }

    [Networked]
    public TickTimer CoolDown { get; set; }

    protected override void TowerAwake()
    {
        SetId(TowerIDContainer.TELEPORT_TOWER_ID);
    }

    public void Interact(PlayerRunner runner)
    {
        if(OtherTeleportTower != null)
        {
            OtherTeleportTower.TryGetComponent(out TeleportTower otherTower);
            if (CoolDown.ExpiredOrNotRunning(Runner) && otherTower.CoolDown.ExpiredOrNotRunning(Runner))
            {
                // runner.Teleport(_otherTeleportTower.transform);
            }
        }
    }

    public void InjectionOtherTeleportReference(TeleportTower tower)
    {
        if (tower.TryGetComponent(out NetworkObject no))
        {
            RPC_InjectionReference(no);
        }
    }

    public void RemoveOherTowerRefence()
    {
        RPC_InjectionReference(null);
    }

    // 쿨타임을 적용하는 함수
    public void SetCoolDown()
    {
        RPC_SetCoolDown();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_InjectionReference(NetworkObject no)
    {
        if (HasStateAuthority)
        {
            OtherTeleportTower = no;
        }
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
