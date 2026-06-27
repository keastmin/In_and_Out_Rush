using Fusion;
using System;
using UnityEngine;

namespace KIM.Dev {
    public class TowerUpgradeManager : NetworkBehaviour
    {
        public event Action<TowerUpgradeType, bool, int> OnTowerUpgradeRequestCompleted;

        [SerializeField] private int _maxSentryGunUpgradeCount;
        [SerializeField] private int _maxLaserBeamUpgradeCount;
        [SerializeField] private int _maxMisileRauncherUpgradeCount;
        [SerializeField] private int _maxRailGunUpgradeCount;
        [SerializeField] private int _maxBladeUpgradeCount;
        [SerializeField] private int _maxPlasmaUpgradeCount;
        [SerializeField] private int _maxSparkUpgradeCount;
        [SerializeField] private int _maxBiohazardUpgradeCount;
        [SerializeField, Min(0f)] private float _towerDamageIncreaseRatePerUpgrade = 0.2f;
        [SerializeField, Min(0f)] private float _propertyDamageIncreaseRatePerUpgrade = 0.05f;

        [Networked, OnChangedRender(nameof(SentryGunUpgradeCountChange))]
        private int _currentSentryGunUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(LaserBeamUpgradeCountChange))]
        private int _currentLaserBeamUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(MisileRauncherUpgradeCountChange))]
        private int _currentMisileRauncherUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(RailGunUpgradeCountChange))]
        private int _currentRailGunUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(BladeUpgradeCountChange))]
        private int _currentBladeUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(PlasmaUpgradeCountChange))]
        private int _currentPlasmaUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(SparkUpgradeCountChange))]
        private int _currentSparkUpgradeCount { get; set; }
        [Networked, OnChangedRender(nameof(BiohazardUpgradeCountChange))]
        private int _currentBiohazardUpgradeCount { get; set; }

        public event Action<TowerUpgradeType, int> OnTowerUpgradeCountChange;

        public override void Spawned()
        {
            InitializeTowerUpgradeManager();
        }

        public bool TryTowerUpgrade(TowerUpgradeType type)
        {
            if (!Enum.IsDefined(typeof(TowerUpgradeType), type))
                return false;

            if (!IsCanUpgrade(type))
                return false;

            if (!CanAffordUpgrade(type))
                return false;

            RPC_RequestTowerUpgradeCountUp(type);

            return true;
        }

        public bool IsUpgradeCountReflected(TowerUpgradeType type, int upgradeCount)
        {
            return GetCurrentUpgradeCount(type) >= upgradeCount;
        }

        public float CalculateDamage(TowerUpgradeType type, float baseDamage)
        {
            float damageMultiplier = 1f + GetCurrentUpgradeCount(type) * _towerDamageIncreaseRatePerUpgrade;
            return Mathf.Max(0f, baseDamage) * damageMultiplier;
        }

        public float CalculateDamage(TowerUpgradeType type, TowerPropertiesType propertyType, float baseDamage)
        {
            float safeBaseDamage = Mathf.Max(0f, baseDamage);
            float towerDamageBonus = GetCurrentUpgradeCount(type) * _towerDamageIncreaseRatePerUpgrade;
            float propertyDamageBonus = GetPropertyUpgradeCount(propertyType) * _propertyDamageIncreaseRatePerUpgrade;
            return safeBaseDamage * (1f + towerDamageBonus + propertyDamageBonus);
        }

        public Cost GetUpgradeCost(TowerUpgradeType type)
        {
            int currentUpgradeCount = GetCurrentUpgradeCount(type);
            int mineralIncreasePerUpgrade = IsPropertyUpgradeType(type) ? 10 : 50;
            return new Cost(100 + mineralIncreasePerUpgrade * currentUpgradeCount, 0);
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
            return IsSupportedUpgradeType(type) &&
                   GetCurrentUpgradeCount(type) < GetMaxUpgradeCount(type);
        }

        public int GetCurrentUpgradeCount(TowerUpgradeType type)
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

        private int GetPropertyUpgradeCount(TowerPropertiesType propertyType)
        {
            return propertyType switch
            {
                TowerPropertiesType.Flame => _currentPlasmaUpgradeCount,
                TowerPropertiesType.Blitz => _currentSparkUpgradeCount,
                TowerPropertiesType.Biochemical => _currentBiohazardUpgradeCount,
                _ => 0
            };
        }

        private static bool IsPropertyUpgradeType(TowerUpgradeType type)
        {
            return type == TowerUpgradeType.Plasma ||
                   type == TowerUpgradeType.Spark ||
                   type == TowerUpgradeType.Biohazard;
        }

        private static bool IsSupportedUpgradeType(TowerUpgradeType type)
        {
            return type != TowerUpgradeType.RailGun;
        }

        private bool CanAffordUpgrade(TowerUpgradeType type)
        {
            return ResourceSystem.Instance != null &&
                   ResourceSystem.Instance.IsResourceSufficient(GetUpgradeCost(type));
        }

        private bool TryPayUpgradeCost(TowerUpgradeType type)
        {
            if (ResourceSystem.Instance == null)
                return false;

            Cost cost = GetUpgradeCost(type);
            if (!ResourceSystem.Instance.IsResourceSufficient(cost))
                return false;

            ResourceSystem.Instance.Mineral -= cost.Mineral;
            ResourceSystem.Instance.Gas -= cost.Gas;
            return true;
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RequestTowerUpgradeCountUp(TowerUpgradeType type, RpcInfo rpcInfo = default)
        {
            bool isValidType = Enum.IsDefined(typeof(TowerUpgradeType), type);
            bool isUpgradeAccepted = isValidType && IsCanUpgrade(type) && TryPayUpgradeCost(type);

            if (isUpgradeAccepted)
                TowerUpgradeCountUp(type);

            int currentUpgradeCount = isValidType ? GetCurrentUpgradeCount(type) : 0;
            SendTowerUpgradeResult(rpcInfo.Source, type, isUpgradeAccepted, currentUpgradeCount);
        }

        private void SendTowerUpgradeResult(
            PlayerRef requester,
            TowerUpgradeType type,
            bool isUpgradeAccepted,
            int currentUpgradeCount)
        {
            if (requester == PlayerRef.None)
            {
                NotifyTowerUpgradeRequestCompleted(type, isUpgradeAccepted, currentUpgradeCount);
                return;
            }

            RPC_NotifyTowerUpgradeResult(requester, type, isUpgradeAccepted, currentUpgradeCount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_NotifyTowerUpgradeResult(
            [RpcTarget] PlayerRef requester,
            TowerUpgradeType type,
            bool isUpgradeAccepted,
            int currentUpgradeCount)
        {
            NotifyTowerUpgradeRequestCompleted(type, isUpgradeAccepted, currentUpgradeCount);
        }

        private void NotifyTowerUpgradeRequestCompleted(
            TowerUpgradeType type,
            bool isUpgradeAccepted,
            int currentUpgradeCount)
        {
            OnTowerUpgradeRequestCompleted?.Invoke(type, isUpgradeAccepted, currentUpgradeCount);
        }

        private void SentryGunUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.SentryGun, _currentSentryGunUpgradeCount);
        }

        private void LaserBeamUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.LaserBeam, _currentLaserBeamUpgradeCount);
        }

        private void MisileRauncherUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.MisileRauncher, _currentMisileRauncherUpgradeCount);
        }

        private void RailGunUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.RailGun, _currentRailGunUpgradeCount);
        }

        private void BladeUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.Blade, _currentBladeUpgradeCount);
        }

        private void PlasmaUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.Plasma, _currentPlasmaUpgradeCount);
        }

        private void SparkUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.Spark, _currentSparkUpgradeCount);
        }

        private void BiohazardUpgradeCountChange()
        {
            OnTowerUpgradeCountChange?.Invoke(TowerUpgradeType.Biohazard, _currentBiohazardUpgradeCount);
        }
    }
}
