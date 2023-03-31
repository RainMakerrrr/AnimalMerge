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
        private readonly int _probability;

        public bool IsBlockingDamage => false;
        public int Priority => 0;
        public bool CanUse => Random.Range(0, 2) > _probability;

        public CounterAttack(AnimalHealth health, AnimalAnimator animator, int probability, AnimalAttack attack)
        {
            _health = health;
            _animator = animator;
            _probability = probability;
            _attack = attack;
        }

        public void Apply()
        {
            _animator.CounterAttackAnimation();

            _health.LastAttack.GetComponent<IDamageable>().TakeDamage(_attack);
        }
    }
}