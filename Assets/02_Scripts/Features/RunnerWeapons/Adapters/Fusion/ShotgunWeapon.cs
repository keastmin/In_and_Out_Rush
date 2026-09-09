using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using ProjectIO.RunnerWeapons;
using UnityEngine;

public sealed class ShotgunWeapon : RunnerWeaponNetworkBehaviour
{
    [Header("Shotgun Origin")]
    [SerializeField] private Transform _muzzle;
    [SerializeField] private LayerMask _damageableMask = ~0;

    [Header("Shotgun Damage")]
    [SerializeField, Min(0f)] private float _nearDamage = 36f;
    [SerializeField, Min(0f)] private float _middleDamage = 18f;
    [SerializeField, Min(0f)] private float _farDamage = 9f;
    [SerializeField, Min(0f)] private float _nearRange = 3f;
    [SerializeField, Min(0f)] private float _middleRange = 5f;
    [SerializeField, Min(0f)] private float _maximumRange = 7f;
    [SerializeField, Min(0f)] private float _fullConeDegrees = 24f;

    [Header("Shotgun Accuracy And Presentation")]
    [SerializeField, Range(0f, 100f)] private float _runningAccuracyPercent = 50f;
    [SerializeField, Min(1)] private int _presentationPelletCount = 8;
    [SerializeField] private bool _showTracers = true;
    [SerializeField] private Color _tracerColor = new(1f, 0.8f, 0.25f, 1f);
    [SerializeField, Min(0.01f)] private float _tracerDuration = 0.1f;
    [SerializeField, Min(0.001f)] private float _tracerStartWidth = 0.035f;
    [SerializeField, Min(0.001f)] private float _tracerEndWidth = 0.008f;

    [Header("Shotgun Knockback")]
    [SerializeField, Min(0f)] private float _knockbackDistance = 1f;
    [SerializeField, Min(0.01f)] private float _knockbackDuration = 0.2f;

    [Networked] private NetworkRNG HitRandom { get; set; }
    [Networked] private int LastPelletSeed { get; set; }

    private readonly List<Candidate> _candidates = new(32);
    private readonly Dictionary<WorldMonster, int> _candidateIndices = new(32);
    private readonly List<GameObject> _activeTracerObjects = new(16);
    private Material _tracerMaterial;
    private bool _tracerShaderUnavailable;

    public event Action<ShotgunShotPresentation> ShotgunShotPresented;
    public event Action<ShotgunTargetResultPresentation> TargetResultPresented;

    protected override bool UsesIncrementalReload => true;
    protected override bool CanInterruptReloadWithFire => true;

    public override void Spawned()
    {
        base.Spawned();

        if (!HasStateAuthority)
            return;

        int seed = unchecked((Runner.Tick.Raw * 397) ^ Object.InputAuthority.RawEncoded ^ 0x51ED270B);
        HitRandom = new NetworkRNG(seed == 0 ? 1 : seed);
        LastPelletSeed = 0;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        StopAllCoroutines();
        ClearTracerPresentation();
        ShotgunShotPresented = null;
        TargetResultPresented = null;
        _candidates.Clear();
        _candidateIndices.Clear();
        base.Despawned(runner, hasState);
    }

    protected override bool TryExecuteShot(
        PlayerRunner owner,
        Vector3 targetPosition,
        bool isRunning,
        RunnerWeaponHand hand,
        int shotSequence,
        out Vector3 shotDirection)
    {
        shotDirection = owner != null ? owner.transform.forward : Vector3.forward;
        if (_muzzle == null)
        {
            Debug.LogWarning($"{nameof(ShotgunWeapon)} requires a muzzle transform.", this);
            return false;
        }

        shotDirection = GetFireDirection(owner, targetPosition);

        NetworkRNG random = HitRandom;
        int pelletSeed = unchecked((int)(random.NextSingle() * int.MaxValue));
        LastPelletSeed = pelletSeed == 0 ? 1 : pelletSeed;

        CollectCandidates(owner, shotDirection);
        _candidates.Sort(CompareCandidates);

        float damageMultiplier = owner.WeaponDamage * owner.WeaponDamageScaler;
        float runningAccuracy = _runningAccuracyPercent * 0.01f;
        for (int candidateIndex = 0; candidateIndex < _candidates.Count; candidateIndex++)
        {
            Candidate candidate = _candidates[candidateIndex];
            NetworkId targetId = candidate.Monster.Object.Id;
            bool hit = ShotgunRules.IsTargetHit(
                isRunning,
                isRunning ? random.NextSingle() : 0f,
                runningAccuracy);

            float appliedDamage = 0f;
            bool knockbackApplied = false;
            if (hit)
            {
                float baseDamage = ShotgunRules.GetBaseDamage(
                    candidate.Distance,
                    _nearRange,
                    _middleRange,
                    _maximumRange,
                    _nearDamage,
                    _middleDamage,
                    _farDamage);
                appliedDamage = baseDamage * damageMultiplier;

                if (candidate.Distance <= _nearRange)
                {
                    Vector3 knockbackDirection = candidate.Point - _muzzle.position;
                    knockbackDirection.y = 0f;
                    knockbackApplied = candidate.Monster.TryApplyKnockback(
                        knockbackDirection,
                        _knockbackDistance,
                        _knockbackDuration);
                }

                candidate.Monster.TakeDamage(appliedDamage);
            }

            RPC_PresentTargetResult(
                targetId,
                candidate.Point,
                hit,
                appliedDamage,
                knockbackApplied,
                shotSequence);
        }

        HitRandom = random;
        return true;
    }

