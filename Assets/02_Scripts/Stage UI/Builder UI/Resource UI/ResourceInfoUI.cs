using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class ResourceInfoUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _mineralText;
        [SerializeField] private TextMeshProUGUI _gasText;

        private ResourceSystem _resourceSystem;

        private void OnEnable()
        {
            RefreshResourceText();
            BindResourceChangedEvent();
        }

        private void OnDisable()
        {
            UnbindResourceChangedEvent();
        }

        public void InitializeResourceInfoUI(ResourceSystem resourceSystem)
        {
            _resourceSystem = resourceSystem;

            if (gameObject.activeInHierarchy)
            {
                RefreshResourceText();
                BindResourceChangedEvent();
            }
        }

        private void BindResourceChangedEvent()
        {
            if (_resourceSystem == null)
                return;

            UnbindResourceChangedEvent();
            _resourceSystem.OnChangedMineral += SetMineralText;
            _resourceSystem.OnChangedGas += SetGasText;
        }

        private void UnbindResourceChangedEvent()
        {
            if (_resourceSystem == null)
                return;

            _resourceSystem.OnChangedMineral -= SetMineralText;
            _resourceSystem.OnChangedGas -= SetGasText;
        }

        private void RefreshResourceText()
        {
            if (_resourceSystem == null)
                return;

            SetMineralText(_resourceSystem.Mineral);
            SetGasText(_resourceSystem.Gas);
        }

        private void SetMineralText(int mineral)
        {
            _mineralText.text = mineral.ToString();
        }

        private void SetGasText(int gas)
        {
            _gasText.text = gas.ToString();
        }
    }
}