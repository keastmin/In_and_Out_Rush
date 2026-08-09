using Unity.Cinemachine;
using UnityEngine;

namespace KIM.Dev
{
    [RequireComponent(typeof(CinemachineCamera))]
    public sealed class PlayerBuilderCinemachineController : MonoBehaviour
    {
        [SerializeField] private float _cameraBorderThickness = 10f;
        [SerializeField] private float _cameraMoveSpeed = 10f;
        [SerializeField] private float _cameraMaxZoomDistance = 11f;
        [SerializeField] private float _cameraWheelPerZoomDistance = 10f;
        [SerializeField] private float _zoomSmoothTime = 0.12f;
        [SerializeField] private bool _allowZoom = true;

        private CinemachineCamera _camera;
        private Transform _laboratoryTarget;
        private float _cameraZoomDistance;
        private float _zoomTargetDistance;
        private float _zoomVelocity;
        private bool _isLocalBuilder;
        private bool _isGameplayInputEnabled;
        private bool _isViewActive;
        private bool _isLocked;

        public CinemachineCamera Camera => _camera;

        public bool TryGetGroundFocusPosition(out Vector3 groundPosition)
        {
            if (_camera == null)
            {
                groundPosition = default;
                return false;
            }

            Transform cameraTransform = _camera.transform;
            Vector3 forward = cameraTransform.forward;
            if (Mathf.Abs(forward.y) <= Mathf.Epsilon)
            {
                groundPosition = default;
                return false;
            }

            float distanceToGround = -cameraTransform.position.y / forward.y;
            if (distanceToGround < 0f)
            {
                groundPosition = default;
                return false;
            }

            groundPosition = cameraTransform.position + forward * distanceToGround;
            groundPosition.y = 0f;
            return true;
        }

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
        }

        private void LateUpdate()
        {
            if (!_isLocalBuilder || !_isGameplayInputEnabled || !_isViewActive || _camera == null)
                return;

            HandleLockInput();
            HandleCenterOnLaboratoryInput();

            if (_isLocked)
                return;

            Move();
            Zoom();
        }

        public void Initialize(bool isLocalBuilder)
        {
            if (_camera == null)
                _camera = GetComponent<CinemachineCamera>();

            _isLocalBuilder = isLocalBuilder;
            _isGameplayInputEnabled = true;
            _isViewActive = isLocalBuilder;
            ResetRuntimeState();
        }

        public void SetGameplayInputEnabled(bool isEnabled)
        {
            _isGameplayInputEnabled = isEnabled;
        }

        public void SetViewActive(bool isActive)
        {
            _isViewActive = isActive;
        }

        public void SetLaboratoryTarget(Transform laboratoryTarget)
        {
            _laboratoryTarget = laboratoryTarget;
        }

        private void HandleLockInput()
        {
            if (Input.GetKeyDown(KeyCode.V))
            {
                _isLocked = !_isLocked;
            }
        }

        private void HandleCenterOnLaboratoryInput()
        {
            if (!Input.GetKeyDown(KeyCode.LeftControl) &&
                !Input.GetKeyDown(KeyCode.RightControl))
            {
                return;
            }

            CenterOnLaboratory();
        }

        private void CenterOnLaboratory()
        {
            if (_laboratoryTarget == null)
                return;

            Transform cameraTransform = _camera.transform;
            Vector3 forward = cameraTransform.forward.normalized;
            float forwardY = forward.y;

            if (Mathf.Abs(forwardY) <= Mathf.Epsilon)
            {
                Vector3 fallbackPosition = cameraTransform.position;
                fallbackPosition.x = _laboratoryTarget.position.x;
                fallbackPosition.z = _laboratoryTarget.position.z;
                cameraTransform.position = fallbackPosition;
                return;
            }

            float distance = (_laboratoryTarget.position.y - cameraTransform.position.y) / forwardY;
            if (distance <= 0f)
                return;

            cameraTransform.position = _laboratoryTarget.position - forward * distance;
        }

        private void Move()
        {
            Transform cameraTransform = _camera.transform;
            Vector3 moveDirection = GetMoveDirection(
                Input.mousePosition,
                Screen.width,
                Screen.height,
                _cameraBorderThickness);

            if (moveDirection.sqrMagnitude <= 0f)
                return;

            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            Vector3 move = moveDirection * (_cameraMoveSpeed * Time.deltaTime);
            cameraTransform.position += new Vector3(move.x, 0f, move.z);
        }

        private void Zoom()
        {
            if (!_allowZoom)
                return;

            Transform cameraTransform = _camera.transform;
            float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
            Vector3 forward = cameraTransform.forward.normalized;
            Vector3 zoomOrigin = cameraTransform.position - forward * _cameraZoomDistance;

            if (Mathf.Abs(scroll) > 0.0001f)
            {
                _zoomTargetDistance += scroll * _cameraWheelPerZoomDistance;
                _zoomTargetDistance = Mathf.Clamp(
                    _zoomTargetDistance,
                    0f,
                    _cameraMaxZoomDistance);
            }

            _cameraZoomDistance = Mathf.SmoothDamp(
                _cameraZoomDistance,
                _zoomTargetDistance,
                ref _zoomVelocity,
                _zoomSmoothTime);

            cameraTransform.position = zoomOrigin + forward * _cameraZoomDistance;
        }

        private void ResetRuntimeState()
        {
            _cameraZoomDistance = 0f;
            _zoomTargetDistance = 0f;
            _zoomVelocity = 0f;
            _isLocked = false;
        }

        private static Vector3 GetMoveDirection(
            Vector3 mousePosition,
            float screenWidth,
            float screenHeight,
            float borderThickness)
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
