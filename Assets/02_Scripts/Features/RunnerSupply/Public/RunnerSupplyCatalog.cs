using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectIO.RunnerSupply
{
    [CreateAssetMenu(menuName = "ProjectIO/Runner Supply Catalog")]
    public sealed class RunnerSupplyCatalog : ScriptableObject
    {
        [Tooltip("Item prices: planning workbook Item!C5:D9. Sprite references reuse Runner UI.")]
        [SerializeField] private RunnerSupplyDefinition[] _products = Array.Empty<RunnerSupplyDefinition>();
        public IReadOnlyList<RunnerSupplyDefinition> Products => _products;

        public bool TryGet(int id, out RunnerSupplyDefinition product)
        {
            foreach (RunnerSupplyDefinition candidate in _products)
            {
                if (candidate != null && candidate.Id == id)
                {
                    product = candidate;
                    return true;
                }
            }
            product = null;
            return false;
        }
    }
}
