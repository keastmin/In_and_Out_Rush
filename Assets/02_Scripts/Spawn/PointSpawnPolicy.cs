using UnityEngine;

namespace Dev
{
    public class PointSpawnParam : SpawnParam
    {
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
    }

    public class PointSpawnPolicy<T> : SpawnPolicy<T> where T : MonoBehaviour
    {
        public override T Spawn(GameObject prefab, SpawnParam param, System.Func<GameObject, Vector3, Quaternion, T> spawnFunc)
        {
            if (param is not PointSpawnParam pointParam)
                return default;

            var spawnedObject = spawnFunc(prefab, pointParam.SpawnPosition, pointParam.SpawnRotation);
            if (spawnedObject == null)
                return default;

            return spawnedObject;
        }
    }
}