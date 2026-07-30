using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace KIM.Dev
{
    [RequireComponent(typeof(CinemachineCamera))]
    [RequireComponent(typeof(CinemachineFollow))]
    public sealed class PlayerRunnerCinemachineController : MonoBehaviour
    {
        private CinemachineCamera _camera;
        private CinemachineFollow _follow;
        private Transform _runnerTarget;
        private Quaternion _defaultRotation;
        private LensSettings _defaultLens;
        private Vector3 _defaultFollowOffset;
        private TrackerSettings _defaultTrackerSettings;
        private bool _hasDefaultProfile;

        private void Awake()
        {
            CacheComponents();
            CaptureDefaultProfile();
        }

        public void Initialize(Transform runnerTarget)
        {
            CacheComponents();
            CaptureDefaultProfile();

            _runnerTarget = runnerTarget;
            RestoreRunnerView();
        }

        public void ApplyObservationView(CinemachineCamera sourceCamera)
        {
            if (sourceCamera == null ||
                _camera == null ||
                _follow == null ||
                _runnerTarget == null)
            {
                return;
            }

            Transform sourceTransform = sourceCamera.transform;
            Vector3 forward = sourceTransform.forward.normalized;
            float forwardY = forward.y;
            Vector3 desiredPosition;

            if (Mathf.Abs(forwardY) > Mathf.Epsilon)
            {
                float distance =
                    (_runnerTarget.position.y - sourceTransform.position.y) / forwardY;
                desiredPosition = distance > 0f
                    ? _runnerTarget.position - forward * distance
                    : _runnerTarget.position + _defaultFollowOffset;
            }
            else
            {
                desiredPosition = _runnerTarget.position + _defaultFollowOffset;
            }

            TrackerSettings trackerSettings = _defaultTrackerSettings;
            trackerSettings.BindingMode = BindingMode.WorldSpace;

            _camera.Target.TrackingTarget = _runnerTarget;
            _camera.Lens = sourceCamera.Lens;
            _camera.transform.rotation = sourceTransform.rotation;
            _follow.TrackerSettings = trackerSettings;
            _follow.FollowOffset = desiredPosition - _runnerTarget.position;
            _camera.ForceCameraPosition(desiredPosition, sourceTransform.rotation);
        }

        private void RestoreRunnerView()
        {
            if (!_hasDefaultProfile || _runnerTarget == null)
                return;

            _camera.Target.TrackingTarget = _runnerTarget;
            _camera.Lens = _defaultLens;
            _camera.transform.rotation = _defaultRotation;
            _follow.TrackerSettings = _defaultTrackerSettings;
            _follow.FollowOffset = _defaultFollowOffset;
        }

        private void CacheComponents()
        {
            if (_camera == null)
                _camera = GetComponent<CinemachineCamera>();

            if (_follow == null)
                _follow = GetComponent<CinemachineFollow>();
        }

        private void CaptureDefaultProfile()
        {
            if (_hasDefaultProfile || _camera == null || _follow == null)
                return;

            _defaultRotation = _camera.transform.rotation;
            _defaultLens = _camera.Lens;
            _defaultFollowOffset = _follow.FollowOffset;
            _defaultTrackerSettings = _follow.TrackerSettings;
            _hasDefaultProfile = true;
        }
    }
}
