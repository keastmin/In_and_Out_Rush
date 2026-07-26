using UnityEngine;

namespace KIM.Dev
{
    [System.Serializable]
    public sealed class InfiniteGridTrackBlockingSettings
    {
        public const float MinimumLineWidth = 0.1f;

        [Header("Monster Track Blocking")]
        [Tooltip("이 두께를 가진 닫힌 몬스터 트랙 선이 육각 셀 영역에 닿으면 해당 셀을 차단합니다.")]
        [SerializeField][Min(MinimumLineWidth)] private float _lineWidth = 1f;

        public float LineWidth => Mathf.Max(MinimumLineWidth, _lineWidth);
        public float HalfLineWidth => LineWidth * 0.5f;
    }
}
