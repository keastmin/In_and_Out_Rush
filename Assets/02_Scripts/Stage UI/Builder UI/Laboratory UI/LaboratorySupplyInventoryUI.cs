using System;
using ProjectIO.RunnerSupply;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class LaboratorySupplyInventoryUI : MonoBehaviour
    {
        [SerializeField] private LaboratorySupplySlotUI[] _slots = Array.Empty<LaboratorySupplySlotUI>();
        [SerializeField] private TMP_Text _count;
        private RunnerSupplyNetwork _network;

        public void Initialize(RunnerSupplyNetwork network)
        {
            Unbind();
            _network = network;
            if (isActiveAndEnabled) Bind();
            RefreshFromPendingSupplies();
        }

        private void OnEnable() { Bind(); RefreshFromPendingSupplies(); }
        private void OnDisable() => Unbind();
        private void Bind()
        {
            if (_network == null) return;
            _network.Changed -= RefreshFromPendingSupplies;
            _network.Changed += RefreshFromPendingSupplies;
        }
        private void Unbind()
        {
            if (_network != null) _network.Changed -= RefreshFromPendingSupplies;
        }

        public void RefreshFromPendingSupplies() =>
            DisplaySnapshot(_network != null ? _network.Catalog : null,
                _network != null ? _network.Snapshot() : Array.Empty<int>());

        public void DisplaySnapshot(RunnerSupplyCatalog catalog, int[] products)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                RunnerSupplyDefinition product = null;
                if (i < products.Length && catalog != null) catalog.TryGet(products[i], out product);
                _slots[i]?.Display(i, product);
            }
            if (_count != null) _count.text = $"{products.Length} / {RunnerSupplyRules.Capacity}";
        }
    }
}
