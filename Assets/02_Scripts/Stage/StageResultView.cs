using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dev
{
    public class StageResultView : MonoBehaviour
    {
        [SerializeField] private Button _nextButton;

        public event Action OnNextButtonClicked;

        private void Awake()
        {
            _nextButton.onClick.AddListener(() => OnNextButtonClicked?.Invoke());
        }

        public void ClearNextButtonListener()
        {
            _nextButton.onClick.RemoveAllListeners();
        }
    }
}