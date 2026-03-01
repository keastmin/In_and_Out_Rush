using UnityEngine;

namespace Dev
{
    public abstract class Spawner
    {
        protected ObjectSampler _objectSampler;

        public Spawner(ObjectSampler objectSampler)
        {
            _objectSampler = objectSampler;
        }

        public virtual bool Spawn(SpawnParam param, out GameObject[] objects)
            => SpawnInternal(param, out objects);

        protected abstract bool SpawnInternal(SpawnParam param, out GameObject[] objects);
    }
}