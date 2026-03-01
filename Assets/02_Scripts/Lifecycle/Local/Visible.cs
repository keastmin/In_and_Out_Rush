namespace Dev.Local
{
    public abstract class Visible : Entity, IVisible
    {
        public void SetVisibility(bool visible)
            => gameObject.SetActive(visible);

        public void Show()
            => gameObject.SetActive(true);

        public void Hide()
            => gameObject.SetActive(false);
    }
}