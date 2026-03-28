using System;
using System.Collections.Generic;
using System.Linq;

namespace Dev
{
    public class RangeEvaluator
    {
        private readonly float[] _thresholds;
        private int _currentRangeIndex;

        public event Action<int, RangeEvaluator, object> OnNextRangeReached;

        public RangeEvaluator(IEnumerable<float> thresholds)
        {
            _thresholds = thresholds.OrderBy(x => x).ToArray();
            _currentRangeIndex = 0;
        }

        public void Evaluate(float value)
        {
            if (_thresholds == null || _thresholds.Length == 0) return;
            if (_currentRangeIndex >= _thresholds.Length) return;

            for (int i = _currentRangeIndex; i < _thresholds.Length; i++)
            {
                if (_thresholds[i] <= value)
                {
                    _currentRangeIndex = i + 1;
                    OnNextRangeReached?.Invoke(i, this, this);
                }
            }
        }
    }
}