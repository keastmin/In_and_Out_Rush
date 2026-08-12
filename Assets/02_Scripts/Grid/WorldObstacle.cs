using UnityEngine;

namespace KIM.Dev
{
    [DisallowMultipleComponent]
    public sealed class WorldObstacle : MonoBehaviour
    {
        [SerializeField] private Collider _collider;
        [SerializeField] private Transform _despawnPoint;
        private Collider[] _colliders;

        public Collider Collider => _collider;
        public Vector3 Position => transform.position;
        public Vector3 DespawnPosition => (_despawnPoint != null ? _despawnPoint : transform).position;
        public Bounds Bounds => CreateBounds();
        public Vector3 Size => Bounds.size;

        private void Awake()
        {
            RefreshColliderCache();
        }

        private void Reset()
        {
            RefreshColliderCache();
        }

        private void OnValidate()
        {
            RefreshColliderCache();
        }

        private void RefreshColliderCache()
        {
            if (_collider == null)
                TryGetComponent(out _collider);

            _colliders = GetComponentsInChildren<Collider>();
        }

        private Bounds CreateBounds()
        {
            if (TryCreateCombinedColliderBounds(out Bounds bounds))
                return bounds;

            return new Bounds(transform.position, Vector3.zero);
        }

        private bool TryCreateCombinedColliderBounds(out Bounds bounds)
        {
            bounds = default;
            bool hasBounds = false;

            EncapsulateColliderBounds(_collider, ref bounds, ref hasBounds);

            if (_colliders != null)
            {
                for (int i = 0; i < _colliders.Length; i++)
                {
                    EncapsulateColliderBounds(_colliders[i], ref bounds, ref hasBounds);
                }
            }

            return hasBounds;
        }

        private static void EncapsulateColliderBounds(Collider targetCollider, ref Bounds bounds, ref bool hasBounds)
        {
            if (targetCollider == null || !targetCollider.enabled || !targetCollider.gameObject.activeInHierarchy)
                return;

            Bounds colliderBounds = targetCollider.bounds;
            if (colliderBounds.size == Vector3.zero)
                return;

            if (!hasBounds)
            {
                bounds = colliderBounds;
                hasBounds = true;
                return;
            }

            bounds.Encapsulate(colliderBounds);
        }
    }
}
