using System;
using System.Collections.Generic;
using Dev;
using UnityEngine;

namespace Dev.Network
{
    [CreateAssetMenu(fileName = "ResourcePlacementSettings", menuName = "Scriptable Objects/Resource Placement Settings")]
    public sealed class ResourcePlacementSettings : ScriptableObject
    {
        [SerializeField] private ResourceType _resourceType;
        [SerializeField] private ResourceChunkPlacementSettings _small = new(null, 1, 0f);
        [SerializeField] private ResourceChunkPlacementSettings _medium = new(null, 1, 0f);
        [SerializeField] private ResourceChunkPlacementSettings _large = new(null, 1, 0f);
        [SerializeField, Min(1)] private int _sectorCount = 8;
        [SerializeField, Min(1)] private int _ringCount = 3;
        [SerializeField, Min(0)] private int _zoneBudget;

        public ResourceType ResourceType => _resourceType;
        public ResourceChunkPlacementSettings Small => _small;
        public ResourceChunkPlacementSettings Medium => _medium;
        public ResourceChunkPlacementSettings Large => _large;
        public int SectorCount => Mathf.Max(1, _sectorCount);
        public int RingCount => Mathf.Max(1, _ringCount);
        public int ZoneBudget => Mathf.Max(0, _zoneBudget);

        public IReadOnlyList<ResourceChunkPlacementSettings> GetAvailableChunks()
        {
            var chunks = new List<ResourceChunkPlacementSettings>(3);
            AddAvailableChunk(chunks, _small);
            AddAvailableChunk(chunks, _medium);
            AddAvailableChunk(chunks, _large);
            return chunks;
        }

        private static void AddAvailableChunk(
            List<ResourceChunkPlacementSettings> chunks,
            ResourceChunkPlacementSettings chunk)
        {
            if (chunk == null || !chunk.IsAvailable)
                return;

            chunks.Add(chunk);
        }
    }

    [Serializable]
    public sealed class ResourceChunkPlacementSettings
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField, Min(1)] private int _amount;
        [SerializeField, Min(0f)] private float _minDistance;

        public ResourceChunkPlacementSettings(GameObject prefab, int amount, float minDistance)
        {
            _prefab = prefab;
            _amount = amount;
            _minDistance = minDistance;
        }

        public GameObject Prefab => _prefab;
        public int Amount => Mathf.Max(1, _amount);
        public float MinDistance => Mathf.Max(0f, _minDistance);
        public bool IsAvailable => _prefab != null;
    }
}
