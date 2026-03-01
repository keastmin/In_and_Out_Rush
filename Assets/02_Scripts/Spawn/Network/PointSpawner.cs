using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public class PointSpawnParam : SpawnParam
    {
        public Vector3 SpawnPosition;
        public Quaternion SpawnRotation;
    }

    public class PointSpawner : Spawner
    {
        public PointSpawner(NetworkBehaviour networkBehaviour, ObjectSampler objectSampler) : base(networkBehaviour, objectSampler) {}

        protected override bool SpawnInternal(SpawnParam param, out GameObject[] objects)
        {
            if (param is not PointSpawnParam pointParam)
            {
                objects = null;
                return false;
            }

            objects = new GameObject[param.Count];
            for (int i = 0; i < objects.Length; i++)
            {
                var @object = _objectSampler.Sample(param.ObjectSampleParam);
                var position = pointParam.SpawnPosition;
                var rotation = pointParam.SpawnRotation;
                var spawnedObject = _networkBehaviour.Runner.Spawn(@object, position, rotation);
                objects[i] = spawnedObject.gameObject;
            }
            return true;
        }
    }
}