using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Abilities
{
    /// <summary>
    /// Manages a collection of abilities and handles their execution.
    /// Abilities are executed in priority order (higher priority first).
    /// At most one <see cref="IMutuallyExclusiveAbility"/> is applied per execution:
    /// once one of them is applied, the remaining ones are skipped.
    /// </summary>
    public class AbilityManager
    {
        private readonly List<IAbility> _abilities = new List<IAbility>();
        private List<IAbility> _sortedAbilities;

        /// <summary>
        /// Gets all registered abilities.
        /// </summary>
        public IReadOnlyList<IAbility> Abilities => _abilities;

        /// <summary>
        /// Registers a new ability with this manager.
        /// The ability will be included in execution based on its priority.
        /// </summary>
        /// <param name="ability">The ability to register.</param>
        public void RegisterAbility(IAbility ability)
        {
            if (ability == null)
            {
                Debug.LogWarning("[AbilityManager] Attempted to register null ability");
                return;
            }

            if (_abilities.Contains(ability))
            {
                Debug.LogWarning($"[AbilityManager] Ability {ability.GetType().Name} is already registered");
                return;
            }

            _abilities.Add(ability);
            _sortedAbilities = null; // Invalidate cache
            Debug.Log($"[AbilityManager] Registered ability: {ability.GetType().Name} (Priority: {ability.Priority})");
        }

        /// <summary>
        /// Unregisters an ability from this manager.
        /// </summary>
        /// <param name="ability">The ability to unregister.</param>
        /// <returns>True if the ability was found and removed, false otherwise.</returns>
        public bool UnregisterAbility(IAbility ability)
        {
            if (ability == null)
            {
                return false;
            }

            var removed = _abilities.Remove(ability);
            if (removed)
            {
                _sortedAbilities = null; // Invalidate cache
                Debug.Log($"[AbilityManager] Unregistered ability: {ability.GetType().Name}");
            }

            return removed;
        }

        /// <summary>
        /// Clears all registered abilities.
        /// </summary>
        public void ClearAbilities()
        {
            _abilities.Clear();
            _sortedAbilities = null;
            Debug.Log("[AbilityManager] Cleared all abilities");
        }

        /// <summary>
        /// Executes all registered abilities in priority order.
        /// Higher priority abilities are executed first.
        /// Every ability that can be used (CanUse returns true) will execute, except mutually
        /// exclusive ones after the first of them has been applied.
        /// If any blocking ability is triggered, damage will be blocked (but other abilities still execute).
        /// </summary>
        /// <param name="context">The context containing information about the damage event.</param>
        /// <returns>True if damage was blocked by any ability, false otherwise.</returns>
        public async UniTask<bool> ExecuteAbilitiesAsync(AbilityContext context)
        {
            if (context == null)
            {
                Debug.LogWarning("[AbilityManager] ExecuteAbilitiesAsync called with null context");
                return false;
            }

            if (_abilities.Count == 0)
            {
                Debug.Log("[AbilityManager] No abilities registered");
                return false;
            }

            var sortedAbilities = GetSortedAbilities();

            Debug.Log($"[AbilityManager] Executing {sortedAbilities.Count} abilities");

            return await ApplyAbilitiesAsync(sortedAbilities, context);
        }

        /// <summary>
        /// Executes only abilities of the specified type in priority order.
        /// Higher priority abilities are executed first.
        /// </summary>
        /// <typeparam name="T">The type of abilities to execute (must implement IAbility).</typeparam>
        /// <param name="context">The context containing information about the damage event.</param>
        /// <returns>True if damage was blocked by any ability, false otherwise.</returns>
        public async UniTask<bool> ExecuteAbilitiesOfTypeAsync<T>(AbilityContext context) where T : IAbility
        {
            if (context == null)
            {
                Debug.LogWarning("[AbilityManager] ExecuteAbilitiesOfTypeAsync called with null context");
                return false;
            }

            var filteredAbilities = _abilities
                .OfType<T>()
                .OrderByDescending(a => a.Priority)
                .Cast<IAbility>()
                .ToList();

            if (filteredAbilities.Count == 0)
            {
                Debug.Log($"[AbilityManager] No abilities of type {typeof(T).Name} registered");
                return false;
            }

            Debug.Log($"[AbilityManager] Executing {filteredAbilities.Count} abilities of type {typeof(T).Name}");

            return await ApplyAbilitiesAsync(filteredAbilities, context);
        }

        /// <summary>
        /// Executes all abilities EXCEPT those of the specified type in priority order.
        /// Higher priority abilities are executed first.
        /// </summary>
        /// <typeparam name="T">The type of abilities to exclude (must implement IAbility).</typeparam>
        /// <param name="context">The context containing information about the damage event.</param>
        /// <returns>True if damage was blocked by any ability, false otherwise.</returns>
        public async UniTask<bool> ExecuteAbilitiesExceptTypeAsync<T>(AbilityContext context) where T : IAbility
        {
            if (context == null)
            {
                Debug.LogWarning("[AbilityManager] ExecuteAbilitiesExceptTypeAsync called with null context");
                return false;
            }

            var filteredAbilities = _abilities
                .Where(a => a is not T)
                .OrderByDescending(a => a.Priority)
                .ToList();

            if (filteredAbilities.Count == 0)
            {
                Debug.Log($"[AbilityManager] No abilities (excluding {typeof(T).Name}) registered");
                return false;
            }

            Debug.Log($"[AbilityManager] Executing {filteredAbilities.Count} abilities (excluding {typeof(T).Name})");

            return await ApplyAbilitiesAsync(filteredAbilities, context);
        }

        /// <summary>
        /// Checks if any registered ability would block the incoming damage.
        /// This is a non-destructive check that doesn't apply the abilities.
        /// </summary>
        /// <param name="context">The context to check.</param>
        /// <returns>True if any ability would block damage, false otherwise.</returns>
        public bool WouldBlockDamage(AbilityContext context)
        {
            if (context == null || _abilities.Count == 0)
            {
                return false;
            }

            foreach (var ability in GetSortedAbilities())
            {
                if (ability.CanUse(context.Attacker) && ability.IsBlockingDamage)
                {
                    return true;
                }
            }

            return false;
        }

        private List<IAbility> GetSortedAbilities()
        {
            return _sortedAbilities ??= _abilities.OrderByDescending(a => a.Priority).ToList();
        }

        private static async UniTask<bool> ApplyAbilitiesAsync(IReadOnlyList<IAbility> abilities, AbilityContext context)
        {
            bool damageBlocked = false;
            bool exclusiveApplied = false;

            foreach (var ability in abilities)
            {
                if (exclusiveApplied && ability is IMutuallyExclusiveAbility)
                {
                    Debug.Log($"[AbilityManager] {ability.GetType().Name} skipped (another mutually exclusive ability already applied)");
                    continue;
                }

                if (!ability.CanUse(context.Attacker))
                {
                    Debug.Log($"[AbilityManager] {ability.GetType().Name} cannot be used (CanUse returned false)");
                    continue;
                }

                Debug.Log($"[AbilityManager] Applying {ability.GetType().Name}");
                await ability.Apply();

                if (ability is IMutuallyExclusiveAbility)
                {
                    exclusiveApplied = true;
                }

                if (ability.IsBlockingDamage)
                {
                    Debug.Log($"[AbilityManager] {ability.GetType().Name} blocked damage");
                    damageBlocked = true;
                }
            }

            context.IsBlocked = damageBlocked;
            return damageBlocked;
        }
    }
}
