using Fusion;

namespace Dev.Network
{
    public abstract class Spawner : Dev.Spawner
    {
        protected NetworkBehaviour _networkBehaviour;

        public Spawner(NetworkBehaviour networkBehaviour, ObjectSampler objectSampler) : base(objectSampler)
        {
            _networkBehaviour = networkBehaviour;
        }
    }
}