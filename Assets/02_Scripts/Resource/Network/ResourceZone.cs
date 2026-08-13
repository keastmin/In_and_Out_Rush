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
        private const float DefaultLocalVisibilityRadius = 128f;

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
        [SerializeField, Min(0f)] private float _localVisibilityRadius = DefaultLocalVisibilityRadius;

        private readonly List<Dev.Local.ResourceVisible> _localResources = new();
        private readonly List<ResourceZoneEntry> _builtEntries = new();
        private int _builtSeed;
        private int _builtEntryCount = -1;
        private Dev.ResourceType _builtResourceType;
        private bool _hasBuiltState;
        private bool _missingVisualPrefabLogged;
        private bool _invalidVisualPrefabLogged;
        private bool _networkSpawned;
        private PlayerRunner _runnerReference;

        public Dev.ResourceType ResourceType => (Dev.ResourceType)Mathf.Clamp(
            NetworkResourceType,
            (int)Dev.ResourceType.Mineral,
            (int)Dev.ResourceType.Gas);

        public int EntryCount => Mathf.Clamp(ResourceCount, 0, MaxEntries);

        public bool HasUncollectedResourceWithin(Vector3 worldPosition, float radius)
        {
            float radiusSqr = Mathf.Max(0f, radius) * Mathf.Max(0f, radius);

            for (int i = 0; i < EntryCount; i++)
            {
                if (IsCollected(i))
                    continue;

                Vector3 offset = Entries[i].WorldPosition - worldPosition;
                offset.y = 0f;
                if (offset.sqrMagnitude <= radiusSqr)
                    return true;
            }

            return false;
        }

        public void InitializeState(
            Dev.ResourceType resourceType,
            int seed,
            IReadOnlyList<ResourceZoneEntry> entries)
        {
            if (!HasStateAuthority || entries == null)
                return;

            int resourceCount = Mathf.Min(entries.Count, MaxEntries);

            NetworkArray<ResourceZoneEntry> networkEntries = Entries;
            for (int i = 0; i < MaxEntries; i++)
                networkEntries[i] = default;

            for (int i = 0; i < resourceCount; i++)
                networkEntries[i] = entries[i];

            NetworkArray<uint> collectedBits = CollectedBits;
            for (int i = 0; i < CollectionWordCount; i++)
                collectedBits[i] = 0u;

            // ResourceCount is used by proxies as the data-ready boundary. Set it
            // only after the entries and collection state have been written.
            Seed = seed;
            NetworkResourceType = (int)resourceType;
            ResourceCount = resourceCount;
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
            _networkSpawned = true;
            EnsureLocalResources();
        }

        public override void Render()
        {
            EnsureLocalResources();
            UpdateLocalResourceVisibility();
        }

        private void LateUpdate()
        {
            // Fusion's Render/SimulationEnter callbacks are AOI-driven. Local
            // visuals must also retry when the network snapshot and the local
            // PlayerRunner become ready on different frames.
            if (!CanReadNetworkState())
                return;

            EnsureLocalResources();
            UpdateLocalResourceVisibility();
        }

        public void SimulationEnter()
        {
            EnsureLocalResources();
            UpdateLocalResourceVisibility();
        }

        public void SimulationExit()
        {
            SetLocalResourcesActive(false);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _networkSpawned = false;
            ClearLocalResources();
            base.Despawned(runner, hasState);
        }

        private bool CanReadNetworkState()
        {
            return _networkSpawned && Object != null && Object.IsValid;
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
            if (!CanReadNetworkState())
                return;

            int entryCount = EntryCount;
            // ResourceCount and Entries can arrive in different network updates.
            // Do not cache a zero/default entry state as a completed build.
            if (!AreEntriesReady(entryCount))
                return;

            if (IsBuiltStateCurrent(entryCount))
                return;

            ClearLocalResources();
            _builtSeed = Seed;
            _builtEntryCount = entryCount;
            _builtResourceType = ResourceType;
            _builtEntries.Clear();

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
                {
                    _localResources.Add(null);
                    _builtEntries.Add(Entries[i]);
                }

                _hasBuiltState = true;
                return;
            }

            for (int i = 0; i < entryCount; i++)
            {
                ResourceZoneEntry entry = Entries[i];
                _builtEntries.Add(entry);
                _localResources.Add(null);
            }

            _hasBuiltState = true;
            UpdateLocalResourceVisibility();
        }

        private bool AreEntriesReady(int entryCount)
        {
            if (entryCount <= 0)
                return false;

            for (int i = 0; i < entryCount; i++)
            {
                if (Entries[i].Amount <= 0)
                    return false;
            }

            return true;
        }

        private bool IsBuiltStateCurrent(int entryCount)
        {
            if (!_hasBuiltState ||
                _builtSeed != Seed ||
                _builtEntryCount != entryCount ||
                _builtResourceType != ResourceType ||
                _builtEntries.Count != entryCount)
            {
                return false;
            }

            for (int i = 0; i < entryCount; i++)
            {
                ResourceZoneEntry currentEntry = Entries[i];
                ResourceZoneEntry builtEntry = _builtEntries[i];
                if (currentEntry.Amount != builtEntry.Amount ||
                    currentEntry.Position != builtEntry.Position)
                {
                    return false;
                }
            }

            return true;
        }

        private void UpdateLocalResourceVisibility()
        {
            if (!_hasBuiltState || !TryGetRunnerPosition(out Vector3 runnerPosition))
                return;

            GameObject prefab = GetLocalPrefab();
            if (prefab == null)
                return;

            // Existing Resource Zone prefab assets were created before this field
            // existed, so Unity can deserialize the missing value as zero. A zero
            // radius would make every local resource invisible unless the runner
            // stands on the exact resource position.
            float visibilityRadius = _localVisibilityRadius > 0f
                ? _localVisibilityRadius
                : DefaultLocalVisibilityRadius;
            float visibilityRadiusSqr = visibilityRadius * visibilityRadius;
            int count = Mathf.Min(EntryCount, _localResources.Count);

            for (int i = 0; i < count; i++)
            {
                ResourceZoneEntry entry = Entries[i];
                Vector3 offset = entry.WorldPosition - runnerPosition;
                offset.y = 0f;
                bool shouldRender = !IsCollected(i) && offset.sqrMagnitude <= visibilityRadiusSqr;
                Dev.Local.ResourceVisible resource = _localResources[i];

                if (!shouldRender)
                {
                    if (resource != null)
                        resource.gameObject.SetActive(false);

                    continue;
                }

                if (resource == null)
                {
                    GameObject instance = Instantiate(prefab, entry.WorldPosition, Quaternion.identity);
                    instance.name = $"{name}_Resource_{i:00}";
                    instance.transform.localScale = Vector3.one * GetVisualScale(entry.Amount);

                    if (!instance.TryGetComponent(out resource))
                    {
                        if (!_invalidVisualPrefabLogged)
                        {
                            Debug.LogWarning(
                                $"Local resource prefab {prefab.name} does not contain {nameof(Dev.Local.ResourceVisible)}.",
                                prefab);
                            _invalidVisualPrefabLogged = true;
                        }

                        Destroy(instance);
                        continue;
                    }

                    resource.Type = ResourceType;
                    resource.Amount = entry.Amount;
                    _localResources[i] = resource;
                }

                resource.gameObject.SetActive(true);
            }
        }

        private bool TryGetRunnerPosition(out Vector3 runnerPosition)
        {
            runnerPosition = default;

            // Prefer this client/server's own player object. StageBootstrapper's
            // shared PlayerRunner can refer to another player's proxy (or to a
            // non-input-authority runner), which would make local culling use a
            // stale position while the camera is moving elsewhere.
            if (Runner != null &&
                Runner.LocalPlayer != PlayerRef.None &&
                Runner.TryGetPlayerObject(Runner.LocalPlayer, out NetworkObject localPlayerObject) &&
                localPlayerObject != null)
            {
                PlayerRunner localRunner = localPlayerObject.GetComponent<PlayerRunner>();
                if (localRunner != null)
                {
                    _runnerReference = localRunner;
                }
                else
                {
                    runnerPosition = localPlayerObject.transform.position;
                    return true;
                }
            }

            StageBootstrapper stageBootstrapper = StageBootstrapper.Instance;
            if (_runnerReference == null &&
                stageBootstrapper != null &&
                stageBootstrapper.PlayerRunner != null)
                _runnerReference = stageBootstrapper.PlayerRunner;

            if (_runnerReference == null)
                _runnerReference = UnityEngine.Object.FindFirstObjectByType<PlayerRunner>();

            if (_runnerReference == null)
                return false;

            runnerPosition = _runnerReference.transform.position;
            return true;
        }

        private void SetLocalResourcesActive(bool isActive)
        {
            for (int i = 0; i < _localResources.Count; i++)
            {
                Dev.Local.ResourceVisible resource = _localResources[i];
                if (resource != null)
                    resource.gameObject.SetActive(isActive);
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
            _builtEntries.Clear();
            _hasBuiltState = false;
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
