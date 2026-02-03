using Code.Animals;
using Code.Animals.Health;
using UnityEngine;

namespace Code.Abilities
{
    public class CounterAttack : IAbility
    {
        private readonly AnimalHealth _health;
        private readonly AnimalAnimator _animator;
        private readonly AnimalAttack _attack;
        private readonly bool _isOwner;

        public bool IsBlockingDamage => false;
        public int Priority => 0;

        public bool CanUse
        {
            get
            {
                // Owner: always 100%
                if (_isOwner) return true;

                // Inherited: 50%
                return Random.Range(0, 100) < 50;
            }
        }

        public CounterAttack(AnimalHealth health, AnimalAnimator animator, AnimalAttack attack, bool isOwner)
        {
            _health = health;
            _animator = animator;
            _attack = attack;
            _isOwner = isOwner;
        }

        public void Apply()
        {
            _animator.CounterAttackAnimation();

            _health.LastAttack.GetComponent<IDamageable>().TakeDamage(_attack);
        }
    }
}