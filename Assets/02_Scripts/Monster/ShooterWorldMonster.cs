using System;
using Fusion;
using ProjectIO.Monsters;
using UnityEngine;

public class ShooterWorldMonster : WorldMonster
{
    protected override bool CanReceiveKnockback => false;
    protected override bool CanRestoreZeroHealth => true;

    [Header("Rafflesia Attack")]
    [SerializeField, Min(0f)] private float detectionRadius = 10f;
    [SerializeField, Min(0f)] private float projectileSpeed = 1f;
    [SerializeField, Min(0f)] private float projectileDamage = 1f;
    [SerializeField, Min(0.01f)] private float projectileSize = 1f;
    [SerializeField, Min(0.01f)] private float projectileLifetime = 5f;
    [SerializeField, Min(0.01f)] private float projectileMaximumRange = 5f;
    [SerializeField] private Transform muzzle;
    [SerializeField] private MonsterProjectile projectilePrefab;

    [Header("Disabled Appearance")]
    [SerializeField] private Renderer[] bodyRenderers;
    [SerializeField] private Material disabledMaterial;
    [SerializeField] private Color disabledColor = new Color(0.2f, 0.2f, 0.2f, 1f);

    [Networked] public NetworkBool IsDisabled { get; private set; }
    public event Action<ShooterWorldMonster> CapturedByTerritory;

    private readonly RafflesiaAttackPattern _pattern = new();
    private readonly float[] _angles = new float[8];
    private bool _captured;
    private bool _appearanceDisabled;
    private Material[][] _originalMaterials;
    private MaterialPropertyBlock[] _originalProperties;
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    public override void Initialize()
    {
        IsDisabled = false;
        _captured = false;
        _pattern.Reset();
        StopMovement();
    }

    public override void Spawned()
    {
        base.Spawned();
        if (bodyRenderers == null || bodyRenderers.Length == 0)
            bodyRenderers = GetComponentsInChildren<Renderer>(true);
        _originalMaterials = new Material[bodyRenderers.Length][];
        _originalProperties = new MaterialPropertyBlock[bodyRenderers.Length];
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] == null)
                continue;
            _originalMaterials[i] = bodyRenderers[i].sharedMaterials;
            _originalProperties[i] = new MaterialPropertyBlock();
            bodyRenderers[i].GetPropertyBlock(_originalProperties[i]);
        }
        _appearanceDisabled = false;
    }

    public override void UpdateMonster()
    {
        StopMovement();
        if (IsDisabled || !HasVisibleTarget() || projectilePrefab == null)
        {
            _pattern.Reset();
            return;
        }

        int count = _pattern.TryEmit(Runner.SimulationTime, _angles);
        for (int i = 0; i < count; i++)
        {
            float angle = _angles[i] * Mathf.Deg2Rad;
            Fire(new Vector3(Mathf.Sin(angle), 0f, Mathf.Cos(angle)));
        }
    }

    private bool HasVisibleTarget()
    {
        if (playerTransform == null || IsTargetInRunnerSafeZone(playerTransform))
            return false;
        PlayerRunner player = playerTransform.GetComponent<PlayerRunner>();
        if (player != null && player.IsDead)
            return false;
        Vector3 offset = playerTransform.position - RigidbodyPosition;
        offset.y = 0f;
        return offset.sqrMagnitude <= detectionRadius * detectionRadius;
    }

    protected override void StopByStun()
    {
        base.StopByStun();
        if (!HasVisibleTarget())
            _pattern.Reset();
    }

    protected override void StopMovement()
    {
        if (rigidBody != null && !rigidBody.isKinematic)
            base.StopMovement();
    }

    protected override void OnHealthDepleted() => DisablePermanently();

    public void RestoreDisabledState(bool disabled)
    {
        if (disabled)
            DisablePermanently();
    }

    private void DisablePermanently()
    {
        if (!CanAccessNetworkState || !HasStateAuthority || IsDisabled)
            return;
        IsDisabled = true;
        _pattern.Reset();
        StopMovement();
    }

    private void OnTriggerEnter(Collider other) => HandleContact(other);
    private void OnTriggerStay(Collider other) => HandleContact(other);

    private void HandleContact(Collider other)
    {
        if (other != null && other.GetComponentInParent<PlayerRunner>() is PlayerRunner player && !player.IsDead)
            DisablePermanently();
    }

    protected override void OnCapturedByTerritory()
    {
        if (!CanAccessNetworkState || !HasStateAuthority || _captured)
            return;
        _captured = true;
        CapturedByTerritory?.Invoke(this);
        if (Object != null && Object.IsValid)
            base.OnCapturedByTerritory();
    }

    private void Fire(Vector3 direction)
    {
        Vector3 origin = muzzle != null ? muzzle.position : RigidbodyPosition + Vector3.up;
        MonsterProjectile projectile = Runner.Spawn(
            projectilePrefab, origin, Quaternion.LookRotation(direction, Vector3.up));
        if (projectile == null)
            return;
        projectile.Initialize(this, direction, projectileSpeed, projectileDamage,
            projectileLifetime, projectileMaximumRange, projectileSize,
            IsPositionInRunnerSafeZone, ignoreSameOwnerProjectiles: true);
    }

    public override void Render()
    {
        if (Object != null && Object.IsValid && _appearanceDisabled != (bool)IsDisabled)
            SetDisabledAppearance(IsDisabled);
    }

    private void SetDisabledAppearance(bool disabled)
    {
        if (_originalMaterials == null)
            return;
        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer body = bodyRenderers[i];
            if (body == null)
                continue;
            if (disabled)
            {
                if (disabledMaterial != null)
                {
                    var materials = new Material[_originalMaterials[i].Length];
                    for (int j = 0; j < materials.Length; j++)
                        materials[j] = disabledMaterial;
                    body.sharedMaterials = materials;
                }
                var properties = new MaterialPropertyBlock();
                body.GetPropertyBlock(properties);
                properties.SetColor(BaseColorId, disabledColor);
                properties.SetColor(ColorId, disabledColor);
                body.SetPropertyBlock(properties);
            }
            else
            {
                body.sharedMaterials = _originalMaterials[i];
                body.SetPropertyBlock(_originalProperties[i]);
            }
        }
        _appearanceDisabled = disabled;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        SetDisabledAppearance(false);
        CapturedByTerritory = null;
        base.Despawned(runner, hasState);
    }

#if UNITY_EDITOR
    protected override void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
#endif
}
