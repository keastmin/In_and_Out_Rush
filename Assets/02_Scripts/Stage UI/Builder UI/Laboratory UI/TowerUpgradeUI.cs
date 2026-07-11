using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class TowerUpgradeUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _sentryUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _laserUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _misileUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _railGunUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _bladeUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _plasmaUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _sparkUpgradeCountText;
        [SerializeField] private TextMeshProUGUI _biohazardUpgradeCountText;

        [Header("다음 업그레이드 비용")]
        [SerializeField] private TextMeshProUGUI _sentryMineralCostText;
        [SerializeField] private TextMeshProUGUI _sentryGasCostText;
        [SerializeField] private TextMeshProUGUI _laserMineralCostText;
        [SerializeField] private TextMeshProUGUI _laserGasCostText;
        [SerializeField] private TextMeshProUGUI _misileMineralCostText;
        [SerializeField] private TextMeshProUGUI _misileGasCostText;
        [SerializeField] private TextMeshProUGUI _railGunMineralCostText;
        [SerializeField] private TextMeshProUGUI _railGunGasCostText;
        [SerializeField] private TextMeshProUGUI _bladeMineralCostText;
        [SerializeField] private TextMeshProUGUI _bladeGasCostText;
        [SerializeField] private TextMeshProUGUI _plasmaMineralCostText;
        [SerializeField] private TextMeshProUGUI _plasmaGasCostText;
        [SerializeField] private TextMeshProUGUI _sparkMineralCostText;
        [SerializeField] private TextMeshProUGUI _sparkGasCostText;
        [SerializeField] private TextMeshProUGUI _biohazardMineralCostText;
        [SerializeField] private TextMeshProUGUI _biohazardGasCostText;

        private TowerUpgradeManager _towerUpgradeManager;
        private readonly HashSet<TowerUpgradeType> _pendingUpgradeTypes = new();
        private readonly Dictionary<TowerUpgradeType, int> _acceptedUpgradeCounts = new();
        private readonly List<TowerUpgradeType> _reflectedUpgradeTypes = new();

        private bool _isInitialize = false;

        private void OnEnable()
        {
            BindTowerUpgradeEvent();
        }

        private void OnDisable()
        {
            UnbindTowerUpgradeEvent();
        }

        private void Update()
        {
            if (_towerUpgradeManager == null || _acceptedUpgradeCounts.Count == 0)
                return;

            _reflectedUpgradeTypes.Clear();

            foreach (KeyValuePair<TowerUpgradeType, int> upgrade in _acceptedUpgradeCounts)
            {
                if (_towerUpgradeManager.IsUpgradeCountReflected(upgrade.Key, upgrade.Value))
                    _reflectedUpgradeTypes.Add(upgrade.Key);
            }

            foreach (TowerUpgradeType type in _reflectedUpgradeTypes)
            {
                _acceptedUpgradeCounts.Remove(type);
                _pendingUpgradeTypes.Remove(type);
            }
        }

        private void OnDestroy()
        {
            UnbindTowerUpgradeEvent();

            if (_towerUpgradeManager != null)
                _towerUpgradeManager.OnTowerUpgradeRequestCompleted -= HandleTowerUpgradeRequestCompleted;
        }

        public void InitializeTowerUpgradeUI(TowerUpgradeManager towerUpgradeManager)
        {
            if (_towerUpgradeManager != null)
                _towerUpgradeManager.OnTowerUpgradeRequestCompleted -= HandleTowerUpgradeRequestCompleted;

            _pendingUpgradeTypes.Clear();
            _acceptedUpgradeCounts.Clear();
            _reflectedUpgradeTypes.Clear();
            _towerUpgradeManager = towerUpgradeManager;

            if (_towerUpgradeManager != null)
                _towerUpgradeManager.OnTowerUpgradeRequestCompleted += HandleTowerUpgradeRequestCompleted;

            _isInitialize = true;

            BindTowerUpgradeEvent();
        }

        private void BindTowerUpgradeEvent()
        {
            if (!_isInitialize || _towerUpgradeManager == null)
                return;
            _towerUpgradeManager.OnTowerUpgradeCountChange -= HandleTowerUpgradeCountTextChange;
            _towerUpgradeManager.OnTowerUpgradeCountChange += HandleTowerUpgradeCountTextChange;
            RefreshAllUpgradeCountTexts();
        }

        private void UnbindTowerUpgradeEvent()
        {
            if (!_isInitialize || _towerUpgradeManager == null)
                return;
            _towerUpgradeManager.OnTowerUpgradeCountChange -= HandleTowerUpgradeCountTextChange;
        }

        private void RefreshAllUpgradeCountTexts()
        {
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.SentryGun,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.SentryGun));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.LaserBeam,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.LaserBeam));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.MisileRauncher,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.MisileRauncher));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.RailGun,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.RailGun));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.Blade,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.Blade));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.Plasma,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.Plasma));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.Spark,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.Spark));
            HandleTowerUpgradeCountTextChange(
                TowerUpgradeType.Biohazard,
                _towerUpgradeManager.GetCurrentUpgradeCount(TowerUpgradeType.Biohazard));
        }

        public void OnClickSentryGunUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.SentryGun);
        }

        public void OnClickLaserBeamUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.LaserBeam);
        }

        public void OnClickMisileRauncherUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.MisileRauncher);
        }

        public void OnClickRailGunUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.RailGun);
        }

        public void OnClickBladeUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.Blade);
        }

        public void OnClickPlasmaUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.Plasma);
        }

        public void OnClickSparkUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.Spark);
        }

        public void OnClickBiohazardUpgradeButton()
        {
            RequestTowerUpgrade(TowerUpgradeType.Biohazard);
        }

        private void RequestTowerUpgrade(TowerUpgradeType type)
        {
            if(_towerUpgradeManager == null)
            {
                Debug.LogError("TowerUpgradeManager가 없음");
                return;
            }

            if (!_pendingUpgradeTypes.Add(type))
                return;

            if (!_towerUpgradeManager.TryTowerUpgrade(type))
                _pendingUpgradeTypes.Remove(type);
        }

        private void HandleTowerUpgradeRequestCompleted(
            TowerUpgradeType type,
            bool isUpgradeAccepted,
            int currentUpgradeCount)
        {
            if (!_pendingUpgradeTypes.Contains(type))
                return;

            if (!isUpgradeAccepted)
            {
                _pendingUpgradeTypes.Remove(type);
                return;
            }

            _acceptedUpgradeCounts[type] = currentUpgradeCount;
        }

        private void HandleTowerUpgradeCountTextChange(TowerUpgradeType type, int count)
        {
            TextMeshProUGUI upgradeCountText = type switch
            {
                TowerUpgradeType.SentryGun => _sentryUpgradeCountText,
                TowerUpgradeType.LaserBeam => _laserUpgradeCountText,
                TowerUpgradeType.MisileRauncher => _misileUpgradeCountText,
                TowerUpgradeType.RailGun => _railGunUpgradeCountText,
                TowerUpgradeType.Blade => _bladeUpgradeCountText,
                TowerUpgradeType.Plasma => _plasmaUpgradeCountText,
                TowerUpgradeType.Spark => _sparkUpgradeCountText,
                TowerUpgradeType.Biohazard => _biohazardUpgradeCountText,
                _ => null
            };

            if (upgradeCountText != null)
                upgradeCountText.text = "Lv. " + count;

            RefreshUpgradeCostText(type);
        }

        private void RefreshUpgradeCostText(TowerUpgradeType type)
        {
            if (_towerUpgradeManager == null)
                return;

            Cost cost = _towerUpgradeManager.GetUpgradeCost(type);
            (TextMeshProUGUI mineralText, TextMeshProUGUI gasText) = type switch
            {
                TowerUpgradeType.SentryGun => (_sentryMineralCostText, _sentryGasCostText),
                TowerUpgradeType.LaserBeam => (_laserMineralCostText, _laserGasCostText),
                TowerUpgradeType.MisileRauncher => (_misileMineralCostText, _misileGasCostText),
                TowerUpgradeType.RailGun => (_railGunMineralCostText, _railGunGasCostText),
                TowerUpgradeType.Blade => (_bladeMineralCostText, _bladeGasCostText),
                TowerUpgradeType.Plasma => (_plasmaMineralCostText, _plasmaGasCostText),
                TowerUpgradeType.Spark => (_sparkMineralCostText, _sparkGasCostText),
                TowerUpgradeType.Biohazard => (_biohazardMineralCostText, _biohazardGasCostText),
                _ => (null, null)
            };

            if (mineralText != null)
                mineralText.text = cost.Mineral.ToString();
            if (gasText != null)
                gasText.text = cost.Gas.ToString();
        }
    }
}
