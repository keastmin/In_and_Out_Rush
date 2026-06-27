using Fusion;
using Dev.Network;
using System;
using System.Collections.Generic;

namespace KIM.Dev
{
    public class PlayerBuilderTowerSell : NetworkBehaviour
    {
        private PlayerBuilderTowerSystem _towerSystem;

        #region API

        public void InitTowerSell(PlayerBuilderTowerSystem towerSystem)
        {
            _towerSystem = towerSystem;
        }

        public void SellTower(HashSet<Tower> towers, PlayerBuilder builder)
        {
            if (towers == null || towers.Count == 0) return;

            NetworkId[] ids = new NetworkId[towers.Count];
            int n = 0;

            foreach (var t in towers)
            {
                if (t == null) continue;
                if (!t.HasCapability(TowerCapability.Sell)) continue;

                NetworkObject no = t.Object;
                if (no == null) continue;

                ids[n++] = no.Id;
            }

            if (n == 0) return;

            if (n != ids.Length)
            {
                Array.Resize(ref ids, n);
            }

            towers.Clear();
            RPC_SellTower(ids);
        }

        #endregion

        #region Core

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SellTower(NetworkId[] towerIds)
        {
            int mineralRefund = 0;
            int gasRefund = 0;

            foreach (var id in towerIds)
            {
                if (!Runner.TryFindObject(id, out NetworkObject obj))
                    continue;

                if (!obj.TryGetComponent(out Tower tower) ||
                    !tower.HasCapability(TowerCapability.Sell))
                {
                    continue;
                }

                mineralRefund += (int)(tower.Cost.Mineral * 0.5f);
                gasRefund += (int)(tower.Cost.Gas * 0.5f);

                if (tower.IsCenter && StageBootstrapper.Instance != null && StageBootstrapper.Instance.PlayerBuilder != null)
                {
                    int nextCenterCount = Math.Max(0, StageBootstrapper.Instance.PlayerBuilder.CenterTowerCount - 1);
                    StageBootstrapper.Instance.PlayerBuilder.SetCenterTowerCount(nextCenterCount);
                }

                tower.ReleaseGridOccupation();

                Runner.Despawn(obj);
            }

            ResourceSystem.Instance.Mineral += mineralRefund;
            ResourceSystem.Instance.Gas += gasRefund;
        }

        #endregion
    }
}
