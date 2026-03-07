using System;
using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public class Gate : Visible
    {
        private CollisionField collisionField;

        public event Action<Collider, Gate, object> OnGateEntered;

        protected override void OnInitialize()
        {
            transform.Find("Colliders").Find("Entrance").TryGetComponent(out collisionField);
            collisionField.OnCollisionFieldEntered += HandleCollisionFieldEntered;
        }

        private void HandleCollisionFieldEntered(Collider other, CollisionField collisionField, object sender)
        {
            if (Object.HasStateAuthority)
                OnGateEntered?.Invoke(other, this, sender);
        }
    }
}