using Cysharp.Threading.Tasks;
using Code.Animals;
using Code.Animals.Health;
using Code.Services.Random;
using UnityEngine;

namespace Code.Abilities
{
    public class CounterAttack : IAbility
    {
        private readonly AnimalHealth _health;
        private readonly AnimalAnimator _animator;
        private readonly AnimalAttack _attack;
        private readonly bool _isOwner;
        private readonly IRandomProvider _randomProvider;

        // Store attacker from CanUse to use in Apply
        private IAttacker _currentAttacker;

        public bool IsBlockingDamage => false;
        public int Priority => 0;

        public bool CanUse(IAttacker attacker)
        {
            // Store attacker for later use in Apply()
            _currentAttacker = attacker;

            // CounterAttack does NOT work against AoE attacks
            if (attacker != null && attacker.IsAoE)
            {
                Debug.Log("[CounterAttack] Cannot counter-attack AoE attack");
                return false;
            }

            // Owner: always 100%
            if (_isOwner) return true;

            // Inherited: 50%
            return _randomProvider.Range(0, 100) < 50;
        }

        public CounterAttack(AnimalHealth health, AnimalAnimator animator, AnimalAttack attack, bool isOwner, IRandomProvider randomProvider)
        {
            _health = health;
            _animator = animator;
            _attack = attack;
            _isOwner = isOwner;
            _randomProvider = randomProvider;
        }

        public async UniTask Apply()
        {
            if (_animator != null)
            {
                _animator.CounterAttackAnimation();
            }

            // Use _currentAttacker stored in CanUse()
            if (_currentAttacker == null)
            {
                Debug.LogWarning("[CounterAttack] CurrentAttacker is null, cannot counter-attack");
                return;
            }

            // Get IDamageable from IAttacker interface
            var attackerHealth = _currentAttacker.Damageable;
            if (attackerHealth == null)
            {
                Debug.LogWarning("[CounterAttack] Attacker has no IDamageable component");
                return;
            }

            await attackerHealth.TakeDamageAsync(_attack);
        }
    }
}