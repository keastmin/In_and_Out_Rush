using UnityEngine;

namespace KIM.Dev
{
    [DisallowMultipleComponent]
    public sealed class WorldObstacle : MonoBehaviour
    {
        [SerializeField] private Collider _collider;

        public Collider Collider => _collider;
        public Vector3 Position => transform.position;
        public Bounds Bounds => _collider != null
            ? _collider.bounds
            : new Bounds(transform.position, Vector3.zero);
        public Vector3 Size => Bounds.size;

        private void Awake()
        {
            ResolveCollider();
        }

        private void Reset()
        {
            ResolveCollider();
        }

        private void OnValidate()
        {
            ResolveCollider();
        }

        private void ResolveCollider()
        {
            if (_collider == null)
                TryGetComponent(out _collider);
        }
    }
}
