using Fusion;
using Dev.Network;
using System.Collections.Generic;
using UnityEngine;

namespace KIM.Dev
{
    public sealed class AmplificationTower : CellBuffSupportTower, ICanDragObject
    {
        [Header("버프")]
        [SerializeField] private AmplificationTowerBuffParam _buffParam = new();
        [SerializeField] private Color _buffCellColor = new(1f, 0.85f, 0.15f, 0.38f);

        private HashSet<IBuffReceiver> _receivers;
        private HashSet<IBuffReceiver> _detectedReceivers;
        private List<IBuffReceiver> _exitCandidates;

        public override Color BuffCellColor => _buffCellColor;

        protected override void TowerAwake()
        {
            base.TowerAwake();
            _receivers = new HashSet<IBuffReceiver>();
            _detectedReceivers = new HashSet<IBuffReceiver>();
            _exitCandidates = new List<IBuffReceiver>();
        }

        protected override void TowerFixedUpdateNetwork()
        {
            RefreshBuffCellSource();

            if (!HasStateAuthority || !Runner.IsServer)
            {
                return;
            }

            _detectedReceivers.Clear();
            IReadOnlyList<BuffReceiverRegistry.Entry> snapshot = BuffReceiverRegistry.Snapshot;
            for (int i = 0; i < snapshot.Count; i++)
            {
                BuffReceiverRegistry.Entry entry = snapshot[i];
                if ((entry.TargetType & (BuffTargetType.Runner | BuffTargetType.Tower)) == 0 ||
                    !IsInsideBuffCells(entry.Transform))
                {
                    continue;
                }

                IBuffReceiver receiver = entry.Receiver;
                if (!_detectedReceivers.Add(receiver))
                {
                    continue;
                }

                if (_receivers.Contains(receiver))
                {
                    receiver.BuffStay(_buffParam);
                }
                else
                {
                    receiver.BuffEnter(_buffParam);
                    _receivers.Add(receiver);
                }
            }

            ExitUndetectedReceivers();
        }

        protected override void TowerDespawned()
        {
            ExitAllReceivers();
            base.TowerDespawned();
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

        #region ICanDragObject

        public void OnDragSelectedThisObject()
        {
            OnLeftMouseDownThisObject();
        }

        public void OnDragOverThisObject()
        {
            OnCancelClickThisObject();
        }

        public void OnDragCompleteThisObject()
        {
            var manager = StageBootstrapper.Instance;
            if (manager != null)
            {
                manager.PlayerBuilder.TowerSelected(this);
            }
        }

        #endregion
    }
}