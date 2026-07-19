using Dev.Network;
using UnityEngine;

namespace KIM.Dev
{
    public class TimerUI : MonoBehaviour
    {
        [SerializeField] private GameTimerUI _gameTimerUI;
        [SerializeField] private WaveTimerUI _waveTimerUI;

        private TimeSystem _timeSystem;

        private bool _isInitialized = false;

        private void Update()
        {
            if (!_isInitialized)
                return;

            _gameTimerUI?.SetElapsedTime(_timeSystem.ElapsedTime);

            float remainingTime = _timeSystem.Phase == RoundPhase.Combat
                ? Mathf.Max(0f, _timeSystem.RoundDuration - _timeSystem.PhaseElapsedTime)
                : 0f;

            _waveTimerUI?.SetRemainingTime(remainingTime);
        }

        public void InitializeTimerUI(TimeSystem timeSystem)
        {
            _timeSystem = timeSystem;

            _gameTimerUI?.SetElapsedTime(0f);
            _waveTimerUI?.SetRemainingTime(0f);

            _isInitialized = true;
        }
    }
}
