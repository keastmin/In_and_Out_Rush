using System;
using Fusion;
using KIM.Dev;
using UnityEngine;

namespace ProjectIO.RunnerSupply
{
    [DisallowMultipleComponent]
    public sealed class RunnerSupplyNetwork : NetworkBehaviour
    {
        [SerializeField] private RunnerSupplyCatalog _catalog;
        [Networked, Capacity(RunnerSupplyRules.Capacity), OnChangedRender(nameof(PublishStateChanged))]
        private NetworkLinkedList<int> PendingSupplies => default;
        [Networked, OnChangedRender(nameof(PublishStateChanged))] private int Centers { get; set; }
        [Networked] private int Revision { get; set; }

        private ResourceSystem _resources;
        private InfiniteGrid _grid;
        private PlayerBuilder _builder;
        private bool _spawned;
        private TickTimer _centerRefreshTimer;
        public event Action Changed;
        public event Action<int, RunnerSupplyResult> PurchaseCompleted;

        public RunnerSupplyCatalog Catalog => _catalog;
        public bool IsReady => _spawned && Object != null && Object.IsValid && _catalog != null &&
            _resources != null && _resources.Object != null && _resources.Object.IsValid &&
            _builder != null && _builder.Object != null && _builder.Object.IsValid;
        public bool IsPurchasePending { get; private set; }
        public int Count => IsReady ? PendingSupplies.Count : 0;
        public int Mineral => IsReady ? _resources.Mineral : 0;
        public int Gas => IsReady ? _resources.Gas : 0;

        public void Initialize(ResourceSystem resources, InfiniteGrid grid, PlayerBuilder builder)
        {
            _resources = resources;
            _grid = grid;
            _builder = builder;
            PublishStateChanged();
        }

        public override void Spawned()
        {
            _spawned = true;
            PublishStateChanged();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            _spawned = false;
            IsPurchasePending = false;
            PublishStateChanged();
        }

        public override void FixedUpdateNetwork()
        {
            if (!IsReady || !HasStateAuthority || !_centerRefreshTimer.ExpiredOrNotRunning(Runner)) return;
            RefreshCenters();
            _centerRefreshTimer = TickTimer.CreateFromSeconds(Runner, 0.25f);
        }

        private void RefreshCenters()
        {
            int mask = 0;
            if (_grid != null && _grid.HostOnlyReadTowers != null)
                foreach (Tower tower in _grid.HostOnlyReadTowers)
                    if (tower != null && tower.IsCenter && tower.Object != null && tower.Object.IsValid)
                        mask |= RunnerSupplyRules.CenterBit(tower.PropertyType);
            if (Centers == mask) return;
            Centers = mask;
            PublishStateChanged();
        }

        public RunnerSupplyResult Evaluate(int productId)
        {
            if (!IsReady) return RunnerSupplyResult.NotReady;
            if (!_catalog.TryGet(productId, out RunnerSupplyDefinition product)) return RunnerSupplyResult.InvalidProduct;
            return RunnerSupplyRules.Evaluate(product, Count, Mineral, Gas, Centers);
        }

        public RunnerSupplyResult RequestPurchase(int productId)
        {
            if (!IsReady) return RunnerSupplyResult.NotReady;
            if (!_builder.HasInputAuthority) return RunnerSupplyResult.NotBuilder;
            if (IsPurchasePending) return RunnerSupplyResult.Pending;
            RunnerSupplyResult result = Evaluate(productId);
            if (result != RunnerSupplyResult.Success) return result;
            IsPurchasePending = true;
            RPC_RequestPurchase(productId, Revision, _builder.Object.Id);
            return RunnerSupplyResult.Pending;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestPurchase(int productId, int expectedRevision, NetworkId builderId, RpcInfo info = default)
        {
            PlayerRef requester = info.Source == PlayerRef.None ? Runner.LocalPlayer : info.Source;
            RunnerSupplyResult result = TryPurchaseOnAuthority(productId, expectedRevision, builderId, requester);
            RPC_PurchaseResult(requester, productId, result);
        }

        private RunnerSupplyResult TryPurchaseOnAuthority(int productId, int expectedRevision, NetworkId builderId, PlayerRef requester)
        {
            if (!IsReady || !HasStateAuthority) return RunnerSupplyResult.NotReady;
            if (_builder.Object.Id != builderId || _builder.Object.InputAuthority != requester)
                return RunnerSupplyResult.NotBuilder;
            if (Revision != expectedRevision) return RunnerSupplyResult.StateChanged;
            RefreshCenters();
            RunnerSupplyResult result = Evaluate(productId);
            if (result != RunnerSupplyResult.Success) return result;
            _catalog.TryGet(productId, out RunnerSupplyDefinition product);
            if (!_resources.TryDeductCost(product.Cost)) return RunnerSupplyResult.InsufficientResources;
            PendingSupplies.Add(productId);
            Revision++;
            PublishStateChanged();
            return RunnerSupplyResult.Success;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PurchaseResult([RpcTarget] PlayerRef requester, int productId, RunnerSupplyResult result)
        {
            IsPurchasePending = false;
            PurchaseCompleted?.Invoke(productId, result);
            PublishStateChanged();
        }

        public int[] Snapshot()
        {
            int[] result = new int[Count];
            for (int i = 0; i < result.Length; i++) result[i] = PendingSupplies[i];
            return result;
        }

        public bool MatchesPending(int[] expected) => IsReady && RunnerSupplyRules.MatchesPrefix(Snapshot(), expected);

        public bool ConsumeLoadedSupplies(int[] expected)
        {
            if (!HasStateAuthority || !MatchesPending(expected)) return false;
            for (int i = 0; i < expected.Length; i++) PendingSupplies.Remove(PendingSupplies[0]);
            Revision++;
            PublishStateChanged();
            return true;
        }

        private void PublishStateChanged() => Changed?.Invoke();
    }
}
