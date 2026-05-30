using UnityEngine;

namespace KIM.Dev
{
    public class DragSection : MonoBehaviour
    {
        private RectTransform _rectTransform;
        public RectTransform DragRect => _rectTransform;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        /// <summary>
        /// 월드 지점을 화면 앵커드 포지션으로 변환하고 endPos도 앵커드 포지션으로 변환하여 두 지점을 이용하여 사각형을 그림
        /// </summary>
        /// <param name="worldPos">시작 월드 포지션</param>
        /// <param name="endPos">끝 마우스 포지션</param>
        public void Drawing(Vector2 startPos, Vector2 endPos)
        {
            Vector2 center = (startPos + endPos) / 2f;
            float width = Mathf.Abs(startPos.x - endPos.x);
            float height = Mathf.Abs(startPos.y - endPos.y);
            _rectTransform.anchoredPosition = center;
            _rectTransform.sizeDelta = new Vector2(width, height);
        }
    }

}