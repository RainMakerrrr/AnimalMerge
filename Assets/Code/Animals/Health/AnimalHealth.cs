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

        // List of abilities added through merges (for tracking purposes)
        // AbilityManager (owned by AnimalFacade) is the source of truth for registered abilities
        public List<IAbility> MergedAbilities { get; } = new List<IAbility>();

        // AbilityManager reference (owned by AnimalFacade, passed via Construct)
        protected AbilityManager _abilityManager;

        public float Current { get; protected set; }
        public float Max { get; protected set; }

        public bool IsDead => Current <= 0;

        private Collider[] _colliders;

        /// <summary>
        /// Constructs AnimalHealth with required dependencies.
        /// AbilityManager is owned by AnimalFacade and passed here for use.
        /// </summary>
        public void Construct(Collider[] colliders, AbilityManager abilityManager)
        {
            _colliders = colliders;
            _abilityManager = abilityManager;
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

        /// <summary>
        /// Sets the current health value directly (for undo operations)
        /// </summary>
        public void SetCurrentHealth(float value)
        {
            Current = Mathf.Clamp(value, 0f, Max);
        }

        public void SetAbility(IAbility ability)
        {
            Ability = ability;

            // Ensure AbilityManager exists (fallback for tests where Construct() might not be called)
            if (_abilityManager == null)
            {
                Debug.LogWarning("[AnimalHealth] AbilityManager is null! Creating fallback instance. This should only happen in tests.");
                _abilityManager = new AbilityManager();
            }

            // Register in AbilityManager
            if (ability != null)
            {
                _abilityManager.RegisterAbility(ability);
                Debug.Log($"[AnimalHealth] Registered primary ability: {ability.GetType().Name}");
            }
        }

        public void AddAbility(IAbility ability)
        {
            MergedAbilities.Add(ability);

            // Ensure AbilityManager exists (fallback for tests where Construct() might not be called)
            if (_abilityManager == null)
            {
                Debug.LogWarning("[AnimalHealth] AbilityManager is null! Creating fallback instance. This should only happen in tests.");
                _abilityManager = new AbilityManager();
            }

            // Register in AbilityManager
            if (ability != null)
            {
                _abilityManager.RegisterAbility(ability);
                Debug.Log($"[AnimalHealth] Registered merged ability: {ability.GetType().Name}");
            }

            Debug.Log($"[AnimalHealth] Add ability: {ability.GetType().Name}, abilities count: {MergedAbilities.Count}");
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

            // Use AbilityManager to execute only defensive abilities (exclude post-attack abilities)
            var context = new AbilityContext(attacker, this, attacker.Damage);
            bool isBlocked = await _abilityManager.ExecuteAbilitiesExceptTypeAsync<IPostAttackAbility>(context);
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