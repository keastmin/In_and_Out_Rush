using UnityEngine;

namespace Dev.Local
{
    public class Spawner : Spawner<MonoBehaviour>
    {
        public Spawner(ObjectSampler objectSampler, SpawnPolicy<MonoBehaviour> spawnPolicy) : base(objectSampler, spawnPolicy) { }

        protected override MonoBehaviour SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            return Object.Instantiate(prefab, position, rotation).GetComponent<MonoBehaviour>();
        }
    }
}