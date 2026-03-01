using System;
using Fusion;
using UnityEngine;

namespace Dev.Network
{
    [Serializable]
    public class CircleSpawnParam : SpawnParam
    {
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
        public float SpawnRadius;
    }

    public class CircleSpawner : Spawner
    {
        public CircleSpawner(NetworkBehaviour networkBehaviour, ObjectSampler objectSampler) : base(networkBehaviour, objectSampler) {}

        protected override bool SpawnInternal(SpawnParam param, out GameObject[] objects)
        {
            if (param is not CircleSpawnParam circleParam)
            {
                objects = null;
                return false;
            }

            objects = new GameObject[param.Count];
            var retryCount = 0;
            for (int i = 0; i < param.Count; i++)
            {
                var @object = _objectSampler.Sample(param.ObjectSampleParam);
                var randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var randomRadius = UnityEngine.Random.Range(0f, circleParam.SpawnRadius);
                var randomPosition = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)) * randomRadius;
                var rotatedPosition = circleParam.SpawnRotation * new Vector3(randomPosition.x, 0, randomPosition.y);
                var position = circleParam.SpawnPosition + rotatedPosition;
                var rotation = circleParam.SpawnRotation;
                if (param.SpawnValidator != null && !param.SpawnValidator((@object, position, rotation)))
                {
                    retryCount++;
                    if (retryCount > param.MaxRetryCount)
                    {
                        if (!param.AllowSpawnFailure)
                        {
                            objects = null;
                            return false;
                        }
                        objects[i] = null;
                        i++;
                        retryCount = 0;
                    }
                    i--;
                    continue;
                }
                var spawnedObject = _networkBehaviour.Runner.Spawn(@object, position, rotation, PlayerRef.None);
                objects[i] = spawnedObject.gameObject;
            }
            return true;
        }
    }
}