using System.Threading.Tasks;
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

        public bool CanUse(AnimalAttack attacker)
        {
            // CounterAttack does NOT work against AoE attacks
            if (attacker != null && attacker.IsAoE)
            {
                Debug.Log("[CounterAttack] Cannot counter-attack AoE attack");
                return false;
            }

            // Owner: always 100%
            if (_isOwner) return true;

            // Inherited: 50%
            return Random.Range(0, 100) < 50;
        }

        public CounterAttack(AnimalHealth health, AnimalAnimator animator, AnimalAttack attack, bool isOwner)
        {
            _health = health;
            _animator = animator;
            _attack = attack;
            _isOwner = isOwner;
        }

        public async Task Apply()
        {
            if (_animator != null)
            {
                _animator.CounterAttackAnimation();
            }

            if (_health.LastAttack == null)
            {
                Debug.LogWarning("[CounterAttack] LastAttack is null, cannot counter-attack");
                return;
            }

            // Try to find IDamageable on attacker (same GameObject, children, or parent)
            var attackerHealth = _health.LastAttack.GetComponent<IDamageable>();
            if (attackerHealth == null)
                attackerHealth = _health.LastAttack.GetComponentInChildren<IDamageable>();
            if (attackerHealth == null)
                attackerHealth = _health.LastAttack.GetComponentInParent<IDamageable>();

            if (attackerHealth == null)
            {
                Debug.LogWarning("[CounterAttack] Attacker has no IDamageable component");
                return;
            }

            await attackerHealth.TakeDamageAsync(_attack);
        }
    }
}