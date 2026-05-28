using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public class RegenerationTower : CellBuffSupportTower
    {
        [Header("재생 버프")]
        [SerializeField] private Color _buffCellColor = new(0.15f, 0.9f, 0.55f, 0.38f);
        [SerializeField] private float _armor = 100f;
        [SerializeField] private float _maxHeal = 100f;
        [SerializeField] private float _healPerSec = 5f;

        private RegenerationTowerBuffParam _buffParam;
        private HashSet<IBuffReceiver> _receivers;
        private HashSet<IBuffReceiver> _detectedReceivers;
        private List<IBuffReceiver> _exitCandidates;
        private HashSet<IHeal> _armorReceivedHeals;
        private float _remainingHeal;

        public override Color BuffCellColor => _buffCellColor;

        protected override void TowerAwake()
        {
            base.TowerAwake();
            _buffParam = new RegenerationTowerBuffParam();
            _receivers = new HashSet<IBuffReceiver>();
            _detectedReceivers = new HashSet<IBuffReceiver>();
            _exitCandidates = new List<IBuffReceiver>();
            _armorReceivedHeals = new HashSet<IHeal>();
        }

        public override void Spawned()
        {
            base.Spawned();
            _remainingHeal = _maxHeal;
            _buffParam.ShieldAmount = _armor;
            _buffParam.TotalHealCharge = _maxHeal;
            _buffParam.RemainingHealCharge = _remainingHeal;
            _buffParam.HealPerSecond = _healPerSec;
        }

        protected override void TowerFixedUpdateNetwork()
        {
            RefreshBuffCellSource();

            if (!HasStateAuthority)
            {
                return;
            }

            if (_remainingHeal <= 0f)
            {
                DespawnRegenerationTower();
                return;
            }

            _detectedReceivers.Clear();
            IReadOnlyList<BuffReceiverRegistry.Entry> snapshot = BuffReceiverRegistry.Snapshot;
            for (int i = 0; i < snapshot.Count; i++)
            {
                BuffReceiverRegistry.Entry entry = snapshot[i];
                if ((entry.TargetType & BuffTargetType.Runner) == 0 || !IsInsideBuffCells(entry.Transform))
                {
                    continue;
                }

                IBuffReceiver receiver = entry.Receiver;
                if (!_detectedReceivers.Add(receiver))
                {
                    continue;
                }

                if (_receivers.Add(receiver))
                {
                    receiver.BuffEnter(_buffParam);
                }
                else
                {
                    receiver.BuffStay(_buffParam);
                }

                if (receiver is not IHeal heal)
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
                _buffParam.RemainingHealCharge = _remainingHeal;
                if (_remainingHeal <= 0f)
                {
                    DespawnRegenerationTower();
                    return;
                }
            }

            ExitUndetectedReceivers();
        }

        protected override void TowerDespawned()
        {
            ExitAllReceivers();
            base.TowerDespawned();
            _armorReceivedHeals?.Clear();
        }

        private void DespawnRegenerationTower()
        {
            ReleaseGridOccupation();
            Runner.Despawn(Object);
        }

        private void ExitUndetectedReceivers()
        {
            _exitCandidates.Clear();
            foreach (IBuffReceiver receiver in _receivers)
            {
                if (!_detectedReceivers.Contains(receiver))
                {
                    _exitCandidates.Add(receiver);
                }
            }

            for (int i = 0; i < _exitCandidates.Count; i++)
            {
                IBuffReceiver receiver = _exitCandidates[i];
                receiver.BuffExit(_buffParam);
                _receivers.Remove(receiver);
            }
        }

        private void ExitAllReceivers()
        {
            if (_receivers == null)
            {
                return;
            }

            foreach (IBuffReceiver receiver in _receivers)
            {
                receiver.BuffExit(_buffParam);
            }

            _receivers.Clear();
        }
    }
}