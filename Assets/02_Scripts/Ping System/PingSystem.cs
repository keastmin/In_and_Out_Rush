using Fusion;
using UnityEngine;

public class PingSystem : NetworkBehaviour
{
    [SerializeField] private ParticleSystem _ping1Particle;
    [SerializeField] private LayerMask _groundLayer;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftAlt))
        {
            if (Input.GetMouseButtonDown(0))
            {
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

                if(Physics.Raycast(ray, out hit, 5000f, _groundLayer))
                {
                    RPC_SendPing(hit.point);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_SendPing(Vector3 pingPos)
    {
        Instantiate(_ping1Particle, pingPos, Quaternion.identity);
    }
}