using System;

namespace Dev.Network
{
    public class ResourceVisible : Visible
    {
        public ResourceType Type;
        public int Amount = 5;

        public event Action<ResourceType, int, ResourceVisible, object> OnCollected;

        public void Collect()
            => OnCollected?.Invoke(Type, Amount, this, this);
    }
}