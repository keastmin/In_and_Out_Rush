using System;
using UnityEngine;

namespace Dev.Local
{
    public class ResourceVisible : Visible, IResourceVisible
    {
        public ResourceType Type;
        public int Amount = 5;

        public event Action OnCollected;

        public void Obtain()
        {
            // 자원 획득 로직
            Debug.Log($"Obtained {Amount} resources from {gameObject.name}");
            Destroy(gameObject); // 자원 획득 후 제거
        }

        public void Collect()
        {
            OnCollected?.Invoke();
        }
    }
}