using UnityEngine;

namespace KIM.Dev
{
    public class TowerUpgradeUI : MonoBehaviour
    {
        private TowerUpgradeManager _towerUpgradeManager;

        public void InitializeTowerUpgradeUI(TowerUpgradeManager towerUpgradeManager)
        {
            _towerUpgradeManager = towerUpgradeManager;
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

            _towerUpgradeManager.TryTowerUpgrade(type);
        }
    }
}