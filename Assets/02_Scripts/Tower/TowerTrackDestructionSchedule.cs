using System;
using System.Collections.Generic;

namespace KIM.Dev
{
    public sealed class TowerTrackDestructionSchedule
    {
        private readonly HashSet<Tower> _pendingDestructionTowers = new();
        private readonly HashSet<Tower> _freeRelocationTowers = new();

        public void Refresh(IEnumerable<Tower> towers, Predicate<Tower> isBlockedByTrack)
        {
            if (towers == null || isBlockedByTrack == null)
                return;

            var currentlyBlockedTowers = new HashSet<Tower>();
            foreach (Tower tower in towers)
            {
                if (tower == null || !isBlockedByTrack(tower))
                    continue;

                currentlyBlockedTowers.Add(tower);
                if (_pendingDestructionTowers.Add(tower) && tower.HasCapability(TowerCapability.Move))
                    _freeRelocationTowers.Add(tower);
            }

            _pendingDestructionTowers.RemoveWhere(
                tower => tower == null || !currentlyBlockedTowers.Contains(tower));
            _freeRelocationTowers.RemoveWhere(
                tower => tower == null || !_pendingDestructionTowers.Contains(tower));
        }

        public bool IsPendingDestruction(Tower tower)
        {
            return tower != null && _pendingDestructionTowers.Contains(tower);
        }

        public bool HasFreeRelocation(Tower tower)
        {
            return tower != null && _freeRelocationTowers.Contains(tower);
        }

        public bool TryConsumeFreeRelocation(Tower tower)
        {
            if (tower == null || !_pendingDestructionTowers.Contains(tower))
                return false;

            if (!_freeRelocationTowers.Remove(tower))
                return false;

            _pendingDestructionTowers.Remove(tower);
            return true;
        }

        public void Remove(Tower tower)
        {
            if (tower == null)
                return;

            _pendingDestructionTowers.Remove(tower);
            _freeRelocationTowers.Remove(tower);
        }

        public void Clear()
        {
            _pendingDestructionTowers.Clear();
            _freeRelocationTowers.Clear();
        }
    }
}
