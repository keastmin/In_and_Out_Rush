using Fusion;

namespace Dev.Network
{
    public abstract class Entity : NetworkBehaviour, ILifecycle
    {
        // States
        protected LifecycleState lifecycleState = LifecycleState.Uninitialized;

        // Properties
        public LifecycleState LifecycleState => lifecycleState;

        // Methods
        public override void Spawned() => Initialize();
        public override void Despawned(NetworkRunner runner, bool hasState) => Dispose();

        public void Initialize()
        {
            if (lifecycleState != LifecycleState.Uninitialized) return;
            OnInitialize();
            lifecycleState = LifecycleState.Initialized;
        }
        public void Dispose()
        {
            if (lifecycleState != LifecycleState.Initialized) return;
            OnDispose();
            lifecycleState = LifecycleState.Disposed;
        }

        protected virtual void OnInitialize() { }
        protected virtual void OnDispose() { }
    }
}