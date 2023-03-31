using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;

namespace Code.Animals.Health
{
    public class AnimalHealth : MonoBehaviour, IDamageable
    {
        public event Action TakenDamage;
        public event Action Died;

        [SerializeField] private float _max;
        [SerializeField] protected AnimalAnimator _animator;

        public IAbility Ability { get; protected set; }

        protected List<IAbility> MergedAbilities = new List<IAbility>();

        public float Current { get; private set; }
        public float Max { get; private set; }

        public bool IsDead => Current <= 0;

        private Collider[] _colliders;

        public AnimalAttack LastAttack { get; protected set; }

        public void Construct(Collider[] colliders)
        {
            _colliders = colliders;
        }

        public void Upgrade(float multiplier)
        {
            _max *= multiplier;
            Current = _max;
        }

        public void SetAbility(IAbility ability) => Ability = ability;

        public void AddAbility(IAbility ability)
        {
            MergedAbilities.Add(ability);
        }


        private void Start()
        {
            Max = _max;
            Current = Max;
        }

        public virtual async void TakeDamage(AnimalAttack attacker)
        {
            LastAttack = attacker;

            bool isBlockedDamage = await ApplyAbilities();

            if (isBlockedDamage)
            {
                EnableColliders();
                return;
            }
            
            Current -= attacker.Damage;
            TakenDamage?.Invoke();

            _animator.TakeDamageAnimation();

            if (IsDead)
            {
                Die();
            }
        }

        private void EnableColliders()
        {
            foreach (Collider col in _colliders)
            {
                col.enabled = true;
            }
        }

        private async Task<bool> ApplyAbilities()
        {
            List<IAbility> abilities = new List<IAbility>(MergedAbilities) {Ability};
            abilities = abilities.Where(a => a != null).ToList();

            bool isBlockedDamage = false;
            
            foreach (IAbility ability in abilities.OrderBy(a => a.Priority))
            {
                if (ability.CanUse)
                {
                    if (ability.IsBlockingDamage)
                    {
                        isBlockedDamage = true;
                    }
                    
                    ability.Apply();
                    await Task.Delay(TimeSpan.FromSeconds(1.3f));
                }
            }

            return isBlockedDamage;
        }

        private void Die()
        {
            Debug.Log("Die");
            Died?.Invoke();

            _animator.DeathAnimation();

            StartCoroutine(DestroyWithDelay());
        }

        private IEnumerator DestroyWithDelay()
        {
            yield return new WaitForSeconds(3f);
            
            Destroy(gameObject);
        }
    }
}