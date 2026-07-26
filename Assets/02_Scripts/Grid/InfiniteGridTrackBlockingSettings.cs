using UnityEngine;

namespace KIM.Dev
{
    [System.Serializable]
    public sealed class InfiniteGridTrackBlockingSettings
    {
        public const float MinimumLineWidth = 0.1f;

        [Header("Monster Track Blocking")]
        [Tooltip("셀 중심과 닫힌 몬스터 트랙 선 사이의 거리가 이 값의 절반 이하일 때 셀을 차단합니다.")]
        [SerializeField][Min(MinimumLineWidth)] private float _lineWidth = 1f;

        public float LineWidth => Mathf.Max(MinimumLineWidth, _lineWidth);
        public float HalfLineWidth => LineWidth * 0.5f;
    }
}
