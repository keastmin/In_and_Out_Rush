using UnityEngine;

namespace KIM.Dev
{
    public abstract class CellBuffSupportTower : SupportTower
    {
        [Header("버프 셀")]
        [SerializeField][Min(0)] private int _buffCellRange = 3;

        public int BuffCellRange => _buffCellRange;
        public abstract Color BuffCellColor { get; }

        public override void Spawned()
        {
            base.Spawned();
            HideLegacyRangeObject();
            RefreshBuffCellSource();
        }

        public override void Render()
        {
            base.Render();
            RefreshBuffCellSource();
        }

        protected override void TowerDespawned()
        {
            if (TryGetGrid(out InfiniteGrid gridManager))
            {
                gridManager.RemoveBuffSource(GetInstanceID());
            }

            base.TowerDespawned();
        }

        protected void RefreshBuffCellSource()
        {
            if (!TryGetGrid(out InfiniteGrid gridManager))
            {
                return;
            }

            Vector2Int centerIndex = gridManager.GetCellIndexFromWorldPosition(transform.position);
            gridManager.RegisterOrUpdateBuffSource(GetInstanceID(), centerIndex, _buffCellRange, BuffCellColor);
        }

        protected bool IsInsideBuffCells(Transform receiverTransform)
        {
            if (receiverTransform == null || !TryGetGrid(out InfiniteGrid gridManager))
            {
                return false;
            }

            return gridManager.IsWorldPositionInBuffSource(GetInstanceID(), receiverTransform.position);
        }

        private void HideLegacyRangeObject()
        {
            Transform legacyRange = transform.Find("Range");
            if (legacyRange != null)
            {
                legacyRange.gameObject.SetActive(false);
            }
        }
    }
}
