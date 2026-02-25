using System;
using Fusion;
using UnityEngine;

namespace Dev
{
    public class Gate : NetworkBehaviour
    {
        [SerializeField] private int targetSceneIndex;
        private CollisionField collisionField;

        public event Action<int> OnGateEntered;

        private void Awake()
        {
            transform.Find("Colliders").Find("Entrance").TryGetComponent(out collisionField);
            collisionField.OnCollisionFieldEnter += HandleCollisionFieldEnter;
        }

        private void HandleCollisionFieldEnter(Collider other, object sender)
        {
            OnGateEntered?.Invoke(targetSceneIndex);
        }
    }
}