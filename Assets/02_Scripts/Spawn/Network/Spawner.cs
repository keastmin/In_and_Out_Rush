using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public class Spawner : Spawner<NetworkObject>
    {
        protected NetworkBehaviour _networkBehaviour;

        public Spawner(NetworkBehaviour networkBehaviour, ObjectSampler objectSampler, SpawnPolicy<NetworkObject> spawnPolicy) : base(objectSampler, spawnPolicy)
        {
            _networkBehaviour = networkBehaviour;
        }

        protected override NetworkObject SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return _networkBehaviour.Runner.Spawn(prefab, position, rotation);
        }
    }
}