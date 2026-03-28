using System;
using UnityEngine;

namespace Dev.Local
{
    public class TimeSystem : System
    {
        private float _time;

        public event Action<float, TimeSystem, object> OnTimeChanged;

        protected override void OnInitialize()
        {
            _time = 0f;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            OnTimeChanged?.Invoke(_time, this, this);
        }
    }
}