using System;
using UnityEngine;

namespace Dev.Network
{
    public class ResourceSpawnSystem : System
    {
        [SerializeField] private ResourceSystem _resourceSystem;
        [SerializeField] private TerritorySystem _territorySystem;
        [SerializeField] private CircleSpawnParam _circleSpawnParam;
        [SerializeField] private Local.ResourceView _resourceView;

        private CircleSpawner _circleSpawner;

        protected override void OnSetUp()
        {
            if (Object.HasStateAuthority)
            {
                _circleSpawnParam.SpawnValidator = args =>
                {
                    var (_, position, _) = (ValueTuple<GameObject, Vector3, Quaternion>)args;
                    var isInPolygon = _territorySystem.Territory.IsPointInPolygon(new Vector2(position.x, position.z));
                    return !isInPolygon;
                };

                _circleSpawner = new CircleSpawner(this, new ObjectSampler());

                GenerateResources();
            }
        }

        public void GenerateResources()
        {
            var isSpawned = _circleSpawner.Spawn(_circleSpawnParam, out var spawnedObjects);
            if (!isSpawned)
            {
                Debug.LogError("Failed to spawn resources.");
                return;
            }

            foreach (var obj in spawnedObjects)
            {
                if (obj == null) continue;
                var resource = obj.GetComponent<ResourceVisible>();

                void HandleTerritoryExpanded(Territory territory, TerritorySystem territorySystem)
                {
                    var xzPosition = new Vector2(resource.transform.position.x, resource.transform.position.z);
                    if (territory.IsPointInPolygon(xzPosition))
                    {
                        _territorySystem.OnTerritoryExpandedEvent -= HandleTerritoryExpanded;
                        resource.Collect();
                    }
                }
                _territorySystem.OnTerritoryExpandedEvent += HandleTerritoryExpanded;
                resource.OnCollected += HandleResourceCollected;
            }
        }

        private void HandleResourceCollected(ResourceType type, int amount, ResourceVisible resource, object context)
        {
            switch (type)
            {
                case ResourceType.Mineral:
                    _resourceSystem.RPC_GetMineral(amount);
                    _resourceView.SetMineral(_resourceSystem.Mineral);
                    break;
                case ResourceType.Gas:
                    _resourceSystem.RPC_GetGas(amount);
                    _resourceView.SetGas(_resourceSystem.Gas);
                    break;
            }
            // Debug.Log($"Obtained {amount} {type} from {resource.gameObject.name}");
            Runner.Despawn(resource.Object);
        }
    }
}