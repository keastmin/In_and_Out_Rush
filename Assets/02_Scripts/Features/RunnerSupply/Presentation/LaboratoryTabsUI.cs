using UnityEngine;
using UnityEngine.UI;

namespace ProjectIO.RunnerSupply
{
    public sealed class LaboratoryTabsUI : MonoBehaviour
    {
        [SerializeField] private GameObject _towerPage;
        [SerializeField] private GameObject _runnerPage;
        [SerializeField] private Image _towerTab;
        [SerializeField] private Image _runnerTab;
        [SerializeField] private Color _selected = new(0.08f, 0.36f, 0.43f, 1f);
        [SerializeField] private Color _normal = new(0.07f, 0.12f, 0.17f, 1f);
        public void ShowTower() => Select(false);
        public void ShowRunner() => Select(true);
        private void Select(bool runner)
        {
            _towerPage.SetActive(!runner);
            _runnerPage.SetActive(runner);
            _towerTab.color = runner ? _normal : _selected;
            _runnerTab.color = runner ? _selected : _normal;
        }
    }
}
