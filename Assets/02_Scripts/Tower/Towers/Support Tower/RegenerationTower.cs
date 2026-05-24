using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class RegenerationTower : SupportTower
    {
        [Header("Heal")]
        [SerializeField] private LayerMask _detectLayer;
        [SerializeField] private float _healRange = 25f;
        [SerializeField] private float _armor = 100f;
        [SerializeField] private float _maxHeal = 100f;
        [SerializeField] private float _healPerSec = 5f;

        private Collider[] _detectedHealReceivers;
        private HashSet<IHeal> _armorReceivedHeals;
        private HashSet<IHeal> _currentFrameHeals;
        private float _remainingHeal;

        protected override void TowerAwake()
        {
            _detectedHealReceivers = new Collider[100];
            _armorReceivedHeals = new HashSet<IHeal>();
            _currentFrameHeals = new HashSet<IHeal>();
        }

        public override void Spawned()
        {
            base.Spawned();
            _remainingHeal = _maxHeal;
        }

        protected override void TowerFixedUpdateNetwork()
        {
            if (!HasStateAuthority)
            {
                return;
            }

            if (_remainingHeal <= 0f)
            {
                DespawnRegenerationTower();
                return;
            }

            int detectedCount = Physics.OverlapSphereNonAlloc(transform.position, _healRange, _detectedHealReceivers, _detectLayer);
            _currentFrameHeals.Clear();
            for (int i = 0; i < detectedCount; i++)
            {
                if (!_detectedHealReceivers[i].TryGetComponent(out IHeal heal))
                {
                    continue;
                }

                if (!_currentFrameHeals.Add(heal))
                {
                    continue;
                }

                if (_armorReceivedHeals.Add(heal))
                {
                    heal.ReceiveArmor(_armor);
                }

                float healAmount = Mathf.Min(_healPerSec * Runner.DeltaTime, _remainingHeal);
                float actualHealedAmount = heal.Heal(healAmount);
                if (actualHealedAmount <= 0f)
                {
                    continue;
                }

                _remainingHeal = Mathf.Max(0f, _remainingHeal - actualHealedAmount);
                if (_remainingHeal <= 0f)
                {
                    DespawnRegenerationTower();
                    break;
                }
            }
        }

        protected override void TowerDespawned()
        {
            base.TowerDespawned();
            _armorReceivedHeals?.Clear();
        }

        private void DespawnRegenerationTower()
        {
            ReleaseGridOccupation();
            Runner.Despawn(Object);
        }
    }
}