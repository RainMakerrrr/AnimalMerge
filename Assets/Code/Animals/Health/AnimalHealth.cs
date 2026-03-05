using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using UnityEngine;
using UnityEngine.Events;
using Zenject;

namespace Code.Animals.Health
{
    public class AnimalHealth : MonoBehaviour, IDamageable
    {
        // Events for code subscriptions (UI, VFX, sound effects)
        public event Action TakenDamage;
        public event Action Died;

        [SerializeField] private float _max;
        [SerializeField] protected AnimalAnimator _animator;

        public IAbility Ability { get; protected set; }

        protected List<IAbility> MergedAbilities = new List<IAbility>();

        // AbilityManager for centralized ability management
        protected AbilityManager _abilityManager;

        public float Current { get; protected set; }
        public float Max { get; protected set; }

        public bool IsDead => Current <= 0;

        private Collider[] _colliders;

        public void Construct(Collider[] colliders)
        {
            _colliders = colliders;
        }

        public void Upgrade(float multiplier)
        {
            _max *= multiplier;
            Max = _max;      // Update public property
            Current = _max;  // Fully heal to new max
        }

        public void SetMaxHealth(float newMaxHealth)
        {
            Max = newMaxHealth;
            _max = newMaxHealth;
            Current = Max; // Full heal when setting new maximum
        }

        public void SetAbility(IAbility ability)
        {
            Ability = ability;

            // Ensure AbilityManager exists (for tests where Start() might not be called)
            _abilityManager ??= new AbilityManager();

            // Register in AbilityManager
            if (ability != null)
            {
                _abilityManager.RegisterAbility(ability);
                Debug.Log($"[AnimalHealth] Registered primary ability in AbilityManager: {ability.GetType().Name}");
            }
        }

        public void AddAbility(IAbility ability)
        {
            MergedAbilities.Add(ability);

            // Ensure AbilityManager exists (for tests where Start() might not be called)
            _abilityManager ??= new AbilityManager();

            // Register in AbilityManager
            if (ability != null)
            {
                _abilityManager.RegisterAbility(ability);
                Debug.Log($"[AnimalHealth] Registered merged ability in AbilityManager: {ability.GetType().Name}");
            }

            Debug.Log($"[AnimaHealth] add ability: {ability.GetType().Name}, {name}, abilities count - {MergedAbilities.Count}, my ability  {Ability?.GetType().Name}");
        }


        private void Start()
        {
            Max = _max;
            Current = Max;
            
            _abilityManager ??= new AbilityManager();
        }

        public virtual async Task TakeDamageAsync(AnimalAttack attacker)
        {
            Debug.Log($"[TakeDamage] {name} took {attacker.Damage} damage from {attacker.name}");

            bool isBlockedDamage = await ApplyAbilities(attacker);

            if (isBlockedDamage)
            {
                Debug.Log($"[AnimaHealth] {name} Damage blocked, return");
                return;
            }

            Debug.Log($"[AnimaHealth] {name} Damage taken");

            Current -= attacker.Damage;

            // Fire event when damage is actually applied
            TakenDamage?.Invoke();

            _animator.TakeDamageAnimation();

            if (IsDead)
            {
                Die();
            }
        }

        protected virtual async Task<bool> ApplyAbilities(AnimalAttack attacker)
        {
            // Ensure AbilityManager exists (lazy initialization for edge cases)
            _abilityManager ??= new AbilityManager();

            // Use AbilityManager for centralized ability execution
            var context = new AbilityContext(attacker, this, attacker.Damage);
            bool isBlocked = await _abilityManager.ExecuteAbilitiesAsync(context);
            return isBlocked;
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

        public void Restore(float amount)
        {
            Current = Mathf.Min(Current + amount, Max);
            Debug.Log($"[AnimalHealth] {name} restored {amount} health, now at {Current}/{Max}");
        }
    }
}