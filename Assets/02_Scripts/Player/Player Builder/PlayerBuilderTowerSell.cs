using Fusion;
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
            Cost[] costs = new Cost[towers.Count];
            int n = 0;

            foreach (var t in towers)
            {
                if (t == null) continue;

                NetworkObject no = t.Object;
                if (no == null) continue;

                costs[n] = t.Cost;
                ids[n++] = no.Id;

                if (t.IsCenter)
                {
                    if (t.TryGetComponent(out CenterTower centerTower))
                    {
                        builder.SetCenterTowerCount(builder.CenterTowerCount - 1);
                    }
                }
            }

            if (n == 0) return;

            if (n != ids.Length)
            {
                Array.Resize(ref ids, n);
                Array.Resize(ref costs, n);
            }

            towers.Clear();
            RPC_SellTower(ids, costs);
        }

        #endregion

        #region Core

        [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
        private void RPC_SellTower(NetworkId[] towerIds, Cost[] costs)
        {
            foreach (var id in towerIds)
            {
                if (!Runner.TryFindObject(id, out NetworkObject obj))
                    continue;

                if (obj.TryGetComponent(out Tower tower))
                {
                    tower.ReleaseGridOccupation();
                }

                Runner.Despawn(obj);
            }

            foreach (var cost in costs)
            {
                ResourceSystem.Instance.Mineral += (int)(cost.Mineral * 0.5f);
                ResourceSystem.Instance.Gas += (int)(cost.Gas * 0.5f);
            }
        }

        #endregion
    }
}