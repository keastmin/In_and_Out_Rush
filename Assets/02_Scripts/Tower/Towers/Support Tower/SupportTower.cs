using UnityEngine;

namespace KIM.Dev
{
    public class SupportTower : Tower
    {
        public override void Spawned()
        {
            base.Spawned();
        }

        protected override void TowerDespawned()
        {
            base.TowerDespawned();
        }

        public override void Render()
        {
            base.Render();
        }


        private void OnDestroy()
        {

        }

        protected bool TryGetGrid(out InfiniteGrid gridManager)
        {
            gridManager = InfiniteGrid.Instance;
            if (gridManager == null)
            {
                gridManager = UnityEngine.Object.FindFirstObjectByType<InfiniteGrid>();
            }

            return gridManager != null;
        }
    }
}