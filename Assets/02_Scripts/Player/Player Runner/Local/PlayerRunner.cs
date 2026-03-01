using System;
using UnityEngine;

namespace Dev.Local
{
    public class PlayerRunner : Visible
    {
        public event Action<Vector3, PlayerRunner, object> OnMoved;

        void Update()
        {
            UpdateMovement();
        }

        void UpdateMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            var isDash = Input.GetKey(KeyCode.LeftShift);
            Vector3 movement = StageInstance.Instance.MovementSpeed * Time.deltaTime * new Vector3(horizontal, 0, vertical);
            if (isDash) { movement *= 2; }
            transform.Translate(movement, Space.World);

            if (movement != Vector3.zero)
                OnMoved?.Invoke(transform.position, this, this);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.back * 3, 0.5f);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position + Vector3.back * 3 + Vector3.forward * 20f, 0.5f);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position + Vector3.back * 3, transform.position + Vector3.back * 3 + Vector3.forward * 20f);
        }
    }
}