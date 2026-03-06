using System;
using UnityEngine;

namespace Dev.Local
{
    public class ResourceSystem : System
    {
        [SerializeField] private CircleSpawnParam _circleSpawnParam;
        [SerializeField] private int _initialResourceCount = 10;

        private CircleSpawnPolicy<MonoBehaviour> _circleSpawnPolicy;

        public event Action<ResourceVisible> OnResourceSpawned;

        protected override void OnInitialize()
        {
            _circleSpawnPolicy = new CircleSpawnPolicy<MonoBehaviour>();
        }

        public void SetSpawnValidator(Func<object, bool> spawnValidator)
            => _circleSpawnParam.SpawnValidator = spawnValidator;

        protected override void OnSetUp()
        {
            SpawnResources();
        }

        public void SpawnResources()
        {
            var spawner = new Spawner(new ObjectSampler(), _circleSpawnPolicy);

            for (int i = 0; i < _initialResourceCount; i++)
            {
                var isSpawned = spawner.Spawn(_circleSpawnParam, out var spawnedObject);
                if (!isSpawned)
                    continue;

                if (!spawnedObject.TryGetComponent<ResourceVisible>(out var resource))
                    continue;

                OnResourceSpawned?.Invoke(resource);
            }
        }
    }
}
