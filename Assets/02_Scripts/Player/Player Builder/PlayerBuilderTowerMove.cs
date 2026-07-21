using Fusion;
using Dev.Network;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class PlayerBuilderTowerMove : NetworkBehaviour
    {
        [Header("Move")]
        [SerializeField, Min(0)] private int _mineralCostPerTower = 25;
        [Networked] private NetworkBool IsMoveAvailable { get; set; }

        private List<TowerGhost> _ghosts;
        private Dictionary<TowerGhost, Tower> _ghostToTowerDic;
        private Dictionary<Tower, Vector3> _towerToVecDic;
        private readonly HashSet<Vector2Int> _previewValidIndices = new();
        private readonly HashSet<Vector2Int> _previewBlockedIndices = new();
        private readonly TowerMoveAvailabilityPolicy _moveAvailabilityPolicy = new();
        private readonly TowerMoveCostPolicy _moveCostPolicy = new();

        private PlayerBuilderTowerSystem _towerSystem;
        private InfiniteGrid _gridManager;

        public bool HasMoveTargets => _ghostToTowerDic != null && _ghostToTowerDic.Count > 0;
        public InfiniteGrid GridManager => _gridManager;

        public bool CanMoveInCurrentPhase()
        {
            return HasStateAuthority
                ? _moveAvailabilityPolicy.IsMaintenanceActive()
                : IsMoveAvailable;
        }

        public override void FixedUpdateNetwork()
        {
            if (HasStateAuthority)
            {
                IsMoveAvailable = _moveAvailabilityPolicy.IsMaintenanceActive();
            }
        }

        public void InitTowerMove(PlayerBuilderTowerSystem towerSystem)
        {
            _towerSystem = towerSystem;
        }

        public void InitializeDependencies(
            TimeSystem timeSystem,
            ResourceSystem resourceSystem,
            InfiniteGrid gridManager)
        {
            _moveAvailabilityPolicy.Initialize(timeSystem);
            _moveCostPolicy.Initialize(resourceSystem);
            _gridManager = gridManager;
        }

        public void TowerMoveSet(HashSet<Tower> towers)
        {
            TowerGhostInstantiate(towers);
            Vector3 pivot = GetPivot(towers);
            TowerDistanceVectorCalc(towers, pivot);
            PruneInvalidTowers();
        }

        public bool TowerGhostSnapShot(Vector3 mousePos)
        {
            PruneInvalidTowers();
            if (!HasMoveTargets)
            {
                _gridManager?.ClearBuildRangePreview();
                return false;
            }

            bool canMoveAll = true;
            _previewValidIndices.Clear();
            _previewBlockedIndices.Clear();
            var gridManager = _gridManager;
            if (gridManager == null)
                return false;
            var selectedOccupied = CollectSelectedOccupiedIndices();
            var claimedTargets = new Dictionary<Vector2Int, Tower>();
            var conflictedTowers = new HashSet<Tower>();
            var plannedMoves = new List<PlannedTowerMove>(_ghosts.Count);

            foreach (var ghost in _ghosts)
            {
                if (ghost == null || !_ghostToTowerDic.TryGetValue(ghost, out Tower targetTower) || targetTower == null)
                    continue;

                if (!_towerToVecDic.TryGetValue(targetTower, out Vector3 diff))
                    continue;

                Vector3 targetPos = mousePos + diff;

                Vector2Int snapshotIndex = gridManager.GetCellIndexFromWorldPosition(targetPos);
                Vector3 snapshotPos = gridManager.GetCellCenterPositionFromCellIndex(snapshotIndex);
                Vector2Int currentIndex = gridManager.GetCellIndexFromWorldPosition(targetTower.transform.position);
                List<Vector2Int> targetIndices = gridManager.GetCellIndicesInRange(snapshotIndex, targetTower.BuildRange, includeCenter: true);

                ghost.transform.position = snapshotPos;
                ghost.EnableTower();

                if (targetTower is CellBuffSupportTower buffTower)
                {
                    gridManager.RegisterOrUpdateBuffPreviewSource(
                        ghost.GetInstanceID(),
                        snapshotIndex,
                        buffTower.BuffCellRange,
                        buffTower.BuffCellColor);
                }

                plannedMoves.Add(new PlannedTowerMove
                {
                    Tower = targetTower,
                    Ghost = ghost,
                    CurrentCenter = currentIndex,
                    TargetCenter = snapshotIndex,
                    TargetPosition = snapshotPos,
                    TargetIndices = targetIndices
                });
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                for (int j = 0; j < move.TargetIndices.Count; j++)
                {
                    Vector2Int idx = move.TargetIndices[j];
                    if (claimedTargets.TryGetValue(idx, out Tower owner))
                    {
                        if (owner != move.Tower)
                        {
                            conflictedTowers.Add(owner);
                            conflictedTowers.Add(move.Tower);
                        }
                    }
                    else
                    {
                        claimedTargets[idx] = move.Tower;
                    }
                }
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                bool isSamePosition = move.TargetCenter == move.CurrentCenter;
                bool canPlace = gridManager.CanPlaceInRange(
                    move.TargetCenter,
                    move.Tower.BuildRange,
                    requireEmpty: true,
                    requireTerritory: true,
                    ignoreOccupiedIndices: selectedOccupied);
                bool conflicted = conflictedTowers.Contains(move.Tower);
                bool canMoveThisTower = !isSamePosition && canPlace && !conflicted;

                AddPreviewIndices(move.TargetIndices, canMoveThisTower, _previewValidIndices, _previewBlockedIndices);

                if (canMoveThisTower)
                {
                    move.Ghost.EnableTower();
                }
                else
                {
                    move.Ghost.DisableTower();
                    canMoveAll = false;
                }
            }

            gridManager.SetBuildRangePreview(_previewValidIndices, _previewBlockedIndices);
            return canMoveAll;
        }

        public void TowerMove()
        {
            if (!CanMoveInCurrentPhase())
                return;

            PruneInvalidTowers();
            if (!HasMoveTargets)
                return;

            var gridManager = _gridManager;
            if (gridManager == null) return;

            var selectedOccupied = CollectSelectedOccupiedIndices();
            var claimedTargets = new Dictionary<Vector2Int, Tower>();
            var conflictedTowers = new HashSet<Tower>();
            var plannedMoves = new List<PlannedTowerMove>(_ghosts.Count);

            foreach (var ghost in _ghosts)
            {
                if (ghost == null || !_ghostToTowerDic.TryGetValue(ghost, out Tower tower) || tower == null)
                    continue;

                Vector2Int currentIndex = gridManager.GetCellIndexFromWorldPosition(tower.transform.position);
                Vector2Int targetIndex = gridManager.GetCellIndexFromWorldPosition(ghost.transform.position);
                List<Vector2Int> targetIndices = gridManager.GetCellIndicesInRange(targetIndex, tower.BuildRange, includeCenter: true);

                plannedMoves.Add(new PlannedTowerMove
                {
                    Tower = tower,
                    Ghost = ghost,
                    CurrentCenter = currentIndex,
                    TargetCenter = targetIndex,
                    TargetPosition = ghost.transform.position,
                    TargetIndices = targetIndices
                });
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                for (int j = 0; j < move.TargetIndices.Count; j++)
                {
                    Vector2Int idx = move.TargetIndices[j];
                    if (claimedTargets.TryGetValue(idx, out Tower owner))
                    {
                        if (owner != move.Tower)
                        {
                            conflictedTowers.Add(owner);
                            conflictedTowers.Add(move.Tower);
                        }
                    }
                    else
                    {
                        claimedTargets[idx] = move.Tower;
                    }
                }
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                bool isSamePosition = move.TargetCenter == move.CurrentCenter;
                bool canPlace = gridManager.CanPlaceInRange(
                    move.TargetCenter,
                    move.Tower.BuildRange,
                    requireEmpty: true,
                    requireTerritory: true,
                    ignoreOccupiedIndices: selectedOccupied);
                bool conflicted = conflictedTowers.Contains(move.Tower);
                if (isSamePosition || !canPlace || conflicted)
                {
                    return;
                }
            }

            int arrayCount = plannedMoves.Count;
            int currentCount = 0;
            NetworkId[] netId = new NetworkId[arrayCount];
            Vector3[] positions = new Vector3[arrayCount];

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                Tower tower = move.Tower;

                netId[currentCount] = tower.Object.Id;
                positions[currentCount++] = move.TargetPosition;
            }

            RPC_TowerMove(netId, positions);
        }

        public void TowerMoveClear()
        {
            _gridManager?.ClearBuildRangePreview();
            _gridManager?.ClearBuffPreviewSources();

            if (_ghosts != null)
            {
                for (int i = 0; i < _ghosts.Count; i++)
                {
                    if (_ghosts[i] != null)
                    {
                        Destroy(_ghosts[i].gameObject);
                    }
                }

                _ghosts.Clear();
            }

            _ghostToTowerDic?.Clear();
            _towerToVecDic?.Clear();
            _previewValidIndices.Clear();
            _previewBlockedIndices.Clear();
        }

        public void RemoveTower(Tower tower)
        {
            if (tower == null)
                return;

            if (_towerToVecDic != null)
            {
                _towerToVecDic.Remove(tower);
            }

            if (_ghostToTowerDic == null || _ghostToTowerDic.Count == 0)
                return;

            TowerGhost removeGhost = null;
            foreach (var pair in _ghostToTowerDic)
            {
                if (pair.Value == tower)
                {
                    removeGhost = pair.Key;
                    break;
                }
            }

            if (removeGhost is null)
                return;

            _ghostToTowerDic.Remove(removeGhost);
            _ghosts?.Remove(removeGhost);

            if (removeGhost is not null)
            {
                _gridManager?.RemoveBuffPreviewSource(removeGhost.GetInstanceID());
                Destroy(removeGhost.gameObject);
            }

            if (!HasMoveTargets)
            {
                _gridManager?.ClearBuildRangePreview();
            }
        }

        public void PruneInvalidTowers()
        {
            if (_ghostToTowerDic == null || _ghostToTowerDic.Count == 0)
                return;

            var removeGhosts = new List<TowerGhost>();
            foreach (var pair in _ghostToTowerDic)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    removeGhosts.Add(pair.Key);
                }
            }

            for (int i = 0; i < removeGhosts.Count; i++)
            {
                TowerGhost ghost = removeGhosts[i];
                if (ghost is not null && _ghostToTowerDic.TryGetValue(ghost, out Tower tower) && tower != null)
                {
                    _towerToVecDic?.Remove(tower);
                }

                if (ghost is not null)
                {
                    _gridManager?.RemoveBuffPreviewSource(ghost.GetInstanceID());
                    _ghostToTowerDic.Remove(ghost);
                    _ghosts?.Remove(ghost);
                    Destroy(ghost.gameObject);
                }
            }
        }

        private void TowerGhostInstantiate(HashSet<Tower> towers)
        {
            _ghosts = new List<TowerGhost>();
            _ghostToTowerDic = new Dictionary<TowerGhost, Tower>();

            foreach (var tower in towers)
            {
                if (tower == null)
                    continue;

                if (!tower.HasCapability(TowerCapability.Move))
                    continue;

                var ghost = Instantiate(tower.Ghost);
                if (ghost == null)
                    continue;

                ghost.InitializePreview();

                _ghosts.Add(ghost);
                _ghostToTowerDic.Add(ghost, tower);
            }
        }

        private Vector3 GetPivot(HashSet<Tower> towers)
        {
            if (towers == null || towers.Count == 0)
                return Vector3.zero;

            bool inited = false;
            Vector3 min = Vector3.zero;
            Vector3 max = Vector3.zero;

            foreach (var t in towers)
            {
                if (!t) continue;

                Vector3 p = t.transform.position;
                if (!inited)
                {
                    min = p;
                    max = p;
                    inited = true;
                }
                else
                {
                    min = Vector3.Min(min, p);
                    max = Vector3.Max(max, p);
                }
            }

            return inited ? (min + max) * 0.5f : Vector3.zero;
        }

        private void TowerDistanceVectorCalc(HashSet<Tower> towers, Vector3 pivot)
        {
            _towerToVecDic = new Dictionary<Tower, Vector3>();

            foreach (var tower in towers)
            {
                if (tower == null)
                    continue;

                Vector3 p = tower.transform.position;
                Vector3 diff = p - pivot;
                _towerToVecDic.Add(tower, diff);
            }
        }

        private HashSet<Vector2Int> CollectSelectedOccupiedIndices()
        {
            var occupied = new HashSet<Vector2Int>();
            if (_ghostToTowerDic == null)
                return occupied;

            foreach (var pair in _ghostToTowerDic)
            {
                Tower tower = pair.Value;
                if (tower == null) continue;

                var indices = tower.OccupiedIndices;
                for (int i = 0; i < indices.Count; i++)
                {
                    occupied.Add(indices[i]);
                }
            }

            return occupied;
        }

        private static void AddPreviewIndices(
            List<Vector2Int> source,
            bool isValid,
            HashSet<Vector2Int> validTarget,
            HashSet<Vector2Int> blockedTarget)
        {
            if (source == null || validTarget == null || blockedTarget == null) return;

            for (int i = 0; i < source.Count; i++)
            {
                Vector2Int index = source[i];
                if (isValid)
                {
                    if (!blockedTarget.Contains(index))
                    {
                        validTarget.Add(index);
                    }
                }
                else
                {
                    blockedTarget.Add(index);
                    validTarget.Remove(index);
                }
            }
        }

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_TowerMove(NetworkId[] netId, Vector3[] positions)
        {
            if (netId == null || positions == null || netId.Length != positions.Length)
                return;

            if (!CanMoveInCurrentPhase())
                return;

            var gridManager = _gridManager;
            if (gridManager == null)
                return;

            var claimedTargets = new Dictionary<Vector2Int, Tower>();
            var conflictedTowers = new HashSet<Tower>();
            var plannedMoves = new List<PlannedTowerMove>(netId.Length);
            var plannedTowers = new HashSet<Tower>();

            for (int i = 0; i < netId.Length; i++)
            {
                if (!Runner.TryFindObject(netId[i], out NetworkObject obj))
                    continue;

                if (!obj.TryGetComponent(out Tower tower) || tower == null)
                    continue;

                if (!tower.HasCapability(TowerCapability.Move))
                    return;

                if (!plannedTowers.Add(tower))
                    return;

                if (!tower.Object.TryGetComponent(out NetworkTransform networkTransform))
                    return;

                Vector2Int currentCenter = tower.HasGridOccupation
                    ? tower.BuiltIndex
                    : gridManager.GetCellIndexFromWorldPosition(tower.transform.position);
                Vector2Int targetCenter = gridManager.GetCellIndexFromWorldPosition(positions[i]);

                plannedMoves.Add(new PlannedTowerMove
                {
                    Tower = tower,
                    NetworkTransform = networkTransform,
                    CurrentCenter = currentCenter,
                    TargetCenter = targetCenter,
                    TargetPosition = gridManager.GetCellCenterPositionFromCellIndex(targetCenter),
                    TargetIndices = gridManager.GetCellIndicesInRange(targetCenter, tower.BuildRange, includeCenter: true)
                });
            }

            if (plannedMoves.Count == 0)
                return;

            var movingTowers = new List<Tower>(plannedMoves.Count);
            for (int i = 0; i < plannedMoves.Count; i++)
            {
                movingTowers.Add(plannedMoves[i].Tower);
            }

            Cost moveCost = _moveCostPolicy.CalculateCost(
                movingTowers,
                gridManager,
                _mineralCostPerTower);
            if (!_moveCostPolicy.CanAfford(moveCost))
                return;

            var selectedOccupied = CollectOccupiedIndices(plannedMoves);

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                for (int j = 0; j < move.TargetIndices.Count; j++)
                {
                    Vector2Int idx = move.TargetIndices[j];
                    if (claimedTargets.TryGetValue(idx, out Tower owner))
                    {
                        if (owner != move.Tower)
                        {
                            conflictedTowers.Add(owner);
                            conflictedTowers.Add(move.Tower);
                        }
                    }
                    else
                    {
                        claimedTargets[idx] = move.Tower;
                    }
                }
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                bool isSamePosition = move.TargetCenter == move.CurrentCenter;
                bool canPlace = gridManager.CanPlaceInRange(
                    move.TargetCenter,
                    move.Tower.BuildRange,
                    requireEmpty: true,
                    requireTerritory: true,
                    ignoreOccupiedIndices: selectedOccupied);
                bool conflicted = conflictedTowers.Contains(move.Tower);

                if (isSamePosition || !canPlace || conflicted)
                {
                    return;
                }
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                plannedMoves[i].Tower.ReleaseGridOccupation();
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                bool occupied = move.Tower.TryOccupyAtIndex(move.TargetCenter, requireTerritory: true, requireEmpty: true);
                if (!occupied)
                {
                    RollbackTowerOccupation(plannedMoves);
                    return;
                }
            }

            if (!_moveCostPolicy.TryPay(moveCost))
            {
                RollbackTowerOccupation(plannedMoves);
                return;
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var move = plannedMoves[i];
                move.NetworkTransform.Teleport(move.TargetPosition);
                gridManager.TryConsumeFreeTrackRelocation(move.Tower);
                TowerMoveProcess(move.Tower);
            }
        }

        private static void RollbackTowerOccupation(List<PlannedTowerMove> plannedMoves)
        {
            for (int i = 0; i < plannedMoves.Count; i++)
            {
                plannedMoves[i].Tower.ReleaseGridOccupation();
            }

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var rollback = plannedMoves[i];
                rollback.Tower.TryOccupyAtIndex(
                    rollback.CurrentCenter,
                    requireTerritory: true,
                    requireEmpty: true);
            }
        }

        private static HashSet<Vector2Int> CollectOccupiedIndices(List<PlannedTowerMove> plannedMoves)
        {
            var occupied = new HashSet<Vector2Int>();
            if (plannedMoves == null)
                return occupied;

            for (int i = 0; i < plannedMoves.Count; i++)
            {
                var indices = plannedMoves[i].Tower.OccupiedIndices;
                for (int j = 0; j < indices.Count; j++)
                {
                    occupied.Add(indices[j]);
                }
            }

            return occupied;
        }

        private void TowerMoveProcess(Tower tower)
        {
            string towerId = tower.TowerID;
            if (towerId == TowerIDContainer.TELEPORT_TOWER_ID)
            {
                TeleportTowerMoveProcess(tower);
            }
        }

        private void TeleportTowerMoveProcess(Tower tower)
        {
            if (tower.TryGetComponent(out TeleportTower teleportTower))
            {
                teleportTower.SetPairCoolDown();
            }
        }
    }
}
