using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace KIM.Dev
{
    public class DragSystem : MonoBehaviour
    {
        [SerializeField] private DragSection _dragSection;

        private void Awake()
        {
            DragEnd();
        }

        private void OnEnable()
        {
            DragEnd();
        }

        private void OnDisable()
        {
            DragEnd();
        }

        #region API

        public void DragStart()
        {
            _dragSection.gameObject.SetActive(true);
        }

        /// <summary>
        /// 이 함수가 호출되는 동안에는 드래그를 유지하고 드래그 상태가 아닐 때 처음 호출되면 초기화를 진행
        /// </summary>
        /// <param name="mousePos">마우스 위치</param>
        public void Dragging(Vector2 startPos, Vector2 endPos)
        {
            _dragSection.Drawing(startPos, endPos);
        }

        /// <summary>
        /// 드래그를 종료하는 함수
        /// </summary>
        public void DragEnd()
        {
            _dragSection.gameObject.SetActive(false);
        }

        public Rect GetDragSectionPixel()
        {
            RectTransform rt = _dragSection.DragRect;
            Vector3[] wc = new Vector3[4];
            rt.GetWorldCorners(wc);

            float xMin = float.PositiveInfinity, yMin = float.PositiveInfinity, xMax = float.NegativeInfinity, yMax = float.NegativeInfinity;
            foreach (var w in wc)
            {
                var ps = RectTransformUtility.WorldToScreenPoint(null, w);
                xMin = Mathf.Min(xMin, ps.x); xMax = Mathf.Max(xMax, ps.x);
                yMin = Mathf.Min(yMin, ps.y); yMax = Mathf.Max(yMax, ps.y);
            }

            return Rect.MinMaxRect(xMin, yMin, xMax, yMax);
        }

        #endregion
    }

}