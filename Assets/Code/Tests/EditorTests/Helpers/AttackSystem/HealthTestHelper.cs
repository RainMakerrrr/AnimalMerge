using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using NSubstitute;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers.AttackSystem
{
    public static class HealthTestHelper
    {
        /// <summary>
        /// Creates a real AnimalHealth instance for testing
        /// </summary>
        public static AnimalHealth CreateAnimalHealth(
            float maxHealth = 100f,
            IAbility ownAbility = null,
            params IAbility[] mergedAbilities)
        {
            var go = new GameObject("TestAnimalHealth");
            var health = go.AddComponent<AnimalHealth>();

            // Create mock animator
            var animatorGO = new GameObject("MockAnimator");
            animatorGO.transform.SetParent(go.transform);
            var animator = animatorGO.AddComponent<AnimalAnimator>();

            // Create mock collider
            var collider = go.AddComponent<BoxCollider>();
            health.Construct(new Collider[] { collider });

            // Use reflection to set private _max field
            var maxField = typeof(AnimalHealth).GetField("_max",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            maxField?.SetValue(health, maxHealth);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(health, animator);

            // Initialize health
            health.SetMaxHealth(maxHealth);

            // Set abilities
            if (ownAbility != null)
            {
                health.SetAbility(ownAbility);
            }

            foreach (var ability in mergedAbilities)
            {
                health.AddAbility(ability);
            }

            return health;
        }

        /// <summary>
        /// Creates a mock IAbility for testing
        /// </summary>
        public static IAbility CreateMockAbility(
            bool canUse = true,
            bool isBlocking = false,
            int priority = 0,
            Action onApply = null)
        {
            var ability = Substitute.For<IAbility>();

            ability.CanUse.Returns(canUse);
            ability.IsBlockingDamage.Returns(isBlocking);
            ability.Priority.Returns(priority);

            ability.Apply().Returns(callInfo =>
            {
                onApply?.Invoke();
                return Task.CompletedTask;
            });

            return ability;
        }

        /// <summary>
        /// Creates a mock Dodge ability
        /// </summary>
        public static IAbility CreateMockDodge(bool canUse = true, Action onApply = null)
        {
            return CreateMockAbility(
                canUse: canUse,
                isBlocking: true,
                priority: 1,
                onApply: onApply);
        }

        /// <summary>
        /// Creates a mock CounterAttack ability
        /// </summary>
        public static IAbility CreateMockCounterAttack(bool canUse = true, Action onApply = null)
        {
            return CreateMockAbility(
                canUse: canUse,
                isBlocking: false,
                priority: 0,
                onApply: onApply);
        }

        /// <summary>
        /// Event tracker utility for verifying events were fired
        /// </summary>
        public class EventTracker<T>
        {
            private readonly List<T> _events = new List<T>();

            public int CallCount => _events.Count;
            public List<T> Events => _events;
            public T LastEvent => _events.Count > 0 ? _events[_events.Count - 1] : default;

            public void Track(T eventData)
            {
                _events.Add(eventData);
            }

            public void Reset()
            {
                _events.Clear();
            }
        }

        /// <summary>
        /// Tracks parameterless events (like TakenDamage, Died)
        /// </summary>
        public class EventTracker
        {
            private int _callCount;

            public int CallCount => _callCount;

            public void Track()
            {
                _callCount++;
            }

            public void Reset()
            {
                _callCount = 0;
            }
        }
    }
}
