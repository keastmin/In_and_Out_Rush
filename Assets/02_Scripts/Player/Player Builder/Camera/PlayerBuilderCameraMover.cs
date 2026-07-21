using System;
using Unity.Cinemachine;
using UnityEngine;

namespace KIM.Dev
{
    [Serializable]
    public class PlayerBuilderCameraMover
    {
        [SerializeField] private CinemachineCamera _cineCam;
        [SerializeField] private float _cameraBorderThickness = 10f;
        [SerializeField] private float _cameraMoveSpeed = 10f;
        [SerializeField] private float _cameraMaxZoomDistance = 11f;
        [SerializeField] private float _cameraWheelPerZoomDistance = 10f;
        [SerializeField] private float _zoomSmoothTime = 0.12f;
        [SerializeField] private float _camZoomDistance;
        [SerializeField] private float _zoomTargetDistance;
        [SerializeField] private float _zoomVel;
        [SerializeField] private bool _isLock;
        [SerializeField] private bool _allowZoom = true;

        public void SetCamera(CinemachineCamera cineCam)
        {
            _cineCam = cineCam;
            ResetZoomState();
        }

        public void Move()
        {
            if (_cineCam == null)
                return;

            if (Input.GetKeyDown(KeyCode.V))
            {
                _isLock = !_isLock;
            }

            if (_isLock)
                return;

            Transform cameraTransform = _cineCam.transform;
            if (!Input.GetKey(KeyCode.Space))
            {
                Vector3 moveDir = GetMoveDirection(Input.mousePosition, Screen.width, Screen.height, _cameraBorderThickness);
                if (moveDir.sqrMagnitude > 0f)
                {
                    if (moveDir.sqrMagnitude > 1f)
                    {
                        moveDir.Normalize();
                    }

                    Vector3 move = moveDir * (_cameraMoveSpeed * Time.deltaTime);
                    cameraTransform.position += new Vector3(move.x, 0f, move.z);
                }
            }

            if (!_allowZoom)
                return;

            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            Vector3 forward = cameraTransform.forward.normalized;
            Vector3 zoomOrigin = cameraTransform.position - forward * _camZoomDistance;

            if (Mathf.Abs(scroll) > 0.0001f)
            {
                _zoomTargetDistance += scroll * _cameraWheelPerZoomDistance;
                _zoomTargetDistance = Mathf.Clamp(_zoomTargetDistance, 0f, _cameraMaxZoomDistance);
            }

            _camZoomDistance = Mathf.SmoothDamp(
                _camZoomDistance,
                _zoomTargetDistance,
                ref _zoomVel,
                _zoomSmoothTime);

            cameraTransform.position = zoomOrigin + forward * _camZoomDistance;
        }

        private void ResetZoomState()
        {
            _camZoomDistance = 0f;
            _zoomTargetDistance = 0f;
            _zoomVel = 0f;
            _isLock = false;
        }

        private static Vector3 GetMoveDirection(Vector3 mousePosition, float screenWidth, float screenHeight, float borderThickness)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (mousePosition.x < borderThickness)
            {
                horizontal -= 1f;
            }
            else if (mousePosition.x > screenWidth - borderThickness)
            {
                horizontal += 1f;
            }

            if (mousePosition.y < borderThickness)
            {
                vertical -= 1f;
            }
            else if (mousePosition.y > screenHeight - borderThickness)
            {
                vertical += 1f;
            }

            return new Vector3(horizontal, 0f, vertical);
        }
    }

}