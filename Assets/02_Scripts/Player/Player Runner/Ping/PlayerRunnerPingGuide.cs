using Fusion;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunnerPingGuide : NetworkBehaviour
{
    #region Field

    private const float MinimumDirectionSqrMagnitude = 0.0001f;

    // 핑 가이드 오브젝트
    [SerializeField] private GameObject _pingGuidePrefab;
    // 플레이어 러너의 중심에서 얼마나 떨어져서 회전할지 정하는 거리
    [SerializeField] private float _pingGuideOffsetDistance = 3f; 

    private Dictionary<PlayerPing, GameObject> _pingGuideDictionary;

    #endregion

    #region MonoBehaviour

    private void Awake()
    {
        // 딕셔너리 초기화
        _pingGuideDictionary = new Dictionary<PlayerPing, GameObject>();
    }

    private void OnDestroy()
    {
        if (_pingGuideDictionary == null)
            return;

        foreach (GameObject guide in _pingGuideDictionary.Values)
        {
            if (guide != null)
                Destroy(guide);
        }

        _pingGuideDictionary.Clear();
    }

    public override void Render()
    {
        base.Render();

        // 러너 입력 권한이 없으면 작동 안 함
        if (!HasInputAuthority)
            return;

        // 각 핑의 방향에 맞게 핑 가이드 회전
        RotatePingGuide();
    }

    #endregion

    #region API

    // 핑 가이드 생성
    public void CreatePingGuide(PlayerPing ping)
    {
        // 핑 가이드 생성
        GameObject pingGuide = Instantiate(_pingGuidePrefab);

        // 딕셔너리에 실제 핑과 그 가이드를 저장
        _pingGuideDictionary.Add(ping, pingGuide);
    }

    // 핑 가이드 파괴
    public void DestroyPingGuide(PlayerPing ping)
    {
        // 핑 가이드 딕셔너리에 해당 핑이 존재하면 가이드를 파괴하고 딕셔너리에서 제거
        if (_pingGuideDictionary.ContainsKey(ping))
        {
            Destroy(_pingGuideDictionary[ping]);
            _pingGuideDictionary.Remove(ping);
        }
    }

    #endregion

    #region Core

    // 각 핑에 맞게 핑 가이드를 회전시키는 함수
    private void RotatePingGuide()
    {
        foreach (var pingPair in _pingGuideDictionary)
        {
            PlayerPing ping = pingPair.Key;
            GameObject guide = pingPair.Value;

            // 러너부터 핑까지의 방향 구하기
            Vector3 runnerPosition = transform.position;
            Vector3 pingPosition = ping.transform.position;
            Vector3 planarRunnerPosition = runnerPosition;
            planarRunnerPosition.y = 0f;
            pingPosition.y = 0f;

            Vector3 offset = pingPosition - planarRunnerPosition;
            float sqrDistance = offset.sqrMagnitude;
            if (sqrDistance < MinimumDirectionSqrMagnitude)
            {
                guide.SetActive(false);
                continue;
            }

            guide.SetActive(true);

            float distance = Mathf.Sqrt(sqrDistance);
            Vector3 direction = offset / distance;
            float guideDistance = Mathf.Min(_pingGuideOffsetDistance, distance);
            Vector3 guidePosition = runnerPosition + (direction * guideDistance);
            Quaternion guideRotation = Quaternion.LookRotation(direction, Vector3.up);

            // 가이드 위치 및 회전 조정
            guide.transform.SetPositionAndRotation(guidePosition, guideRotation);
        }
    }

    #endregion

    #region Debug

    private void OnDrawGizmos()
    {
        
    }

    #endregion
}
