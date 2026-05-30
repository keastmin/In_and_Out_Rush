using System;
using UnityEngine;

namespace KIM.Dev
{
    public class Blade : MonoBehaviour
    {
        [SerializeField] Animator animator;
        public float SpinSpeed = 1f;
        public float Damage = 10f;
        public bool CanHit;

        public event Action<bool> OnSpinEvent;

        void Awake()
        {
            CanHit = false;
        }

        public void StartSpinAnimation()
        {
            animator.SetTrigger("Spin");
        }

        public void StartSpin()
        {
            animator.SetFloat("Blade Spin Speed", SpinSpeed);
            CanHit = true;
            OnSpinEvent?.Invoke(CanHit);
        }

        public void StopSpin()
        {
            CanHit = false;
            OnSpinEvent?.Invoke(CanHit);
        }
    } 
}