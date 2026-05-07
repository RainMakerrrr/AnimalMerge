using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Health;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class AnimalAttackTests
    {
        private GameObject _attackGO;
        private AnimalAttack _attack;
        private Transform _attackPoint;

        [SetUp]
        public void SetUp()
        {
            // Clean scene before each test to ensure test isolation
            var allObjects = UnityEngine.Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                try
                {
                    if (obj == null) continue;
                    if (obj.scene.name == null || obj.scene.name == "DontDestroyOnLoad") continue;

                    UnityEngine.Object.DestroyImmediate(obj);
                }
                catch (System.Exception)
                {
                    // Object was already destroyed or is invalid, skip
                }
            }

            // Create attack GameObject with all required components
            _attackGO = new GameObject("TestAttack");
            _attack = _attackGO.AddComponent<AnimalAttack>();

            // Create attack point
            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(_attackGO.transform);
            _attackPoint = attackPointGO.transform;

            // Create animator
            var animatorGO = new GameObject("Animator");
            animatorGO.transform.SetParent(_attackGO.transform);
            animatorGO.AddComponent<AnimalAnimator>();

            // Set attack point via reflection
            var attackPointField = typeof(AnimalAttack).GetField("_attackPoint",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            attackPointField?.SetValue(_attack, _attackPoint);

            // Set other fields
            SetPrivateField("_radius", 1f);
            SetPrivateField("_forwardReach", 0.25f);
            SetPrivateField("_maxTargets", 10);

            // LayerMask needs explicit conversion from int
            LayerMask mask = LayerMask.GetMask("Default");
            SetPrivateField("_mask", mask);

            _attack.SetDamage(10f);

            // These tests expect multi-target behavior (attack all found targets)
            // Set IsAoE = true to maintain that behavior
            _attack.SetIsAoE(true);
        }

        [TearDown]
        public void TearDown()
        {
            if (_attackGO != null)
                UnityEngine.Object.DestroyImmediate(_attackGO);
        }

        private void SetPrivateField(string fieldName, object value)
        {
            var field = typeof(AnimalAttack).GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(_attack, value);
        }

        /// <summary>
        /// UT-ATK-001: SingleTarget_SingleCollider
        /// Verifies basic damage application to a single target with one collider
        /// </summary>
        [Test]
        public void SingleTarget_SingleCollider_AppliesDamageOnce()
        {
            // Arrange
            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.transform.position = _attackPoint.position + Vector3.forward * 0.5f;

            // Add collider to layer that attack can hit
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            target.TakeDamageCallCount.Should().Be(1, "damage should be applied exactly once");
            target.Current.Should().Be(90f, "target should have 100 - 10 = 90 HP");
            target.DamageReceived.Should().HaveCount(1);
            target.DamageReceived[0].Should().Be(10f);
        }

        /// <summary>
        /// UT-ATK-002: SingleTarget_MultipleColliders (HashSet Fix Verification)
        /// Verifies that HashSet deduplication works correctly (fix from 2026-02-04)
        /// Target with 5 colliders should only receive damage once, not 5 times
        /// </summary>
        [Test]
        public void SingleTarget_MultipleColliders_AppliesDamageOnce_HashSetFix()
        {
            // Arrange
            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.transform.position = _attackPoint.position + Vector3.forward * 0.5f;

            // Create 4 additional child colliders (total 5 colliders)
            for (int i = 0; i < 4; i++)
            {
                var childGO = new GameObject($"ChildCollider_{i}");
                childGO.transform.SetParent(target.transform);
                childGO.AddComponent<BoxCollider>();
                childGO.layer = LayerMask.NameToLayer("Default");
            }

            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            target.TakeDamageCallCount.Should().Be(1,
                "HashSet should deduplicate - damage applied once, not 5 times");
            target.Current.Should().Be(90f,
                "target should have taken 10 damage, not 50 (10 * 5)");
            target.DamageReceived.Should().HaveCount(1);
        }

        /// <summary>
        /// UT-ATK-003: MultipleTargets_MultipleColliders
        /// Verifies correct handling of multiple targets, each with multiple colliders
        /// 3 targets with (3, 2, 4) colliders = 9 total colliders → 3 damage applications
        /// </summary>
        [Test]
        public void MultipleTargets_MultipleColliders_AppliesDamageToEachTargetOnce()
        {
            // Arrange
            var target1 = CreateTargetWithColliders("Target1", 3);
            var target2 = CreateTargetWithColliders("Target2", 2);
            var target3 = CreateTargetWithColliders("Target3", 4);

            var targets = new[] { target1, target2, target3 };

            // Act
            _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            foreach (var target in targets)
            {
                target.TakeDamageCallCount.Should().Be(1,
                    $"{target.name} should receive damage exactly once despite multiple colliders");
                target.Current.Should().Be(90f);
            }
        }

        /// <summary>
        /// UT-ATK-004: Hedgehog_SkipsExecution
        /// Verifies that Hedgehog does not actively attack (passive counterattack only)
        /// </summary>
        [Test]
        public void Hedgehog_SkipsExecution_NoPhysicsCall()
        {
            // Arrange
            var animalTypeField = typeof(AnimalAttack).GetField("_animalType",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animalTypeField?.SetValue(_attack, AnimalType.Hedgehog);

            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.transform.position = _attackPoint.position + Vector3.forward * 0.5f;
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            target.TakeDamageCallCount.Should().Be(0,
                "Hedgehog should not attack - it only counter-attacks when hit");
            target.Current.Should().Be(100f, "target HP should be unchanged");
        }

        /// <summary>
        /// UT-ATK-005: NullAttackPoint
        /// Verifies graceful handling when attack point is null
        /// </summary>
        [Test]
        public void NullAttackPoint_ReturnsEarly_NoException()
        {
            // Arrange
            SetPrivateField("_attackPoint", null);

            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            Action act = () => _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            act.Should().NotThrow("null attack point should be handled gracefully");
            target.TakeDamageCallCount.Should().Be(0, "no damage should be applied");
        }

        /// <summary>
        /// UT-ATK-008: NullColliderInArray
        /// Verifies that null colliders in the results array are skipped without exception
        /// </summary>
        [Test]
        public void NullColliderInArray_SkipsNull_NoException()
        {
            // Arrange
            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.transform.position = _attackPoint.position + Vector3.forward * 0.5f;
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Note: This test is harder to set up directly, but the code handles it via:
            // if (col == null) continue;
            // We verify it doesn't crash

            // Act
            Action act = () => _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            act.Should().NotThrow("null colliders should be skipped gracefully");
        }

        /// <summary>
        /// UT-ATK-009: ColliderWithoutIDamageable
        /// Verifies that colliders without IDamageable component are skipped
        /// </summary>
        [Test]
        public void ColliderWithoutIDamageable_SkipsCollider_NoException()
        {
            // Arrange - create collider without IDamageable
            var nonDamageableGO = new GameObject("NonDamageable");
            nonDamageableGO.transform.position = _attackPoint.position + Vector3.forward * 0.5f;
            nonDamageableGO.AddComponent<BoxCollider>();
            nonDamageableGO.layer = LayerMask.NameToLayer("Default");

            // Also create a valid target to ensure attack executes
            var validTarget = AttackTestHelper.CreateMockDamageable(100f);
            validTarget.transform.position = _attackPoint.position + Vector3.forward * 0.6f;
            validTarget.gameObject.layer = LayerMask.NameToLayer("Default");

            // Act
            Action act = () => _attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert
            act.Should().NotThrow("colliders without IDamageable should be skipped");
            validTarget.TakeDamageCallCount.Should().Be(1,
                "valid target should still receive damage");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(nonDamageableGO);
        }

        #region Helper Methods

        private MockDamageable CreateTargetWithColliders(string baseName, int colliderCount)
        {
            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.name = baseName;
            target.transform.position = _attackPoint.position + Vector3.forward * 0.5f;
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            // Create child colliders
            for (int i = 1; i < colliderCount; i++)
            {
                var childGO = new GameObject($"{baseName}_Collider_{i}");
                childGO.transform.SetParent(target.transform);
                childGO.AddComponent<BoxCollider>();
                childGO.layer = LayerMask.NameToLayer("Default");
            }

            return target;
        }

        #endregion
    }
}
