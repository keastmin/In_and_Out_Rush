using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(PlayerRunner))]
public class PlayerRunnerTeleporter : NetworkBehaviour
{
    public void TeleportTo(Vector3 position)
    {
        RPC_TeleportTo(position);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_TeleportTo(Vector3 position)
    {
        if (!HasStateAuthority) return;

        TryGetComponent(out NetworkRigidbody3D networkRigidbody);
        networkRigidbody.Teleport(position, transform.rotation);
    }
}
