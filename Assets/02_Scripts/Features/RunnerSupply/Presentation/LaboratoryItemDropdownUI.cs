using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectIO.RunnerSupply
{
    public sealed class LaboratoryItemDropdownUI : MonoBehaviour
    {
        [SerializeField] private RunnerSupplyCatalog _catalog;
        [SerializeField] private TMP_Dropdown _dropdown;
        [SerializeField] private Image _selectedIcon;
        [SerializeField] private TMP_Text _description;
        [SerializeField] private TMP_Text _price;
        [SerializeField] private TMP_Text _availability;
        [SerializeField] private Button _purchaseButton;
        private readonly List<RunnerSupplyDefinition> _items = new();
        private RunnerSupplyNetwork _network;
        public int SelectedId => _items.Count > 0 ? _items[Mathf.Clamp(_dropdown.value, 0, _items.Count - 1)].Id : 0;

        private void Awake() => Populate();

        public void Populate()
        {
            if (_catalog == null || _dropdown == null) return;
            int selected = _dropdown.value;
            _items.Clear();
            var options = new List<TMP_Dropdown.OptionData>();
            foreach (RunnerSupplyDefinition product in _catalog.Products)
            {
                if (product == null || !product.IsItem) continue;
                _items.Add(product);
                options.Add(new TMP_Dropdown.OptionData($"{product.Name}   {Price(product)}", product.Icon, Color.white));
            }
            _dropdown.ClearOptions();
            _dropdown.AddOptions(options);
            _dropdown.SetValueWithoutNotify(Mathf.Clamp(selected, 0, Mathf.Max(0, _items.Count - 1)));
            _dropdown.RefreshShownValue();
            Refresh(_network);
        }

        public void OnSelectionChanged(int index) => Refresh(_network);

        public void Refresh(RunnerSupplyNetwork network)
        {
            _network = network;
            if (_items.Count == 0) return;
            RunnerSupplyDefinition product = _items[Mathf.Clamp(_dropdown.value, 0, _items.Count - 1)];
            if (_selectedIcon != null) _selectedIcon.sprite = product.Icon;
            if (_description != null) _description.text = product.Description;
            if (_price != null) _price.text = Price(product);
            RunnerSupplyResult result = network != null ? network.Evaluate(product.Id) : RunnerSupplyResult.NotReady;
            if (_purchaseButton != null) _purchaseButton.interactable = result == RunnerSupplyResult.Success && !network.IsPurchasePending;
            if (_availability != null)
            {
                _availability.text = result == RunnerSupplyResult.Success ? "구매 시 보급 대기열에 1개 추가" : RunnerSupplyRules.Message(result, product);
                _availability.color = result == RunnerSupplyResult.Success ? new Color(0.56f, 0.77f, 0.78f) : new Color(1f, 0.74f, 0.38f);
            }
        }

        public static string Price(RunnerSupplyDefinition product)
        {
            if (product.Cost.Mineral == 0) return $"가스 {product.Cost.Gas}";
            if (product.Cost.Gas == 0) return $"광물 {product.Cost.Mineral}";
            return $"광물 {product.Cost.Mineral}  ·  가스 {product.Cost.Gas}";
        }
    }
}
