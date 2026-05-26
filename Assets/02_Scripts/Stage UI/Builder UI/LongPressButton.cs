using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace KIM.Dev
{
    public class LongPressButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField][Min(0)] private float _pressTime; // 누르고 있을 시간
        [SerializeField] private UnityEvent _pressSuccessEvent; // 누르는 것에 성공했을 때 발동되는 이벤트
        [SerializeField] private Slider _effectSlider; // 누를 때의 효과를 나타낼 슬라이더

        private bool _isPressed = false;
        private float _currentTime = 0f; // 현재 누르고 있는 시간
        public float CurrentTime
        {
            get { return _currentTime; }
            set
            {
                _currentTime = value;
                SetSliderValue(_currentTime);
            }
        }

        private void OnEnable()
        {
            SetUpPress(false);
        }

        private void OnDisable()
        {
            SetUpPress(false);
        }

        private void Update()
        {
            if (_isPressed)
            {
                CurrentTime += Time.deltaTime;
                if (CurrentTime >= _pressTime)
                {
                    _pressSuccessEvent.Invoke();
                    SetUpPress(false);
                }
            }
        }

        // 마우스가 눌렸을 때
        public void OnPointerDown(PointerEventData eventData)
        {
            SetUpPress(true);
        }

        // 마우스가 버튼 밖으로 나갔을 때
        public void OnPointerExit(PointerEventData eventData)
        {
            SetUpPress(false);
        }

        // 마우스를 뗐을 때
        public void OnPointerUp(PointerEventData eventData)
        {
            SetUpPress(false);
        }

        // 버튼이 눌렸을 때의 설정
        public void SetUpPress(bool isPress)
        {
            if (_isPressed == isPress) return;

            CurrentTime = 0f;
            _isPressed = isPress;
        }

        // 슬라이더의 값을 설정
        private void SetSliderValue(float value)
        {
            if (_effectSlider != null)
                _effectSlider.value = value / _pressTime;
        }
    }

}