using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public struct ResourceZoneEntry : INetworkStruct
    {
        public Vector2 Position;
        public int Amount;

        public ResourceZoneEntry(Vector3 position, int amount)
        {
            Position = new Vector2(position.x, position.z);
            Amount = amount;
        }

        public Vector3 WorldPosition => new(Position.x, 0f, Position.y);
    }

    public sealed class ResourceZone : NetworkBehaviour, ISimulationEnter, ISimulationExit
    {
        public const int MaxEntries = 96;

        private const int CollectionWordCount = (MaxEntries + 31) / 32;

        [Networked]
        public int Seed { get; private set; }

        [Networked]
        private int NetworkResourceType { get; set; }

        [Networked]
        private int ResourceCount { get; set; }

        [Networked, Capacity(MaxEntries)]
        private NetworkArray<ResourceZoneEntry> Entries => default;

        [Networked, Capacity(CollectionWordCount)]
        private NetworkArray<uint> CollectedBits => default;

        [Header("Local Resource Views")]
        [SerializeField] private GameObject _mineralPrefab;
        [SerializeField] private GameObject _gasPrefab;

        private readonly List<Dev.Local.ResourceVisible> _localResources = new();
        private int _builtSeed;
        private int _builtEntryCount = -1;
        private bool _missingVisualPrefabLogged;

        public Dev.ResourceType ResourceType => (Dev.ResourceType)Mathf.Clamp(
            NetworkResourceType,
            (int)Dev.ResourceType.Mineral,
            (int)Dev.ResourceType.Gas);

        public int EntryCount => Mathf.Clamp(ResourceCount, 0, MaxEntries);

        public void InitializeState(
            Dev.ResourceType resourceType,
            int seed,
            IReadOnlyList<ResourceZoneEntry> entries)
        {
            if (!HasStateAuthority || entries == null)
                return;

            Seed = seed;
            NetworkResourceType = (int)resourceType;
            ResourceCount = Mathf.Min(entries.Count, MaxEntries);

            NetworkArray<ResourceZoneEntry> networkEntries = Entries;
            for (int i = 0; i < MaxEntries; i++)
                networkEntries[i] = default;

            for (int i = 0; i < ResourceCount; i++)
                networkEntries[i] = entries[i];

            NetworkArray<uint> collectedBits = CollectedBits;
            for (int i = 0; i < CollectionWordCount; i++)
                collectedBits[i] = 0u;
        }

        public int CollectWithin(Territory territory)
        {
            if (!HasStateAuthority || territory == null)
                return 0;

            int collectedAmount = 0;
            for (int i = 0; i < EntryCount; i++)
            {
                if (IsCollected(i))
                    continue;

                ResourceZoneEntry entry = Entries[i];
                if (!territory.IsPointInPolygon(entry.Position))
                    continue;

                SetCollected(i);
                collectedAmount += Mathf.Max(0, entry.Amount);
            }

            return collectedAmount;
        }

        public override void Spawned()
        {
            base.Spawned();
            EnsureLocalResources();
        }

        public override void Render()
        {
            if (!IsInSimulation())
                return;

            EnsureLocalResources();
            ApplyCollectionVisibility();
        }

        public void SimulationEnter()
        {
            EnsureLocalResources();
            ApplyCollectionVisibility();
        }

        public void SimulationExit()
        {
            ClearLocalResources();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            ClearLocalResources();
            base.Despawned(runner, hasState);
        }

        private bool IsInSimulation()
        {
            return Object != null && Object.IsValid && Object.IsInSimulation;
        }

        private bool IsCollected(int index)
        {
            int wordIndex = index / 32;
            int bitIndex = index % 32;
            return (CollectedBits[wordIndex] & (1u << bitIndex)) != 0u;
        }

        private void SetCollected(int index)
        {
            int wordIndex = index / 32;
            int bitIndex = index % 32;
            NetworkArray<uint> collectedBits = CollectedBits;
            collectedBits[wordIndex] = collectedBits[wordIndex] | 1u << bitIndex;
        }

        private void EnsureLocalResources()
        {
            if (!IsInSimulation())
                return;

            int entryCount = EntryCount;
            if (_builtSeed == Seed && _builtEntryCount == entryCount)
                return;

            ClearLocalResources();
            _builtSeed = Seed;
            _builtEntryCount = entryCount;

            GameObject prefab = GetLocalPrefab();
            if (prefab == null)
            {
                if (!_missingVisualPrefabLogged)
                {
                    Debug.LogWarning(
                        $"{nameof(ResourceZone)} has no local prefab assigned for {ResourceType}.",
                        this);
                    _missingVisualPrefabLogged = true;
                }

                for (int i = 0; i < entryCount; i++)
                    _localResources.Add(null);

                return;
            }

            for (int i = 0; i < entryCount; i++)
            {
                ResourceZoneEntry entry = Entries[i];
                GameObject instance = Instantiate(prefab, entry.WorldPosition, Quaternion.identity);
                instance.name = $"{name}_Resource_{i:00}";
                instance.transform.localScale = Vector3.one * GetVisualScale(entry.Amount);

                if (!instance.TryGetComponent(out Dev.Local.ResourceVisible resource))
                {
                    Debug.LogWarning(
                        $"Local resource prefab {prefab.name} does not contain {nameof(Dev.Local.ResourceVisible)}.",
                        prefab);
                    Destroy(instance);
                    _localResources.Add(null);
                    continue;
                }

                resource.Type = ResourceType;
                resource.Amount = entry.Amount;
                _localResources.Add(resource);
            }

            ApplyCollectionVisibility();
        }

        private void ApplyCollectionVisibility()
        {
            int count = Mathf.Min(EntryCount, _localResources.Count);
            for (int i = 0; i < count; i++)
            {
                Dev.Local.ResourceVisible resource = _localResources[i];
                if (resource != null)
                    resource.gameObject.SetActive(!IsCollected(i));
            }
        }

        private void ClearLocalResources()
        {
            for (int i = 0; i < _localResources.Count; i++)
            {
                Dev.Local.ResourceVisible resource = _localResources[i];
                if (resource != null)
                    Destroy(resource.gameObject);
            }

            _localResources.Clear();
            _builtEntryCount = -1;
        }

        private GameObject GetLocalPrefab()
        {
            return ResourceType == Dev.ResourceType.Mineral ? _mineralPrefab : _gasPrefab;
        }

        private static float GetVisualScale(int amount)
        {
            if (amount >= 90)
                return 2f;

            if (amount >= 60)
                return 1.5f;

            return 1f;
        }
    }
}