    protected override void OnShotPresented(
        int shotSequence,
        RunnerWeaponHand hand,
        Vector3 shotDirection)
    {
        int pelletCount = Mathf.Max(1, _presentationPelletCount);
        Vector3[] pelletDirections = new Vector3[pelletCount];
        for (int pelletIndex = 0; pelletIndex < pelletCount; pelletIndex++)
        {
            float angle = ShotgunRules.GetPelletAngleDegrees(
                LastPelletSeed,
                shotSequence,
                pelletIndex,
                _fullConeDegrees);
            pelletDirections[pelletIndex] =
                (Quaternion.AngleAxis(angle, Vector3.up) * shotDirection).normalized;
        }

        ShotgunShotPresented?.Invoke(new ShotgunShotPresentation(
            shotSequence,
            LastPelletSeed,
            shotDirection,
            pelletDirections));
        PresentTracers(pelletDirections);
    }

    private void PresentTracers(IReadOnlyList<Vector3> pelletDirections)
    {
        if (!_showTracers || _muzzle == null || pelletDirections == null)
            return;

        Material material = GetOrCreateTracerMaterial();
        if (material == null)
            return;

        PruneDestroyedTracers();
        Vector3 startPosition = _muzzle.position;
        float range = Mathf.Max(0f, _maximumRange);
        float startWidth = Mathf.Max(0.001f, _tracerStartWidth);
        float endWidth = Mathf.Max(0.001f, _tracerEndWidth);

        for (int pelletIndex = 0; pelletIndex < pelletDirections.Count; pelletIndex++)
        {
            Vector3 direction = pelletDirections[pelletIndex];
            if (!IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
                continue;

            GameObject tracerObject = new($"Shotgun Tracer {pelletIndex}");
            tracerObject.transform.SetParent(transform, true);
            LineRenderer tracer = tracerObject.AddComponent<LineRenderer>();
            tracer.sharedMaterial = material;
            tracer.useWorldSpace = true;
            tracer.positionCount = 2;
            tracer.alignment = LineAlignment.View;
            tracer.textureMode = LineTextureMode.Stretch;
            tracer.numCapVertices = 2;
            tracer.numCornerVertices = 2;
            tracer.startWidth = startWidth;
            tracer.endWidth = endWidth;
            tracer.startColor = _tracerColor;
            tracer.endColor = _tracerColor;
            tracer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            tracer.receiveShadows = false;
            tracer.SetPosition(0, startPosition);
            tracer.SetPosition(1, startPosition + direction.normalized * range);

            _activeTracerObjects.Add(tracerObject);
            StartCoroutine(FadeAndDestroyTracer(tracer, tracerObject));
        }
    }

    private IEnumerator FadeAndDestroyTracer(LineRenderer tracer, GameObject tracerObject)
    {
        float duration = Mathf.Max(0.01f, _tracerDuration);
        float elapsed = 0f;
        while (tracer != null && tracerObject != null && elapsed < duration)
        {
            float alpha = 1f - Mathf.Clamp01(elapsed / duration);
            Color color = _tracerColor;
            color.a *= alpha;
            tracer.startColor = color;
            tracer.endColor = color;
            elapsed += Time.deltaTime;
            yield return null;
        }

        _activeTracerObjects.Remove(tracerObject);
        if (tracerObject != null)
            Destroy(tracerObject);
    }

    private Material GetOrCreateTracerMaterial()
    {
        if (_tracerMaterial != null)
            return _tracerMaterial;
        if (_tracerShaderUnavailable)
            return null;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            _tracerShaderUnavailable = true;
            Debug.LogWarning($"{nameof(ShotgunWeapon)} could not find a tracer shader.", this);
            return null;
        }

        _tracerMaterial = new Material(shader)
        {
            name = "Shotgun Tracer Runtime Material",
            hideFlags = HideFlags.HideAndDontSave,
        };
        if (_tracerMaterial.HasProperty("_BaseColor"))
            _tracerMaterial.SetColor("_BaseColor", Color.white);
        if (_tracerMaterial.HasProperty("_Color"))
            _tracerMaterial.SetColor("_Color", Color.white);
        return _tracerMaterial;
    }

    private void PruneDestroyedTracers()
    {
        for (int tracerIndex = _activeTracerObjects.Count - 1; tracerIndex >= 0; tracerIndex--)
        {
            if (_activeTracerObjects[tracerIndex] == null)
                _activeTracerObjects.RemoveAt(tracerIndex);
        }
    }

