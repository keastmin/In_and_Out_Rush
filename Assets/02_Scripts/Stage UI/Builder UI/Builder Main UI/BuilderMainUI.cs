using UnityEngine;

namespace KIM.Dev
{
    public class BuilderMainUI : MonoBehaviour
    {
        [SerializeField] private MainButtons _mainButtons;
        [SerializeField] private TowerTypeButtons _towerTypeButtons;
        [SerializeField] private AttackTowerButtons _attackTowerButtons;
        [SerializeField] private CenterTowerButtons _centerTowerButtons;
        [SerializeField] private SupportTowerButtons _supportTowerButtons;

        private void Awake()
        {
            DisableAll();
        }

        private void Start()
        {
            InitializeBuilderMainUI();
        }

        private void DisableAll()
        {
            _mainButtons.gameObject.SetActive(false);
            _towerTypeButtons.gameObject.SetActive(false);
            _attackTowerButtons.gameObject.SetActive(false);
            _centerTowerButtons.gameObject.SetActive(false);
            _supportTowerButtons.gameObject.SetActive(false);
        }

        private void InitializeBuilderMainUI()
        {
            _mainButtons.gameObject.SetActive(true);
        }
    }
}