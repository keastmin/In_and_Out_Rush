using System;
using UnityEngine;

namespace Dev
{
    [Serializable]
    public class CircleSpawnParam : SpawnParam
    {
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
        public float SpawnRadius;
    }

    public class CircleSpawnPolicy<T> : SpawnPolicy<T> where T : MonoBehaviour
    {
        public override T Spawn(GameObject prefab, SpawnParam param, Func<GameObject, Vector3, Quaternion, T> spawnFunc)
        {
            if (param is not CircleSpawnParam circleParam)
                return default;

            for (int i = 0; i < param.MaxRetryCount; i++)
            {
                var randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var randomRadius = UnityEngine.Random.Range(0f, circleParam.SpawnRadius);
                var randomPosition = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
                var rotatedPosition = circleParam.SpawnRotation * new Vector3(randomPosition.x, 0, randomPosition.y);
                var position = circleParam.SpawnPosition + rotatedPosition;
                var rotation = circleParam.SpawnRotation;
                if (param.SpawnValidator != null && !param.SpawnValidator((prefab, position, rotation)))
                    continue;

                var spawnedObject = spawnFunc(prefab, position, rotation);
                return spawnedObject;
            }

            return null;
        }
    }
}