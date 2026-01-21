using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    [Header("도착 판정(미세 오차용)")]
    [SerializeField] private float _arriveDistance = 0.05f;

    private Collider _target;
    private Vector3 _lastTargetPos;
    private float _bulletSpeed;

    private Rigidbody _rb;
    private Vector3 _prevPos;
    private bool _initialized;

    private float ArriveSqr => _arriveDistance * _arriveDistance;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    public void InitBullet(Collider target, float speed)
    {
        _target = target;
        _bulletSpeed = speed;

        // 타겟이 즉시 파괴될 수 있으니 "마지막 위치"를 확보해 둠
        _lastTargetPos = target != null ? target.bounds.center : transform.position;

        _prevPos = _rb.position;
        _initialized = true;

        // 첫 틱부터 바로 날아가게
        UpdateVelocityTowardLastPos();
    }

    private void FixedUpdate()
    {
        if (!_initialized) return;

        // 지난 틱 대비 이동량(이미 물리 시뮬이 반영된 현재 위치 기준)
        Vector3 currPos = _rb.position;
        Vector3 step = currPos - _prevPos;

        // 타겟이 살아있으면 마지막 위치 갱신
        SetLastTargetPosition();

        // 1) _lastTargetPos에 도착(근접)했으면 제거
        Vector3 toTarget = _lastTargetPos - currPos;
        if (toTarget.sqrMagnitude <= ArriveSqr)
        {
            Destroy(gameObject);
            return;
        }

        // 2) _lastTargetPos를 "지나쳤으면" 제거
        //    (이전 프레임에서의 이동 방향(step) 기준으로, 타겟이 뒤로 넘어가면 지나친 것)
        if (step.sqrMagnitude > 1e-8f && Vector3.Dot(toTarget, step) <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        // 다음 틱 비교를 위해 현재 위치 저장
        _prevPos = currPos;

        // 다음 물리 스텝에서 사용할 속도 갱신
        UpdateVelocityTowardLastPos();
    }

    private void SetLastTargetPosition()
    {
        // UnityEngine.Object는 Destroy되면 (== null)로 null처럼 취급됨
        if (_target != null)
            _lastTargetPos = _target.bounds.center;
    }

    private void UpdateVelocityTowardLastPos()
    {
        Vector3 dir = _lastTargetPos - _rb.position;
        if (dir.sqrMagnitude <= 1e-8f)
        {
            SetLinearVelocity(Vector3.zero);
            return;
        }

        dir.Normalize();
        SetLinearVelocity(dir * _bulletSpeed);
    }

    private void SetLinearVelocity(Vector3 v)
    {
        _rb.linearVelocity = v;
    }
}
