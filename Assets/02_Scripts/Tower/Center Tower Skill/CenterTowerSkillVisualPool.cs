using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class CenterTowerSkillVisualPool
    {
        private readonly Transform _root;
        private readonly GameObject _projectilePrefab;
        private readonly GameObject _explosionPrefab;
        private readonly Queue<GameObject> _projectiles = new();
        private readonly Queue<GameObject> _explosions = new();
        private readonly List<GameObject> _createdObjects = new();

        public CenterTowerSkillVisualPool(
            Transform root,
            GameObject projectilePrefab,
            GameObject explosionPrefab,
            int prewarmCount)
        {
            _root = root;
            _projectilePrefab = projectilePrefab;
            _explosionPrefab = explosionPrefab;

            int count = Mathf.Max(0, prewarmCount);
            for (int i = 0; i < count; i++)
            {
                ReleaseProjectile(Create(_projectilePrefab));
                ReleaseExplosion(Create(_explosionPrefab));
            }
        }

        public GameObject AcquireProjectile()
        {
            return Acquire(_projectiles, _projectilePrefab);
        }

        public GameObject AcquireExplosion()
        {
            return Acquire(_explosions, _explosionPrefab);
        }

        public void ReleaseProjectile(GameObject projectile)
        {
            Release(projectile, _projectiles);
        }

        public void ReleaseExplosion(GameObject explosion)
        {
            Release(explosion, _explosions);
        }

        public void Dispose()
        {
            for (int i = 0; i < _createdObjects.Count; i++)
            {
                GameObject createdObject = _createdObjects[i];
                if (createdObject != null)
                    UnityEngine.Object.Destroy(createdObject);
            }

            _createdObjects.Clear();
            _projectiles.Clear();
            _explosions.Clear();
        }

        private GameObject Acquire(Queue<GameObject> pool, GameObject prefab)
        {
            GameObject instance = pool.Count > 0 ? pool.Dequeue() : Create(prefab);
            if (instance != null)
                instance.SetActive(true);

            return instance;
        }

        private GameObject Create(GameObject prefab)
        {
            if (prefab == null)
                return null;

            GameObject instance = UnityEngine.Object.Instantiate(prefab, _root);
            instance.SetActive(false);
            _createdObjects.Add(instance);
            return instance;
        }

        private static void Release(GameObject instance, Queue<GameObject> pool)
        {
            if (instance == null)
                return;

            instance.SetActive(false);
            pool.Enqueue(instance);
        }
    }
}
