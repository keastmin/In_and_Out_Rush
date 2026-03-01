using UnityEngine;

namespace Dev
{
    public abstract class Spawner<T> where T : MonoBehaviour
    {
        protected ObjectSampler _objectSampler;
        protected SpawnPolicy<T> _spawnPolicy;

        public Spawner(ObjectSampler objectSampler, SpawnPolicy<T> spawnPolicy)
        {
            _objectSampler = objectSampler;
            _spawnPolicy = spawnPolicy;
        }

        public bool Spawn(SpawnParam param, out T @object)
        {
            @object = default;

            var spawnedPrefab = _objectSampler.Sample(param.ObjectSampleParam);
            if (spawnedPrefab == null)
                return false;

            var spawnedObject = _spawnPolicy.Spawn(spawnedPrefab, param, SpawnInternal);
            if (spawnedObject == null)
                return false;

            @object = spawnedObject;
            return true;
        }

        protected abstract T SpawnInternal(GameObject prefab, Vector3 position, Quaternion rotation);
    }
}