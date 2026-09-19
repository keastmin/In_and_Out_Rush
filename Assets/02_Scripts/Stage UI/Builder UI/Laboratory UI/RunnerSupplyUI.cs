using ProjectIO.RunnerSupply;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public sealed class RunnerSupplyUI : MonoBehaviour
    {
        [Header("Item selection")]
        [SerializeField] private LaboratoryItemDropdownUI _items;
        [Header("Existing supply actions")]
        [SerializeField] private Button _skillButton;
        [SerializeField] private Button _weaponButton;
        [SerializeField] private TMP_Text _skillPrice;
        [SerializeField] private TMP_Text _weaponPrice;
        [Header("Resources and feedback")]
        [SerializeField] private TMP_Text _mineral;
        [SerializeField] private TMP_Text _gas;
        [SerializeField] private TMP_Text _feedback;
        private RunnerSupplyNetwork _network;
        private float _nextRefresh;

        public void Initialize(RunnerSupplyNetwork network)
        {
            Unbind();
            _network = network;
            if (isActiveAndEnabled) Bind();
            Refresh();
        }

        public void InitializeRunnerSupplyUI() => Refresh();
        private void OnEnable() { Bind(); Refresh(); }
        private void OnDisable() => Unbind();
        private void Bind()
        {
            if (_network == null) return;
            _network.Changed -= Refresh;
            _network.PurchaseCompleted -= HandlePurchaseCompleted;
            _network.Changed += Refresh;
            _network.PurchaseCompleted += HandlePurchaseCompleted;
        }
        private void Unbind()
        {
            if (_network == null) return;
            _network.Changed -= Refresh;
            _network.PurchaseCompleted -= HandlePurchaseCompleted;
        }
        private void Update()
        {
            // Resource balances can also change through upgrades while this screen is open.
            if (Time.unscaledTime < _nextRefresh) return;
            _nextRefresh = Time.unscaledTime + 0.15f;
            Refresh();
        }

        public void OnClickSkillSupplyButton() => Purchase(RunnerSupplyRules.SkillId);
        public void OnClickWeaponSupplyButton() => Purchase(RunnerSupplyRules.WeaponId);
        public void OnClickItemSupplyButton() => Purchase(_items != null ? _items.SelectedId : 0);

        private void Purchase(int id)
        {
            if (_feedback != null) _feedback.text = RunnerSupplyRules.Message(RunnerSupplyResult.Pending);
            RunnerSupplyResult result = _network != null ? _network.RequestPurchase(id) : RunnerSupplyResult.NotReady;
            // Host-local RPC can return synchronously. Do not overwrite its success with Pending.
            if (result != RunnerSupplyResult.Pending) HandlePurchaseCompleted(id, result);
            Refresh();
        }

        private void HandlePurchaseCompleted(int id, RunnerSupplyResult result)
        {
            RunnerSupplyDefinition product = null;
            if (_network != null && _network.Catalog != null) _network.Catalog.TryGet(id, out product);
            if (_feedback != null)
            {
                _feedback.text = result == RunnerSupplyResult.Success && product != null
                    ? $"{product.Name} 1개를 보급 대기열에 추가했습니다."
                    : RunnerSupplyRules.Message(result, product);
                _feedback.color = result == RunnerSupplyResult.Success
                    ? new Color(0.48f, 0.88f, 0.74f) : new Color(1f, 0.74f, 0.38f);
            }
            Refresh();
        }

        public void Refresh()
        {
            _items?.Refresh(_network);
            if (_mineral != null) _mineral.text = "광물  " + (_network != null && _network.IsReady ? _network.Mineral.ToString("N0") : "—");
            if (_gas != null) _gas.text = "가스  " + (_network != null && _network.IsReady ? _network.Gas.ToString("N0") : "—");
            RefreshSupply(RunnerSupplyRules.SkillId, _skillButton, _skillPrice);
            RefreshSupply(RunnerSupplyRules.WeaponId, _weaponButton, _weaponPrice);
        }

        private void RefreshSupply(int id, Button button, TMP_Text price)
        {
            if (button != null) button.interactable = _network != null && !_network.IsPurchasePending &&
                _network.Evaluate(id) == RunnerSupplyResult.Success;
            if (price != null && _network != null && _network.Catalog != null &&
                _network.Catalog.TryGet(id, out RunnerSupplyDefinition product))
                price.text = LaboratoryItemDropdownUI.Price(product);
        }
    }
}
