using DG.Tweening;
using UnityEngine;

namespace KIM.Dev
{
    public class UIShortcutPart : MonoBehaviour
    {
        public UIShortcutLongPressInfo[] ShortcutLongPresses;
        public UIShortcutInfo[] Shortcuts;

        [Header("Tween")]
        [SerializeField] private float _activeDuration = 0.18f; // 켜질 때 시간
        [SerializeField] private float _deactiveDuration = 0.14f; // 꺼질 때 시간
        [SerializeField] private float _overshoot = 1.1f;  // OutBack 튕김 강도(1.0~1.7권장)
        [SerializeField] private bool _ignoreTimeScale = true;  // 일시정지 중에도 동작하려면 true

        private Tween _scaleTween;

        public virtual void ActiveThisPart()
        {
            // 이미 켜지는 중/켜져 있던 트윈 정리
            _scaleTween?.Kill();

            // 활성화하고 스케일 0에서 시작
            gameObject.SetActive(true);
            transform.localScale = Vector3.zero;

            // 0 → 1, 한 번만 바운스(OutBack)
            _scaleTween = transform
                .DOScale(Vector3.one, _activeDuration)
                .SetEase(Ease.OutBack, _overshoot)   // overshoot로 바운스 강도 조절
                .SetUpdate(_ignoreTimeScale);
        }

        public virtual void DeactiveThisPart()
        {
            // 진행 중 트윈 정리
            _scaleTween?.Kill();

            // 1(or 현재값) → 0, 부드럽게 축소 후 비활성화
            _scaleTween = transform
                .DOScale(Vector3.zero, _deactiveDuration)
                .SetEase(Ease.InBack)                // 또는 Ease.InQuad/OutSine 등 취향대로
                .SetUpdate(_ignoreTimeScale)
                .OnComplete(() => gameObject.SetActive(false));
        }
    }

}