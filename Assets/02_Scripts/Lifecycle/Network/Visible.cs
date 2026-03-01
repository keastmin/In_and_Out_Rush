using UnityEngine;

namespace Dev.Network
{
    public abstract class Visible : Entity, IVisible
    {
        protected Renderer[] renderers;

        public override void Spawned()
        {
            base.Spawned();
            renderers = GetComponentsInChildren<Renderer>();
        }

        public void SetVisibility(bool visible)
        {
            foreach (var renderer in renderers)
                renderer.enabled = visible;
        }

        public void Show()
            => SetVisibility(true);

        public void Hide()
            => SetVisibility(false);
    }   
}