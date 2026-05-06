using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Animals
{
    public class AnimalAnimator : MonoBehaviour
    {
        private static readonly int MoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int Attack = Animator.StringToHash("Attack");
        private static readonly int TakeDamage = Animator.StringToHash("TakeDamage");
        private static readonly int IsDead = Animator.StringToHash("IsDead");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int CounterAttack = Animator.StringToHash("CounterAttack");
        private static readonly int IsFlapping = Animator.StringToHash("IsFlapping");
        private static readonly int TurnDirection = Animator.StringToHash("TurnDirection");

        [SerializeField] private Animator _animator;
        [SerializeField] private AnimationClip _attackClip;

        private void Awake()
        {
            if (_animator != null)
                _animator.SetFloat(TurnDirection, 0f);
        }

        public async UniTask WaitForAttackAnimation()
        {
            PlayAttackAnimation();

            await UniTask.Delay(TimeSpan.FromSeconds(_attackClip.length));
        }

        public void UpdateMovementAnimation(float moveSpeed)
        {
            if (_animator != null)
                _animator.SetFloat(MoveSpeed, moveSpeed);
        }

        public void PlayAttackAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger(Attack);
        }

        public void TakeDamageAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger(TakeDamage);
        }

        public void DeathAnimation()
        {
            if (_animator != null)
                _animator.SetBool(IsDead, true);
        }

        public void JumpAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger(Jump);
        }

        public void CounterAttackAnimation()
        {
            if (_animator != null)
                _animator.SetTrigger(CounterAttack);
        }

        public void SetEnableFlappingAnimation(bool enable)
        {
            if(_animator != null)
                _animator.SetBool(IsFlapping, enable);
        }

        public void UpdateTurnDirection(float direction)
        {
            if (_animator == null) return;

            // Clamp to [-1, 1]
            //Debug.Log($"[TurnAnimation] name - {name}, direction - {direction}");
            var clampedDirection = Mathf.Clamp(direction, -1f, 1f);
            _animator.SetFloat(TurnDirection, clampedDirection);
        }
    }
}