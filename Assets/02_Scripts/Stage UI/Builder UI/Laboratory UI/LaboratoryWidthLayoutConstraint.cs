using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    [DisallowMultipleComponent]
    public sealed class LaboratoryWidthLayoutConstraint : MonoBehaviour, ILayoutElement
    {
        [SerializeField, Min(0f)] private float _flexibleWidth = 1f;

        public float minWidth => 0f;
        public float preferredWidth => 0f;
        public float flexibleWidth => _flexibleWidth;
        public float minHeight => -1f;
        public float preferredHeight => -1f;
        public float flexibleHeight => -1f;
        public int layoutPriority => 2;

        public void SetFlexibleWidth(float flexibleWidth)
        {
            _flexibleWidth = Mathf.Max(0f, flexibleWidth);
        }

        public void CalculateLayoutInputHorizontal()
        {
        }

        public void CalculateLayoutInputVertical()
        {
        }
    }
}
