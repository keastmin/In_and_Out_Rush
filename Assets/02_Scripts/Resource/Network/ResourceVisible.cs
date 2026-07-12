using System;

namespace Dev.Network
{
    public class ResourceVisible : Visible, IResourceVisible
    {
        public ResourceType Type;
        public int Amount = 5;

        private bool _isCollected;

        public event Action<ResourceType, int, ResourceVisible, object> OnCollected;

        public void Collect()
        {
            if (_isCollected)
                return;

            _isCollected = true;
            OnCollected?.Invoke(Type, Amount, this, this);
        }
    }
}
