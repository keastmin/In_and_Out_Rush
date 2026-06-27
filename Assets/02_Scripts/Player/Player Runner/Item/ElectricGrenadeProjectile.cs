using System.Collections.Generic;
using Fusion;
using KIM.Dev;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public sealed class ElectricGrenadeProjectile : NetworkBehaviour
{
    private const float DefaultCellSize = 1.6f;

    [Header("Movement")]
    [SerializeField, Min(0.01f)] private float horizontalSpeedInTiles = 5f;
    [SerializeField, Min(0f)] private float arcHeightInTiles = 1.5f;

    [Header("Effect")]
    [SerializeField, Min(0f)] private float effectDiameterInTiles = 2f;
    [SerializeField, Min(0f)] private float stunDuration = 2f;
    [SerializeField] private LayerMask monsterLayerMask = ~0;

    private readonly HashSet<Monster> _affectedMonsters = new();

    private Vector3 _startPosition;
    private Vector3 _targetPosition;
    private float _cellSize;
    private float _duration;
    private float _elapsedTime;
    private bool _initialized;

    public void Initialize(Vector3 startPosition, Vector3 targetPosition)
    {
        if (!HasStateAuthority)
            return;

        _cellSize = ResolveCellSize();
        _startPosition = startPosition;
        _targetPosition = targetPosition;
        _duration = CalculateTravelDuration(startPosition, targetPosition, _cellSize);
        _elapsedTime = 0f;
        _initialized = true;

        transform.position = startPosition;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !_initialized)
            return;

        _elapsedTime += Runner.DeltaTime;
        float progress = Mathf.Clamp01(_elapsedTime / _duration);
        transform.position = CalculateArcPosition(progress);

        if (progress < 1f)
            return;

        ApplyStun();
        Runner.Despawn(Object);
    }

    private Vector3 CalculateArcPosition(float progress)
    {
        Vector3 position = Vector3.Lerp(_startPosition, _targetPosition, progress);
        float arcHeight = arcHeightInTiles * _cellSize;
        position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;
        return position;
    }

    private void ApplyStun()
    {
        _affectedMonsters.Clear();

        float radius = effectDiameterInTiles * _cellSize * 0.5f;
        Collider[] hits = Physics.OverlapSphere(
            _targetPosition,
            radius,
            monsterLayerMask,
            QueryTriggerInteraction.Collide);

        for (int i = 0; i < hits.Length; i++)
        {
            Monster monster = hits[i].GetComponentInParent<Monster>();
            if (monster == null || !_affectedMonsters.Add(monster))
                continue;

            monster.ApplyStun(stunDuration);
        }
    }

    private float CalculateTravelDuration(Vector3 startPosition, Vector3 targetPosition, float cellSize)
    {
        Vector3 planarOffset = targetPosition - startPosition;
        planarOffset.y = 0f;

        float horizontalSpeed = Mathf.Max(0.01f, horizontalSpeedInTiles * cellSize);
        return Mathf.Max(0.01f, planarOffset.magnitude / horizontalSpeed);
    }

    private static float ResolveCellSize()
    {
        return InfiniteGrid.Instance != null
            ? InfiniteGrid.Instance.CellSize
            : DefaultCellSize;
    }

    private void OnValidate()
    {
        effectDiameterInTiles = Mathf.Max(0f, effectDiameterInTiles);
        stunDuration = Mathf.Max(0f, stunDuration);
    }
}
