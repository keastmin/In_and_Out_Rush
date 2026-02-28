using UnityEngine;

namespace Dev.Local
{
    public class Visible : MonoBehaviour, IVisible
    {
        public void SetVisibility(bool visible)
            => gameObject.SetActive(visible);

        public void Show()
            => gameObject.SetActive(true);

        public void Hide()
            => gameObject.SetActive(false);
    }
}