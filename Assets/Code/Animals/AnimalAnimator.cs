using System;
using System.Collections.Generic;
using UnityEngine;

namespace Code.Animals
{
    public class AnimalAnimator : MonoBehaviour
    {
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int TakeDamage = Animator.StringToHash("TakeDamage");
        private static readonly int IsDead = Animator.StringToHash("IsDead");

        [SerializeField] private Animator _animator;

        public void UpdateMovementAnimation(float moveSpeed) => _animator.SetFloat(MoveSpeed, moveSpeed);

        public void PlayAttackAnimation() => _animator.SetTrigger(Attack);

        public void TakeDamageAnimation() => _animator.SetTrigger(TakeDamage);

        public void DeathAnimation() => _animator.SetTrigger(IsDead);


        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M))
            {
                UpdateMovementAnimation(2f);
            }
            else if (Input.GetKeyDown(KeyCode.N))
            {
                UpdateMovementAnimation(0f);
            }
            else if (Input.GetKeyDown(KeyCode.G))
            {
                PlayAttackAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                TakeDamageAnimation();
            }
            else if (Input.GetKeyDown(KeyCode.J))
            {
                DeathAnimation();
            }
        }
    }
}