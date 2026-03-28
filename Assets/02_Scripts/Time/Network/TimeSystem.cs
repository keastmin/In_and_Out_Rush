using System;
using Dev.Local;
using Fusion;
using UnityEngine;

namespace Dev.Network
{
    public class TimeSystem : System
    {
        #region Temporary
        [Header("Wave Settings")]
        [SerializeField] private float[] _waveThresholds;

        [Header("Track Settings")]
        [SerializeField] private float[] _trackLevelThresholds;
        #endregion

        [Networked] public float ElapsedTime { get; set; } // 클라이언트 측에 동기화되는 경과 시간(단순 표시 용도)

        private float _elapsedTime; // 서버 측에서 실제로 계산되는 경과 시간(클라이언트에는 동기화되지 않음)
        private RangeEvaluator _waveRangeEvaluator;
        private RangeEvaluator _trackLevelRangeEvaluator;

        public event Action<int, TimeSystem, object> OnNextWaveReached;
        public event Action<int, TimeSystem, object> OnNextTrackLevelReached;

        protected override void OnInitialize()
        {
            ElapsedTime = 0f;
            _elapsedTime = 0f;

            _waveRangeEvaluator = new RangeEvaluator(_waveThresholds);
            _waveRangeEvaluator.OnNextRangeReached += HandleNextWaveRangeReached;
            _trackLevelRangeEvaluator = new RangeEvaluator(_trackLevelThresholds);
            _trackLevelRangeEvaluator.OnNextRangeReached += HandleNextTrackLevelRangeReached;
        }

        private void HandleNextWaveRangeReached(int waveIndex, RangeEvaluator waveRangeEvaluator, object sender)
        {
            StageInstance.Instance.WaveIndex = waveIndex;
            OnNextWaveReached?.Invoke(waveIndex, this, sender);
        }

        private void HandleNextTrackLevelRangeReached(int trackLevelIndex, RangeEvaluator trackLevelRangeEvaluator, object sender)
        {
            StageInstance.Instance.TrackLevelIndex = trackLevelIndex;
            OnNextTrackLevelReached?.Invoke(trackLevelIndex, this, sender);
        }

        private void Update()
        {
            if (Object.HasStateAuthority)
            {
                _elapsedTime += Time.deltaTime;
                _waveRangeEvaluator.Evaluate(_elapsedTime);
                _trackLevelRangeEvaluator.Evaluate(_elapsedTime);
            }
        }

        public override void FixedUpdateNetwork()
        {
            if (Object.HasStateAuthority)
            {
                ElapsedTime = _elapsedTime;
            }
        }
    }
}