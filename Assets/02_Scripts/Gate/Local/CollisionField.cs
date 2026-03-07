using System;
using UnityEngine;

namespace Dev.Local
{
    public class CollisionField : Entity
    {
        public event Action<Collider, CollisionField, object> OnCollisionFieldEntered;

        private void OnTriggerEnter(Collider other)
        {
            OnCollisionFieldEntered?.Invoke(other, this, this);
        }
    }
}