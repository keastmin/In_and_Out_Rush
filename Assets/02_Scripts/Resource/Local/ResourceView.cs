using TMPro;
using UnityEngine;

namespace Dev.Local
{
    public class ResourceView : View
    {
        [SerializeField] private TextMeshProUGUI _mineralText;
        [SerializeField] private TextMeshProUGUI _gasText;

        public void SetMineral(int mineral)
            => _mineralText.text = $"Mineral: {mineral}";

        public void SetGas(int gas)
            => _gasText.text = $"Gas: {gas}";
    }
}