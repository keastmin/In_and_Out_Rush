using System;

namespace Dev.Network
{
    public interface IResourceVisible : IVisible
    {
        event Action<ResourceType, int, ResourceVisible, object> OnCollected;
    }
}