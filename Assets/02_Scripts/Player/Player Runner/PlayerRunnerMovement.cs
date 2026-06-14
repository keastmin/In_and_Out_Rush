using UnityEngine;

namespace Dev.Network
{
    public class PlayerRunnerMovement : Entity
    {
        private Rigidbody _targetRigidbody;

        public void SetTarget(Transform targetTransform)
        {
            targetTransform.TryGetComponent(out _targetRigidbody);
        }

        public void UpdateMovement(float movementSpeed, bool isDashing, Vector3 direction)
        {
            if (_targetRigidbody == null) return;
            if (Globals.Store == null) return; // 임시

            var dashScaler = Globals.Store.PlayerRunnerDashScaler;
            var actualMovementSpeed = isDashing ? movementSpeed * dashScaler : movementSpeed;
            _targetRigidbody.linearVelocity = actualMovementSpeed * direction;
            _targetRigidbody.transform.LookAt(_targetRigidbody.transform.position + direction);
        }

        public void Stop()
        {
            if (_targetRigidbody == null) return;

            _targetRigidbody.linearVelocity = Vector3.zero;
            _targetRigidbody.angularVelocity = Vector3.zero;
        }
    }
}
