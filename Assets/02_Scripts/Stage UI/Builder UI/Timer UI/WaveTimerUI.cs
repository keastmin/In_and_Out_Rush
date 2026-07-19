using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class WaveTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _waveTimerText;

        private int _displayedSecond = -1;

        public void SetRemainingTime(float remainingTime)
        {
            int totalSeconds = Mathf.CeilToInt(Mathf.Max(0f, remainingTime));
            if (_waveTimerText == null || totalSeconds == _displayedSecond)
                return;

            _displayedSecond = totalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _waveTimerText.text = $"{minutes:D2}:{seconds:D2}";
        }
    }
}
