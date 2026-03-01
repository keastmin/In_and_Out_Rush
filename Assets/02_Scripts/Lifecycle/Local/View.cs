using UnityEngine;

namespace Dev.Local
{
    public class View : Visible
    {
        protected RectTransform rectTransform;

        protected override void OnInitialize()
            => TryGetComponent(out rectTransform);

        protected override void OnDispose()
            => rectTransform = null;
    }
}