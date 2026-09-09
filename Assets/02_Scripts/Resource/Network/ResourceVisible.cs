using System;
using Fusion;
using KIM.Dev;

namespace Dev.Network
{
    public class ResourceVisible : Visible, IResourceVisible
    {
        public ResourceType Type;
        public int Amount = 5;

        private bool _isCollected;

        public event Action<ResourceType, int, ResourceVisible, object> OnCollected;

        public override void Spawned()
        {
            base.Spawned();
            FogOfWarHiddenObjectController.Register(transform);
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            FogOfWarHiddenObjectController.Unregister(transform);
            base.Despawned(runner, hasState);
        }

        public void Collect()
        {
            if (_isCollected)
                return;

            _isCollected = true;
            OnCollected?.Invoke(Type, Amount, this, this);
        }
    }
}
