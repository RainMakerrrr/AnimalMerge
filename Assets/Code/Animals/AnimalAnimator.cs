using System;
using System.Threading.Tasks;
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
        [SerializeField] private AnimationClip _attackClip;

        public async Task WaitForAttackAnimation()
        {
            PlayAttackAnimation();

            await Task.Delay(TimeSpan.FromSeconds(_attackClip.length));
        }

        public void UpdateMovementAnimation(float moveSpeed) => _animator.SetFloat(MoveSpeed, moveSpeed);

        public void PlayAttackAnimation() => _animator.SetTrigger(Attack);

        public void TakeDamageAnimation() => _animator.SetTrigger(TakeDamage);

        public void DeathAnimation() => _animator.SetTrigger(IsDead);
        
    }
}