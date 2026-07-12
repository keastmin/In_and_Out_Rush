using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class RunnerSupplyUI : MonoBehaviour
    {
        [Header("보급품 설정")]
        [SerializeField, Min(1)] private int _maxSupplyCount = 10;
        [SerializeField] private Cost _skillSupplyCost = new Cost(50, 50);
        [SerializeField] private Cost _itemSupplyCost = new Cost(50, 50);
        [SerializeField] private Cost _weaponSupplyCost = new Cost(50, 50);

        [Header("비용 텍스트")]
        [SerializeField] private TextMeshProUGUI _skillMineralCostText;
        [SerializeField] private TextMeshProUGUI _skillGasCostText;
        [SerializeField] private TextMeshProUGUI _itemMineralCostText;
        [SerializeField] private TextMeshProUGUI _itemGasCostText;
        [SerializeField] private TextMeshProUGUI _weaponMineralCostText;
        [SerializeField] private TextMeshProUGUI _weaponGasCostText;

        [SerializeField] private LaboratorySupplyInventoryUI _supplyInventoryUI;

        private void OnEnable()
        {
            RefreshAllSupplyCosts();
        }

        public void InitializeRunnerSupplyUI()
        {
            RefreshAllSupplyCosts();
            _supplyInventoryUI?.RefreshFromPendingSupplies();
        }

        public void OnClickSkillSupplyButton()
        {
            TryPurchaseSupply(
                SupplyTowerManager.SKILL_SUPPLY_NUM,
                _skillSupplyCost,
                _skillMineralCostText,
                _skillGasCostText);
        }

        public void OnClickItemSupplyButton()
        {
            TryPurchaseSupply(
                SupplyTowerManager.ITEM_SUPPLY_NUM,
                _itemSupplyCost,
                _itemMineralCostText,
                _itemGasCostText);
        }

        public void OnClickWeaponSupplyButton()
        {
            TryPurchaseSupply(
                SupplyTowerManager.WEAPON_SUPPLY_NUM,
                _weaponSupplyCost,
                _weaponMineralCostText,
                _weaponGasCostText);
        }

        private void TryPurchaseSupply(
            int supplyNumber,
            Cost cost,
            TextMeshProUGUI mineralCostText,
            TextMeshProUGUI gasCostText)
        {
            SupplyTowerManager supplyManager = SupplyTowerManager.Instance;
            if (supplyManager == null || supplyManager.PendingSupplyCount >= _maxSupplyCount)
                return;

            if (ResourceSystem.Instance == null || !ResourceSystem.Instance.IsResourceSufficient(cost))
                return;

            supplyManager.FillSupplyList(supplyNumber);
            ResourceSystem.Instance.DeductCost(cost);
            RefreshSupplyCost(cost, mineralCostText, gasCostText);
            _supplyInventoryUI?.RefreshFromPendingSupplies();
        }

        private void RefreshAllSupplyCosts()
        {
            RefreshSupplyCost(_skillSupplyCost, _skillMineralCostText, _skillGasCostText);
            RefreshSupplyCost(_itemSupplyCost, _itemMineralCostText, _itemGasCostText);
            RefreshSupplyCost(_weaponSupplyCost, _weaponMineralCostText, _weaponGasCostText);
        }

        private static void RefreshSupplyCost(
            Cost cost,
            TextMeshProUGUI mineralCostText,
            TextMeshProUGUI gasCostText)
        {
            if (mineralCostText != null)
                mineralCostText.text = cost.Mineral.ToString();
            if (gasCostText != null)
                gasCostText.text = cost.Gas.ToString();
        }
    }
}
