using System;
using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public enum RoundPhase
    {
        Maintenance,
        Combat
    }

    public class TimeSystem : System
    {
        [Header("Round Settings")]
        [SerializeField] private float _maintenanceDuration = 30f;
        [SerializeField] private float _roundDuration = 180f;
        [SerializeField] private int _defaultRoundCount = 10;

        [Networked] public float ElapsedTime { get; private set; }
        [Networked] public int RoundNumber { get; private set; }
        [Networked] public RoundPhase Phase { get; private set; }
        [Networked] public float PhaseElapsedTime { get; private set; }
        [Networked] public NetworkBool IsBerserk { get; private set; }

        public float RoundDuration => _roundDuration;

        private bool _isRunning;

        public event Action<int, TimeSystem, object> OnRoundStarting;
        public event Action<int, TimeSystem, object> OnRoundEnded;
        public event Action<int, TimeSystem, object> OnMaintenanceStarting;
        public event Action<TimeSystem, object> OnBerserkStarted;

        protected override void OnInitialize()
        {
            ResetRoundState();
        }

        protected override void OnSetUp()
        {
            if (Object == null)
            {
                Debug.LogWarning("TimeSystem needs a NetworkObject to run round progression.");
                return;
            }

            if (!Object.HasStateAuthority)
                return;

            ResetRoundState();
            _isRunning = true;

            Debug.Log($"Round maintenance started. Next round: {RoundNumber}");
            OnMaintenanceStarting?.Invoke(RoundNumber, this, this);
        }

        protected override void OnTearDown()
        {
            _isRunning = false;
        }

        public override void FixedUpdateNetwork()
        {
            if (Object == null || !Object.HasStateAuthority || !_isRunning)
                return;

            float deltaTime = Runner != null ? Runner.DeltaTime : Time.deltaTime;
            ElapsedTime += deltaTime;
            PhaseElapsedTime += deltaTime;

            if (Phase == RoundPhase.Maintenance && PhaseElapsedTime >= _maintenanceDuration)
                StartRound();
            else if (Phase == RoundPhase.Combat && PhaseElapsedTime >= _roundDuration)
                EndRound();
        }

        private void ResetRoundState()
        {
            ElapsedTime = 0f;
            RoundNumber = 1;
            Phase = RoundPhase.Maintenance;
            PhaseElapsedTime = 0f;
            IsBerserk = false;
            _isRunning = false;
        }

        private void StartRound()
        {
            Phase = RoundPhase.Combat;
            PhaseElapsedTime = 0f;

            Debug.Log($"Round {RoundNumber} started.");
            OnRoundStarting?.Invoke(RoundNumber, this, this);
        }

        private void EndRound()
        {
            int endedRound = RoundNumber;
            Debug.Log($"Round {endedRound} ended.");
            OnRoundEnded?.Invoke(endedRound, this, this);

            if (!IsBerserk && endedRound >= _defaultRoundCount)
            {
                IsBerserk = true;
                Debug.Log("Berserk mode started.");
                OnBerserkStarted?.Invoke(this, this);
            }

            RoundNumber++;
            Phase = RoundPhase.Maintenance;
            PhaseElapsedTime = 0f;

            Debug.Log($"Round maintenance started. Next round: {RoundNumber}");
            OnMaintenanceStarting?.Invoke(RoundNumber, this, this);
        }
    }
}
