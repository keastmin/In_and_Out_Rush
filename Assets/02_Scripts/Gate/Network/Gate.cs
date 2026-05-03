using System;
using Dev.Local;
using UnityEngine;

namespace Dev.Network
{
    public class Gate : Visible
    {
        private CollisionField collisionField;

        public event Action<PlayerRunner, Gate, object> OnPlayerRunnerEntered;

        protected override void OnInitialize()
        {
            transform.Find("Colliders").Find("Entrance").TryGetComponent(out collisionField);
            collisionField.OnCollisionFieldEntered += HandleCollisionFieldEntered;
        }

        protected override void OnDispose()
        {
            if (collisionField != null)
                collisionField.OnCollisionFieldEntered -= HandleCollisionFieldEntered;
        }

        private void HandleCollisionFieldEntered(Collider other, CollisionField collisionField, object sender)
        {
            if (!Object.HasStateAuthority)
                return;

            var runner = other.GetComponentInParent<PlayerRunner>();
            if (runner == null)
                return;

            OnPlayerRunnerEntered?.Invoke(runner, this, sender);
        }
    }
}
