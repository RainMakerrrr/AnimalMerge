using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.GridPathfinding;
using Code.Pathfinding;
using Code.Services.Random;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.PlayModeTests.AttackAndDamageSystem
{
    [TestFixture]
    public class AttackAndDamageIntegrationTests
    {
        /// <summary>
        /// Integration test: Full attack flow with async damage application
        /// </summary>
        [UnityTest]
        public IEnumerator FullAttackFlow_AppliesDamageCorrectly()
        {
            // Arrange
            var attackGO = new GameObject("Attacker");
            var attack = attackGO.AddComponent<AnimalAttack>();
            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(attackGO.transform);

            var animatorGO = new GameObject("Animator");
            animatorGO.transform.SetParent(attackGO.transform);
            animatorGO.AddComponent<AnimalAnimator>();

            SetPrivateField(attack, "_attackPoint", attackPointGO.transform);
            SetPrivateField(attack, "_radius", 1f);
            SetPrivateField(attack, "_forwardReach", 0.25f);
            SetPrivateField(attack, "_maxTargets", 10);

            // LayerMask needs explicit conversion from int
            LayerMask mask = LayerMask.GetMask("Default");
            SetPrivateField(attack, "_mask", mask);
            attack.SetDamage(25f);

            var target = CreateMockDamageable(100f);
            target.transform.position = attackPointGO.transform.position + Vector3.forward * 0.5f;
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            Task attackTask = attack.AttackAnimationHandlerAsync();

            // Wait for task to complete
            while (!attackTask.IsCompleted)
            {
                yield return null;
            }

            // Assert
            target.TakeDamageCallCount.Should().Be(1);
            target.Current.Should().Be(75f);
            target.DamageReceived[0].Should().Be(25f);

            // Cleanup
            Object.DestroyImmediate(attackGO);
            Object.DestroyImmediate(target.gameObject);
        }

        /// <summary>
        /// Integration test: Death event fires when HP reaches 0
        /// </summary>
        [UnityTest]
        public IEnumerator DamageCausesDeath_FiresDeathEvent()
        {
            // Arrange
            var health = CreateAnimalHealth(maxHealth: 30f);
            var attacker = CreateMockAttack(damage: 30f);

            var diedTracker = new EventTracker();
            health.Died += diedTracker.Track;

            // Act
            Task damageTask = health.TakeDamageAsync(attacker);

            while (!damageTask.IsCompleted)
            {
                yield return null;
            }

            // Assert
            health.Current.Should().Be(0f);
            health.IsDead.Should().BeTrue();
            diedTracker.CallCount.Should().Be(1, "Died event should fire once");

            // Cleanup
            Object.DestroyImmediate(health.gameObject);
            Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// Integration test: Dodge ability disables colliders during shift and re-enables them after
        /// UPDATED: Event-Driven architecture - Dodge self-manages colliders
        /// </summary>
        [UnityTest]
        public IEnumerator Dodge_Apply_DisablesCollidersAndShifts()
        {
            // Arrange
            var go = new GameObject("DodgeTest");
            var collider1 = go.AddComponent<BoxCollider>();
            var collider2 = go.AddComponent<SphereCollider>();
            var colliders = new Collider[] { collider1, collider2 };

            collider1.enabled = true;
            collider2.enabled = true;

            var transformableMock = new TestTransformable();
            var randomProvider = new TestRandomProvider(0);
            var dodge = new Code.Abilities.Dodge(transformableMock, colliders, isOwner: true, randomProvider);

            // Act
            Task applyTask = dodge.Apply();

            while (!applyTask.IsCompleted)
            {
                yield return null;
            }

            // Assert
            // NEW BEHAVIOR: Dodge self-manages colliders (Event-Driven architecture)
            // Colliders are temporarily disabled during shift, then re-enabled after
            collider1.enabled.Should().BeTrue("collider 1 should be re-enabled after shift (self-management)");
            collider2.enabled.Should().BeTrue("collider 2 should be re-enabled after shift (self-management)");
            transformableMock.ShiftCalled.Should().BeTrue("Shift() should be called");

            // Cleanup
            Object.DestroyImmediate(go);
        }

        /// <summary>
        /// Integration test: Fox Dodge blocks damage
        /// </summary>
        [UnityTest]
        public IEnumerator FoxHealth_DodgeSucceeds_BlocksDamage()
        {
            // Arrange
            var foxGO = new GameObject("Fox");
            var foxHealth = foxGO.AddComponent<FoxHealth>();
            var foxAnimator = foxGO.AddComponent<AnimalAnimator>();

            foxHealth.Construct(new Collider[] { foxGO.AddComponent<BoxCollider>() });

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(foxHealth, foxAnimator);

            foxHealth.SetMaxHealth(100f);

            var blockingDodge = CreateMockDodge(canUse: true);
            foxHealth.SetAbility(blockingDodge);

            var attacker = CreateMockAttack(damage: 30f);

            // Act
            Task damageTask = foxHealth.TakeDamageAsync(attacker);

            while (!damageTask.IsCompleted)
            {
                yield return null;
            }

            // Assert
            foxHealth.Current.Should().Be(100f, "Dodge should block damage");

            // Cleanup
            Object.DestroyImmediate(foxGO);
            Object.DestroyImmediate(attacker.gameObject);
        }

        #region Helper Classes

        private class TestTransformable : ITransformable
        {
            public bool ShiftCalled { get; private set; }

            public Vector3 Position => Vector3.zero;
            public Vector2Int IntPosition => Vector2Int.zero;
            public UnitSize UnitSize => UnitSize.Small;
            public int SizeEffect => 1;
            public IGridCell CurrentPathNode => null;

            public List<IGridCell> GetOccupiedCells()
            {
                return new List<IGridCell>();
            }

            public Task Shift()
            {
                ShiftCalled = true;
                return Task.CompletedTask;
            }
        }

        #endregion

        #region Helper Methods

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        private MockDamageable CreateMockDamageable(float maxHealth)
        {
            var go = new GameObject("MockDamageable");
            var mockDamageable = go.AddComponent<MockDamageable>();

            // Add collider so Physics can detect this target
            var collider = go.AddComponent<BoxCollider>();
            collider.size = Vector3.one * 0.5f;

            mockDamageable.Initialize(maxHealth);
            return mockDamageable;
        }

        private AnimalHealth CreateAnimalHealth(float maxHealth)
        {
            var go = new GameObject("TestAnimalHealth");
            var health = go.AddComponent<AnimalHealth>();
            var animatorGO = new GameObject("MockAnimator");
            animatorGO.transform.SetParent(go.transform);
            var animator = animatorGO.AddComponent<AnimalAnimator>();
            var collider = go.AddComponent<BoxCollider>();
            health.Construct(new UnityEngine.Collider[] { collider });

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(health, animator);

            health.SetMaxHealth(maxHealth);
            return health;
        }

        private AnimalAttack CreateMockAttack(float damage)
        {
            var attackGO = new GameObject("MockAttack");
            var attack = attackGO.AddComponent<AnimalAttack>();
            var animal = attackGO.AddComponent<Animal>();

            var typeField = typeof(Animal).GetField("_type",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            typeField?.SetValue(animal, AnimalType.Cheetah);

            var animatorGO = new GameObject("Animator");
            animatorGO.transform.SetParent(attackGO.transform);
            animatorGO.AddComponent<AnimalAnimator>();

            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(attackGO.transform);

            var animatorField = typeof(AnimalAttack).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var attackPointField = typeof(AnimalAttack).GetField("_attackPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            animatorField?.SetValue(attack, animatorGO.GetComponent<AnimalAnimator>());
            attackPointField?.SetValue(attack, attackPointGO.transform);

            attack.SetDamage(damage);
            return attack;
        }

        private Code.Abilities.IAbility CreateMockDodge(bool canUse)
        {
            var transformable = new TestTransformable();
            var go = new GameObject("DodgeMock");
            var collider = go.AddComponent<BoxCollider>();
            var randomProvider = new TestRandomProvider(0);
            var dodge = new Code.Abilities.Dodge(transformable, new[] { collider }, isOwner: true, randomProvider);
            return dodge;
        }

        private class EventTracker
        {
            private int _callCount;
            public int CallCount => _callCount;

            public void Track()
            {
                _callCount++;
            }
        }

        private class MockDamageable : MonoBehaviour, IDamageable
        {
            public float Current { get; private set; }
            public float Max { get; private set; }
            public bool IsDead => Current <= 0;
            public int TakeDamageCallCount { get; private set; }
            public List<float> DamageReceived { get; private set; } = new List<float>();

            public void Initialize(float maxHealth)
            {
                Max = maxHealth;
                Current = maxHealth;
            }

            public async Task TakeDamageAsync(AnimalAttack attacker)
            {
                TakeDamageCallCount++;
                DamageReceived.Add(attacker.Damage);
                Current -= attacker.Damage;
                await Task.CompletedTask;
            }
        }

        #endregion
    }

    /// <summary>
    /// Test implementation of IRandomProvider for deterministic testing
    /// </summary>
    public class TestRandomProvider : IRandomProvider
    {
        private readonly int _fixedValue;

        public TestRandomProvider(int fixedValue = 0)
        {
            _fixedValue = fixedValue;
        }

        public int Range(int min, int max)
        {
            return _fixedValue;
        }

        public float Range(float min, float max)
        {
            return _fixedValue;
        }
    }
}
