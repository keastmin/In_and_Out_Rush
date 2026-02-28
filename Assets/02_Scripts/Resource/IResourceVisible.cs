using System;

namespace Dev
{
    public interface IResourceVisible : IVisible
    {
        event Action OnCollected;
    }
}