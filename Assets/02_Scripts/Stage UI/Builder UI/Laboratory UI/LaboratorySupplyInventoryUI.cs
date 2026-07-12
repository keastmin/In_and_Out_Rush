using UnityEngine;
using UnityEngine.UI;

namespace KIM.Dev
{
    public sealed class LaboratorySupplyInventoryUI : MonoBehaviour
    {
        [SerializeField] private Image[] _slotIcons = System.Array.Empty<Image>();
        [SerializeField] private GameObject[] _emptySlotMarkers = System.Array.Empty<GameObject>();
        [SerializeField] private Sprite _skillSupplyIcon;
        [SerializeField] private Sprite _itemSupplyIcon;
        [SerializeField] private Sprite _weaponSupplyIcon;

        private SupplyTowerManager _supplyManager;
        private bool _hasStarted;

        private void Awake()
        {
            ClearSlots();
        }

        private void OnEnable()
        {
            if (!_hasStarted)
                return;

            BindSupplyManager();
            RefreshFromPendingSupplies();
        }

        private void Start()
        {
            _hasStarted = true;
            BindSupplyManager();
            RefreshFromPendingSupplies();
        }

        private void OnDisable()
        {
            UnbindSupplyManager();
        }

        public void RefreshFromPendingSupplies()
        {
            BindSupplyManager();
            int[] pendingSupplies = _supplyManager != null
                ? _supplyManager.PeekSupplyNumArray()
                : System.Array.Empty<int>();

            for (int i = 0; i < _slotIcons.Length; i++)
            {
                Sprite supplyIcon = i < pendingSupplies.Length
                    ? GetSupplyIcon(pendingSupplies[i])
                    : null;
                SetSlot(i, supplyIcon);
            }
        }

        private void BindSupplyManager()
        {
            SupplyTowerManager currentManager = SupplyTowerManager.Instance;
            if (_supplyManager == currentManager)
                return;

            UnbindSupplyManager();
            _supplyManager = currentManager;
            if (_supplyManager != null)
                _supplyManager.OnUIRevertAction += HandlePendingSuppliesConsumed;
        }

        private void UnbindSupplyManager()
        {
            if (_supplyManager != null)
                _supplyManager.OnUIRevertAction -= HandlePendingSuppliesConsumed;

            _supplyManager = null;
        }

        private void HandlePendingSuppliesConsumed()
        {
            RefreshFromPendingSupplies();
        }

        private void ClearSlots()
        {
            for (int i = 0; i < _slotIcons.Length; i++)
                SetSlot(i, null);
        }

        private void SetSlot(int index, Sprite supplyIcon)
        {
            if (index < 0 || index >= _slotIcons.Length)
                return;

            Image slotIcon = _slotIcons[index];
            if (slotIcon != null)
            {
                slotIcon.sprite = supplyIcon;
                slotIcon.enabled = supplyIcon != null;
            }

            if (index < _emptySlotMarkers.Length && _emptySlotMarkers[index] != null)
                _emptySlotMarkers[index].SetActive(supplyIcon == null);
        }

        private Sprite GetSupplyIcon(int supplyNumber)
        {
            return supplyNumber switch
            {
                SupplyTowerManager.SKILL_SUPPLY_NUM => _skillSupplyIcon,
                SupplyTowerManager.ITEM_SUPPLY_NUM => _itemSupplyIcon,
                SupplyTowerManager.WEAPON_SUPPLY_NUM => _weaponSupplyIcon,
                _ => null
            };
        }
    }
}
