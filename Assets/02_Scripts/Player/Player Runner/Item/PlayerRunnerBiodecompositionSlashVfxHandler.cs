using System.Collections.Generic;
using UnityEngine;

public sealed class PlayerRunnerBiodecompositionSlashVfxHandler
{
    private const float HeightOffset = 0.05f;

    private readonly List<Vector3> _samplePoints = new();
    private readonly List<SlashVfxPoint> _pool = new();
    private GameObject _root;
    private GameObject _currentPrefab;
    private int _activeCount;

    public void Render(
        bool isActive,
        GameObject effectPrefab,
        SlashContactDetector slashContactDetector,
        TerritorySystem territorySystem,
        float sampleSpacing,
        int maxSampleCount)
    {
        if (!isActive || effectPrefab == null || slashContactDetector == null)
        {
            StopAll();
            return;
        }

        if (_currentPrefab != effectPrefab)
            ResetPool(effectPrefab);

        if (!slashContactDetector.TrySampleSlashPath(territorySystem, _samplePoints, sampleSpacing, maxSampleCount))
        {
            StopAll();
            return;
        }

        EnsureRoot();
        int activeCount = Mathf.Min(_samplePoints.Count, Mathf.Max(0, maxSampleCount));
        for (int i = 0; i < activeCount; i++)
        {
            SlashVfxPoint point = GetOrCreatePoint(effectPrefab, i);
            point.SetPosition(_samplePoints[i] + Vector3.up * HeightOffset);
            point.Play();
        }

        StopRange(activeCount, _activeCount);
        _activeCount = activeCount;
    }

    public void StopAll()
    {
        StopRange(0, _activeCount);
        _activeCount = 0;
    }

    public void Dispose()
    {
        StopAll();
        if (_root != null)
            Object.Destroy(_root);

        _root = null;
        _currentPrefab = null;
        _pool.Clear();
        _samplePoints.Clear();
    }

    private void ResetPool(GameObject effectPrefab)
    {
        Dispose();
        _currentPrefab = effectPrefab;
    }

    private void EnsureRoot()
    {
        if (_root != null)
            return;

        _root = new GameObject("Biodecomposition Slash VFX");
    }

    private SlashVfxPoint GetOrCreatePoint(GameObject effectPrefab, int index)
    {
        while (_pool.Count <= index)
            _pool.Add(SlashVfxPoint.Create(effectPrefab, _root.transform, _pool.Count));

        return _pool[index];
    }

    private void StopRange(int startInclusive, int endExclusive)
    {
        int end = Mathf.Min(endExclusive, _pool.Count);
        for (int i = Mathf.Max(0, startInclusive); i < end; i++)
            _pool[i].Stop();
    }

    private sealed class SlashVfxPoint
    {
        private readonly GameObject _instance;
        private readonly Transform _transform;
        private readonly ParticleSystem[] _particles;

        private SlashVfxPoint(GameObject instance)
        {
            _instance = instance;
            _transform = instance.transform;
            _particles = instance.GetComponentsInChildren<ParticleSystem>(true);
        }

        public static SlashVfxPoint Create(GameObject prefab, Transform parent, int index)
        {
            GameObject instance = Object.Instantiate(prefab, parent);
            instance.name = $"{prefab.name} {index:00}";
            instance.SetActive(false);

            return new SlashVfxPoint(instance);
        }

        public void SetPosition(Vector3 position)
        {
            _transform.position = position;
        }

        public void Play()
        {
            if (!_instance.activeSelf)
                _instance.SetActive(true);

            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem particle = _particles[i];
                if (particle != null && !particle.isPlaying)
                    particle.Play(true);
            }
        }

        public void Stop()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                ParticleSystem particle = _particles[i];
                if (particle != null && particle.isPlaying)
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            if (_instance.activeSelf)
                _instance.SetActive(false);
        }
    }
}
