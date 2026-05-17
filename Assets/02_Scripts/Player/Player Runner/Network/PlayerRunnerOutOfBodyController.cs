using Fusion;
using Fusion.Addons.Physics;
using UnityEngine;

[RequireComponent(typeof(PlayerRunner))]
public class PlayerRunnerOutOfBodyController : NetworkBehaviour
{
    [Networked] private TickTimer _outOfBodyTimer { get; set; }
    [SerializeField] private NetworkObject _outOfBodyPrefab;

    private bool _isOutOfBodyActive;
    private NetworkObject _outOfBodySpiritObject;
    private Transform _currentTargetTransform;
    private bool _isRecovering;
    private Vector3 _recoveryTargetPosition;

    private PlayerRunner _playerRunner;

    public bool IsOutOfBodyActive => _isOutOfBodyActive;
    public Transform CurrentTargetTransform => _currentTargetTransform;
    public bool IsRecovering => _isRecovering;
    public Vector3 RecoveryTargetPosition => _recoveryTargetPosition;

    public override void Spawned()
    {
        _playerRunner = GetComponent<PlayerRunner>();
        _currentTargetTransform = transform;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (!_isOutOfBodyActive || _outOfBodySpiritObject == null) return;

        if (_outOfBodyTimer.Expired(Runner))
            EndOutOfBody();
    }

    public void StartOutOfBody()
    {
        if (_isOutOfBodyActive) return;
        _isOutOfBodyActive = true;
        RPC_StartOutOfBody();
    }

    public void EndOutOfBody()
    {
        if (!_isOutOfBodyActive) return;
        RPC_EndOutOfBody();
        _isOutOfBodyActive = false;
    }

    public void ClearRecovering()
    {
        _isRecovering = false;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_StartOutOfBody()
    {
        _outOfBodyTimer = TickTimer.CreateFromSeconds(Runner, 2.0f);
        _outOfBodySpiritObject = Runner.Spawn(_outOfBodyPrefab, transform.position);
        _outOfBodySpiritObject.GetComponent<NetworkRigidbody3D>()
            .Teleport(transform.position + transform.forward * 1f);
        _outOfBodySpiritObject.GetComponent<Rigidbody>().linearVelocity =
            transform.forward * _playerRunner.EffectiveMovementSpeed;
        _currentTargetTransform = _outOfBodySpiritObject.transform;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_EndOutOfBody()
    {
        _recoveryTargetPosition = _outOfBodySpiritObject.transform.position;
        transform.GetComponent<NetworkRigidbody3D>().Teleport(_recoveryTargetPosition);
        _currentTargetTransform = transform;
        _isRecovering = true;
        Runner.Despawn(_outOfBodySpiritObject);
        _outOfBodySpiritObject = null;
    }
}
