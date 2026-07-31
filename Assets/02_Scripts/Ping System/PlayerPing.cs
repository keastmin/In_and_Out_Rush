using UnityEngine;

public class PlayerPing : MonoBehaviour
{
    [SerializeField] private float _pingLifeTime = 2f; // 핑이 살아있는 시간(초)

    private PlayerRunnerPingGuide _playerRunnerPingGuide;

    private float _currentLifeTime;
    private bool _isInitialize = false;

    private void Update()
    {
        // 초기화가 된 후에 동작
        if (!_isInitialize)
            return;

        // 핑 생존시간 카운팅
        CountLifeTime();

        // 생존시간이 끝나면 핑 제거
        if(_currentLifeTime <= 0f)
        {
            DestroyPing();
        }
    }

    // 핑 초기화
    public void InitializePlayerPing(PlayerRunnerPingGuide runnerPingGuide)
    {
        // 플레이어 러너 핑 가이드 컴포넌트 캐싱
        _playerRunnerPingGuide = runnerPingGuide;

        // 플레이어 러너 입력 권한이 있으면 핑 가이드에 핑 가이드 생성 요청
        if (_playerRunnerPingGuide.HasInputAuthority)
            _playerRunnerPingGuide.CreatePingGuide(this);

        // 생존시간 초기화
        _currentLifeTime = _pingLifeTime;

        // 초기화 완료
        _isInitialize = true;
    }

    // 이 핑의 생존시간 카운팅
    private void CountLifeTime()
    {
        _currentLifeTime -= Time.deltaTime;
    }

    // 핑 제거
    private void DestroyPing()
    {
        // 플레이어 러너 입력 권한이 있으면 핑 가이드 제거
        if (_playerRunnerPingGuide.HasInputAuthority)
            _playerRunnerPingGuide.DestroyPingGuide(this);

        // 현재 핑 파괴
        Destroy(this.gameObject);
    }
}
