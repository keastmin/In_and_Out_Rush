using System.Collections.Generic;
using UnityEngine;

public sealed class SlashContactDetector
{
    private const int CollisionBufferSize = 128;

    private readonly Collider[] _targetBuffer = new Collider[CollisionBufferSize];
    private readonly List<Vector3> _slashPath = new();
    private readonly HashSet<WorldMonster> _worldMonsters = new();

    public IReadOnlyCollection<WorldMonster> DetectWorldMonsters(
        TerritorySystem territorySystem,
        float fallbackRadius,
        LayerMask monsterLayerMask)
    {
        _worldMonsters.Clear();

        if (territorySystem == null || !territorySystem.TryGetCurrentExpansionPath(_slashPath))
            return _worldMonsters;

        float radius = ResolveSlashRadius(territorySystem, fallbackRadius);
        for (int i = 0; i < _slashPath.Count - 1; i++)
        {
            Vector3 start = _slashPath[i];
            Vector3 end = _slashPath[i + 1];
            int hitCount = Physics.OverlapCapsuleNonAlloc(
                start,
                end,
                radius,
                _targetBuffer,
                monsterLayerMask,
                QueryTriggerInteraction.Collide);

            for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
            {
                WorldMonster monster = _targetBuffer[hitIndex] != null
                    ? _targetBuffer[hitIndex].GetComponentInParent<WorldMonster>()
                    : null;

                if (monster != null && monster.CanAccessNetworkState)
                    _worldMonsters.Add(monster);
            }
        }

        return _worldMonsters;
    }

    public bool TrySampleSlashPath(
        TerritorySystem territorySystem,
        List<Vector3> results,
        float sampleSpacing,
        int maxSampleCount)
    {
        if (results == null)
            return false;

        results.Clear();
        if (territorySystem == null || !territorySystem.TryGetCurrentExpansionPath(_slashPath))
            return false;

        int safeMaxSampleCount = Mathf.Max(1, maxSampleCount);
        float safeSampleSpacing = Mathf.Max(0.01f, sampleSpacing);
        results.Add(_slashPath[0]);

        float distanceSinceLastSample = 0f;
        for (int i = 0; i < _slashPath.Count - 1 && results.Count < safeMaxSampleCount; i++)
        {
            Vector3 start = _slashPath[i];
            Vector3 end = _slashPath[i + 1];
            float segmentLength = Vector3.Distance(start, end);
            if (segmentLength <= Mathf.Epsilon)
                continue;

            Vector3 direction = (end - start) / segmentLength;
            float distanceOnSegment = safeSampleSpacing - distanceSinceLastSample;
            while (distanceOnSegment <= segmentLength && results.Count < safeMaxSampleCount)
            {
                results.Add(start + direction * distanceOnSegment);
                distanceOnSegment += safeSampleSpacing;
            }

            distanceSinceLastSample = segmentLength - (distanceOnSegment - safeSampleSpacing);
            if (distanceSinceLastSample >= safeSampleSpacing)
                distanceSinceLastSample = 0f;
        }

        Vector3 lastPoint = _slashPath[^1];
        if (results.Count < safeMaxSampleCount && Vector3.Distance(results[^1], lastPoint) > safeSampleSpacing * 0.5f)
            results.Add(lastPoint);

        return results.Count > 0;
    }

    private static float ResolveSlashRadius(TerritorySystem territorySystem, float fallbackRadius)
    {
        float safeFallbackRadius = Mathf.Max(0.01f, fallbackRadius);
        if (territorySystem != null && territorySystem.ExpansionLineWidth > 0f)
            return Mathf.Max(territorySystem.ExpansionLineWidth * 0.5f, safeFallbackRadius);

        return safeFallbackRadius;
    }
}
