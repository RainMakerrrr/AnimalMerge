using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Health;
using NSubstitute;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers.AttackSystem
{
    public static class AttackTestHelper
    {
        /// <summary>
        /// Creates a mock AnimalAttack with specified damage
        /// </summary>
        public static AnimalAttack CreateMockAttack(float damage, AnimalType type = AnimalType.Cheetah)
        {
            var attackGO = new GameObject("MockAttack");
            var attack = attackGO.AddComponent<AnimalAttack>();

            var animal = attackGO.AddComponent<Animal>();

            // Set animal type via reflection since it's read-only
            var typeField = typeof(Animal).GetField("_type",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (typeField != null)
            {
                typeField.SetValue(animal, type);
            }

            // Create animator child object
            var animatorGO = new GameObject("Animator");
            animatorGO.transform.SetParent(attackGO.transform);
            var animator = animatorGO.AddComponent<AnimalAnimator>();

            // Create attack point child object
            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(attackGO.transform);

            // Set private fields via reflection
            var animatorField = typeof(AnimalAttack).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(attack, animator);

            var attackPointField = typeof(AnimalAttack).GetField("_attackPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attackPointField?.SetValue(attack, attackPointGO.transform);

            attack.SetDamage(damage);

            return attack;
        }

        /// <summary>
        /// Creates a mock collider with IDamageable component
        /// </summary>
        public static Collider CreateColliderWithDamageable(IDamageable damageable, string name = "MockCollider")
        {
            var go = new GameObject(name);
            var collider = go.AddComponent<BoxCollider>();

            // If damageable is a MonoBehaviour mock, attach it
            if (damageable is MonoBehaviour mb)
            {
                // Mock is already on the object
            }
            else
            {
                // Create a wrapper component that implements IDamageable
                var wrapper = go.AddComponent<DamageableWrapper>();
                wrapper.SetMockDamageable(damageable);
            }

            return collider;
        }

        /// <summary>
        /// Creates multiple colliders for the same target (for HashSet deduplication tests)
        /// </summary>
        public static List<Collider> CreateMultipleCollidersForTarget(
            IDamageable target,
            int colliderCount,
            string baseName = "TargetPart")
        {
            var colliders = new List<Collider>();

            // Create parent object with IDamageable
            var parentGO = new GameObject($"{baseName}_Parent");

            // Add mock damageable to parent
            if (target is MonoBehaviour mb)
            {
                // Mock setup
            }
            else
            {
                var wrapper = parentGO.AddComponent<DamageableWrapper>();
                wrapper.SetMockDamageable(target);
            }

            // Create child colliders
            for (int i = 0; i < colliderCount; i++)
            {
                var childGO = new GameObject($"{baseName}_{i}");
                childGO.transform.SetParent(parentGO.transform);
                var collider = childGO.AddComponent<BoxCollider>();
                colliders.Add(collider);
            }

            return colliders;
        }

        /// <summary>
        /// Creates a mock IDamageable that tracks TakeDamageAsync calls
        /// </summary>
        public static MockDamageable CreateMockDamageable(float maxHealth = 100f)
        {
            var go = new GameObject("MockDamageable");
            var mockDamageable = go.AddComponent<MockDamageable>();

            // Add BoxCollider so Physics can detect it
            go.AddComponent<BoxCollider>();

            mockDamageable.Initialize(maxHealth);
            return mockDamageable;
        }

        /// <summary>
        /// Helper component to wrap non-MonoBehaviour IDamageable mocks
        /// </summary>
        private class DamageableWrapper : MonoBehaviour, IDamageable
        {
            private IDamageable _mockDamageable;

            public void SetMockDamageable(IDamageable mock)
            {
                _mockDamageable = mock;
            }

            public float Current => _mockDamageable.Current;
            public float Max => _mockDamageable.Max;
            public bool IsDead => _mockDamageable.IsDead;

            public Task TakeDamageAsync(AnimalAttack attacker)
            {
                return _mockDamageable.TakeDamageAsync(attacker);
            }
        }
    }

    /// <summary>
    /// Simple mock implementation of IDamageable for testing
    /// </summary>
    public class MockDamageable : MonoBehaviour, IDamageable
    {
        public float Current { get; private set; }
        public float Max { get; private set; }
        public bool IsDead => Current <= 0;

        public int TakeDamageCallCount { get; private set; }
        public List<float> DamageReceived { get; private set; } = new List<float>();
        public List<AnimalAttack> Attackers { get; private set; } = new List<AnimalAttack>();

        public void Initialize(float maxHealth)
        {
            Max = maxHealth;
            Current = maxHealth;
        }

        public async Task TakeDamageAsync(AnimalAttack attacker)
        {
            TakeDamageCallCount++;
            DamageReceived.Add(attacker.Damage);
            Attackers.Add(attacker);

            Current -= attacker.Damage;

            await Task.CompletedTask;
        }

        public void Reset()
        {
            TakeDamageCallCount = 0;
            DamageReceived.Clear();
            Attackers.Clear();
            Current = Max;
        }
    }
}
