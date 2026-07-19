using TMPro;
using UnityEngine;

namespace KIM.Dev
{
    public class GameTimerUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _gameTimerText;

        private int _displayedSecond = -1;

        public void SetElapsedTime(float elapsedTime)
        {
            int totalSeconds = Mathf.FloorToInt(Mathf.Max(0f, elapsedTime));
            if (_gameTimerText == null || totalSeconds == _displayedSecond)
                return;

            _displayedSecond = totalSeconds;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            _gameTimerText.text = $"{minutes:D2}:{seconds:D2}";
        }
    }
}
