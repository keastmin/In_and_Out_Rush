using System;
using UnityEngine;
using UnityEngine.UI;

namespace Dev.Local
{
    public class StageResultView : View
    {
        [SerializeField] private Button _nextButton;

        public event Action OnNextButtonClicked;

        protected override void OnInitialize()
        {
            _nextButton.onClick.AddListener(() => OnNextButtonClicked?.Invoke());
        }

        public void ClearNextButtonListener()
        {
            OnNextButtonClicked = null;
        }
    }
}