using UnityEngine;

namespace Dev.Local
{
    public abstract class Entity : MonoBehaviour, ILifecycle
    {
        // States
        protected LifecycleState lifecycleState = LifecycleState.Uninitialized;

        // Properties
        public LifecycleState LifecycleState => lifecycleState;

        // Methods
        protected virtual void Awake() => Initialize();
        protected virtual void OnDestroy() => Dispose();

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