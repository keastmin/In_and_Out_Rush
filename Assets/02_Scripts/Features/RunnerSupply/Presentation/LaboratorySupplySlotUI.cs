using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ProjectIO.RunnerSupply
{
    public sealed class LaboratorySupplySlotUI : MonoBehaviour
    {
        [SerializeField] private Image _icon;
        [SerializeField] private TMP_Text _name;
        [SerializeField] private TMP_Text _order;
        [SerializeField] private GameObject _empty;

        public void Display(int index, RunnerSupplyDefinition product)
        {
            if (_order != null) _order.text = (index + 1).ToString("00");
            if (_icon != null)
            {
                _icon.sprite = product?.Icon;
                _icon.enabled = product?.Icon != null;
            }
            if (_name != null) _name.text = product?.Name ?? string.Empty;
            if (_empty != null) _empty.SetActive(product == null);
        }
    }
}
