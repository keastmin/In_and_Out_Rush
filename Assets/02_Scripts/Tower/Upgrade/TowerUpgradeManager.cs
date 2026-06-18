using Fusion;
using System;
using UnityEngine;

namespace KIM.Dev {
    public class TowerUpgradeManager : NetworkBehaviour
    {
        [SerializeField] private int _maxSentryGunUpgradeCount;
        [SerializeField] private int _maxLaserBeamUpgradeCount;
        [SerializeField] private int _maxMisileRauncherUpgradeCount;
        [SerializeField] private int _maxRailGunUpgradeCount;
        [SerializeField] private int _maxBladeUpgradeCount;
        [SerializeField] private int _maxPlasmaUpgradeCount;
        [SerializeField] private int _maxSparkUpgradeCount;
        [SerializeField] private int _maxBiohazardUpgradeCount;

        [Networked]
        private int _currentSentryGunUpgradeCount { get; set; }
        [Networked]
        private int _currentLaserBeamUpgradeCount { get; set; }
        [Networked]
        private int _currentMisileRauncherUpgradeCount { get; set; }
        [Networked]
        private int _currentRailGunUpgradeCount { get; set; }
        [Networked]
        private int _currentBladeUpgradeCount { get; set; }
        [Networked]
        private int _currentPlasmaUpgradeCount { get; set; }
        [Networked]
        private int _currentSparkUpgradeCount { get; set; }
        [Networked]
        private int _currentBiohazardUpgradeCount { get; set; }

        public override void Spawned()
        {
            InitializeTowerUpgradeManager();
        }

        public bool TryTowerUpgrade(TowerUpgradeType type)
        {
            if (!IsCanUpgrade(type))
                return false;

            TowerUpgradeCountUp(type);

            return true;
        }

        private void InitializeTowerUpgradeManager()
        {
            if (!HasStateAuthority)
                return;

            _currentSentryGunUpgradeCount = 0;
            _currentLaserBeamUpgradeCount = 0;
            _currentMisileRauncherUpgradeCount = 0;
            _currentRailGunUpgradeCount = 0;
            _currentBladeUpgradeCount = 0;
            _currentPlasmaUpgradeCount = 0;
            _currentSparkUpgradeCount = 0;
            _currentBiohazardUpgradeCount = 0;
        }

        private bool IsCanUpgrade(TowerUpgradeType type)
        {
            return GetCurrentUpgradeCount(type) < GetMaxUpgradeCount(type);
        }

        private int GetCurrentUpgradeCount(TowerUpgradeType type)
        {
            return type switch
            {
                TowerUpgradeType.SentryGun => _currentSentryGunUpgradeCount,
                TowerUpgradeType.LaserBeam => _currentLaserBeamUpgradeCount,
                TowerUpgradeType.MisileRauncher => _currentMisileRauncherUpgradeCount,
                TowerUpgradeType.RailGun => _currentRailGunUpgradeCount,
                TowerUpgradeType.Blade => _currentBladeUpgradeCount,
                TowerUpgradeType.Plasma => _currentPlasmaUpgradeCount,
                TowerUpgradeType.Spark => _currentSparkUpgradeCount,
                TowerUpgradeType.Biohazard => _currentBiohazardUpgradeCount,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private int GetMaxUpgradeCount(TowerUpgradeType type)
        {
            return type switch
            {
                TowerUpgradeType.SentryGun => _maxSentryGunUpgradeCount,
                TowerUpgradeType.LaserBeam => _maxLaserBeamUpgradeCount,
                TowerUpgradeType.MisileRauncher => _maxMisileRauncherUpgradeCount,
                TowerUpgradeType.RailGun => _maxRailGunUpgradeCount,
                TowerUpgradeType.Blade => _maxBladeUpgradeCount,
                TowerUpgradeType.Plasma => _maxPlasmaUpgradeCount,
                TowerUpgradeType.Spark => _maxSparkUpgradeCount,
                TowerUpgradeType.Biohazard => _maxBiohazardUpgradeCount,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        private void TowerUpgradeCountUp(TowerUpgradeType type)
        {
            RPC_RequestTowerUpgradeCountUp(type);
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestTowerUpgradeCountUp(TowerUpgradeType type)
        {
            if (!HasStateAuthority)
                return;

            switch (type)
            {
                case TowerUpgradeType.SentryGun:
                    _currentSentryGunUpgradeCount++;
                    break;
                case TowerUpgradeType.LaserBeam:
                    _currentLaserBeamUpgradeCount++;
                    break;
                case TowerUpgradeType.MisileRauncher:
                    _currentMisileRauncherUpgradeCount++;
                    break;
                case TowerUpgradeType.RailGun:
                    _currentRailGunUpgradeCount++;
                    break;
                case TowerUpgradeType.Blade:
                    _currentBladeUpgradeCount++;
                    break;
                case TowerUpgradeType.Plasma:
                    _currentPlasmaUpgradeCount++;
                    break;
                case TowerUpgradeType.Spark:
                    _currentSparkUpgradeCount++;
                    break;
                case TowerUpgradeType.Biohazard:
                    _currentBiohazardUpgradeCount++;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
    }
}
