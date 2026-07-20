using System;
using Dev.Local;
using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public class Gate : Visible
    {
        private CollisionField collisionField;

        [Networked, OnChangedRender(nameof(HandleUnlockedChanged))]
        public bool IsUnlocked { get; private set; }

        public event Action<PlayerRunner, Gate, object> OnPlayerRunnerEntered;

        public override void Spawned()
        {
            base.Spawned();
            RefreshUnlockedState();
        }

        protected override void OnInitialize()
        {
            if (Object.HasStateAuthority)
                IsUnlocked = false;

            Transform colliders = transform.Find("Colliders");
            Transform entrance = colliders != null ? colliders.Find("Entrance") : null;
            if (entrance == null || !entrance.TryGetComponent(out collisionField))
            {
                Debug.LogWarning($"{nameof(Gate)} could not find its entrance collision field.", this);
                return;
            }

            collisionField.OnCollisionFieldEntered += HandleCollisionFieldEntered;
            RefreshUnlockedState();
        }

        protected override void OnDispose()
        {
            if (collisionField != null)
                collisionField.OnCollisionFieldEntered -= HandleCollisionFieldEntered;
        }

        public void SetUnlocked(bool unlocked)
        {
            if (!Object.HasStateAuthority)
                return;

            if (IsUnlocked == unlocked)
                return;

            IsUnlocked = unlocked;
            RefreshUnlockedState();
        }

        private void HandleCollisionFieldEntered(Collider other, CollisionField collisionField, object sender)
        {
            if (!Object.HasStateAuthority)
                return;

            if (!IsUnlocked)
            {
                Debug.Log("Gate is locked. Sacred zone quota has not been reached.");
                return;
            }

            var runner = other.GetComponentInParent<PlayerRunner>();
            if (runner == null)
                return;

            OnPlayerRunnerEntered?.Invoke(runner, this, sender);
        }

        private void HandleUnlockedChanged()
        {
            RefreshUnlockedState();
        }

        private void RefreshUnlockedState()
        {
            if (renderers == null)
                return;

            Color color = IsUnlocked ? Color.white : new Color(0.35f, 0.45f, 0.55f, 1f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null)
                    continue;

                var propertyBlock = new MaterialPropertyBlock();
                renderers[i].GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor("_BaseColor", color);
                propertyBlock.SetColor("_Color", color);
                renderers[i].SetPropertyBlock(propertyBlock);
            }
        }
    }
}