    private void ClearTracerPresentation()
    {
        for (int tracerIndex = 0; tracerIndex < _activeTracerObjects.Count; tracerIndex++)
        {
            GameObject tracerObject = _activeTracerObjects[tracerIndex];
            if (tracerObject != null)
                Destroy(tracerObject);
        }
        _activeTracerObjects.Clear();

        if (_tracerMaterial != null)
            Destroy(_tracerMaterial);
        _tracerMaterial = null;
    }

    private void CollectCandidates(PlayerRunner owner, Vector3 shotDirection)
    {
        _candidates.Clear();
        _candidateIndices.Clear();

        Collider[] colliders = Physics.OverlapSphere(
            _muzzle.position,
            Mathf.Max(0f, _maximumRange),
            _damageableMask,
            QueryTriggerInteraction.Ignore);

        for (int colliderIndex = 0; colliderIndex < colliders.Length; colliderIndex++)
        {
            Collider collider = colliders[colliderIndex];
            WorldMonster monster = collider != null
                ? collider.GetComponentInParent<WorldMonster>()
                : null;
            if (monster == null ||
                monster.Object == null ||
                !monster.Object.IsValid)
            {
                continue;
            }

            Vector3 point = collider.bounds.ClosestPoint(_muzzle.position);
            Vector3 offset = point - _muzzle.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= 0.0001f)
            {
                point = collider.bounds.center;
                offset = point - _muzzle.position;
                offset.y = 0f;
            }
            float distance = offset.magnitude;
            if (!ShotgunRules.IsInsideCone(
                    shotDirection.x,
                    shotDirection.z,
                    offset.x,
                    offset.z,
                    _maximumRange,
                    _fullConeDegrees) ||
                !IsFirstVisibleCollider(owner, monster, point))
            {
                continue;
            }

            Candidate candidate = new(monster, point, distance);
            if (_candidateIndices.TryGetValue(monster, out int existingIndex))
            {
                if (distance < _candidates[existingIndex].Distance)
                    _candidates[existingIndex] = candidate;
                continue;
            }

            _candidateIndices.Add(monster, _candidates.Count);
            _candidates.Add(candidate);
        }
    }

    private bool IsFirstVisibleCollider(
        PlayerRunner owner,
        WorldMonster target,
        Vector3 targetPoint)
    {
        Vector3 offset = targetPoint - _muzzle.position;
        float distance = offset.magnitude;
        if (distance <= Mathf.Epsilon)
            return true;

        RaycastHit[] hits = Physics.RaycastAll(
            _muzzle.position,
            offset / distance,
            distance + 0.05f,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore);

        Collider firstCollider = null;
        float firstDistance = float.PositiveInfinity;
        for (int hitIndex = 0; hitIndex < hits.Length; hitIndex++)
        {
            Collider hitCollider = hits[hitIndex].collider;
            if (hitCollider == null || hitCollider.GetComponentInParent<PlayerRunner>() == owner)
                continue;

            float hitDistance = hits[hitIndex].distance;
            if (hitDistance >= firstDistance)
                continue;

            firstDistance = hitDistance;
            firstCollider = hitCollider;
        }

        return firstCollider != null &&
               firstCollider.GetComponentInParent<WorldMonster>() == target;
    }

    private Vector3 GetFireDirection(PlayerRunner owner, Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - _muzzle.position;
        direction.y = 0f;

        if (!IsFinite(direction) || direction.sqrMagnitude <= 0.0001f)
            direction = owner != null ? owner.transform.forward : transform.forward;

        direction.y = 0f;
        return IsFinite(direction) && direction.sqrMagnitude > 0.0001f
            ? direction.normalized
            : Vector3.forward;
    }

    private static int CompareCandidates(Candidate left, Candidate right)
    {
        return string.CompareOrdinal(
            left.Monster.Object.Id.ToString(),
            right.Monster.Object.Id.ToString());
    }

    private static bool IsFinite(Vector3 value)
    {
        return !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
               !float.IsNaN(value.y) && !float.IsInfinity(value.y) &&
               !float.IsNaN(value.z) && !float.IsInfinity(value.z);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority, Channel = RpcChannel.Reliable)]
    private void RPC_PresentTargetResult(
        NetworkId targetId,
        Vector3 targetPosition,
        bool hit,
        float appliedDamage,
        bool knockbackApplied,
        int shotSequence)
    {
        TargetResultPresented?.Invoke(new ShotgunTargetResultPresentation(
            targetId,
            targetPosition,
            hit,
            appliedDamage,
            knockbackApplied,
            shotSequence));
    }

    private readonly struct Candidate
    {
        public Candidate(WorldMonster monster, Vector3 point, float distance)
        {
            Monster = monster;
            Point = point;
            Distance = distance;
        }

        public WorldMonster Monster { get; }
        public Vector3 Point { get; }
        public float Distance { get; }
    }
}
