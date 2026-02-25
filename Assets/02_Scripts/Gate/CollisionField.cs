using System;
using UnityEngine;

namespace Dev
{
    public class CollisionField : MonoBehaviour
    {
        public event Action<Collider, object> OnCollisionFieldEnter;

        private void OnTriggerEnter(Collider other)
        {
            OnCollisionFieldEnter?.Invoke(other, this);
        }
    }
}