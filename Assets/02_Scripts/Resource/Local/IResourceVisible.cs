using System;

namespace Dev.Local
{
    public interface IResourceVisible : IVisible
    {
        event Action<ResourceType, int, ResourceVisible, object> OnCollected;
    }
}