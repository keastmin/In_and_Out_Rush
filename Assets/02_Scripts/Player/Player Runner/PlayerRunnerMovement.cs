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

        public void UpdateMovement(bool isDashing, Vector3 direction)
        {
            if (_targetRigidbody == null) return;
            if (Globals.Store == null) return; // 임시

            var movementSpeed = Globals.Store.PlayerRunnerMovementSpeed;
            var dashScaler = Globals.Store.PlayerRunnerDashScaler;
            var actualMovementSpeed = isDashing ? movementSpeed * dashScaler : movementSpeed;
            _targetRigidbody.linearVelocity = actualMovementSpeed * direction;
            _targetRigidbody.transform.LookAt(_targetRigidbody.transform.position + direction);
        }
    }
}