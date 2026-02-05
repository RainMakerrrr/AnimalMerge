using System;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.RegressionTests
{
    /// <summary>
    /// Regression tests to verify critical bug fixes in the Attack and Damage system.
    /// These tests document known bugs and ensure they remain fixed.
    /// </summary>
    [TestFixture]
    public class AttackAndDamageRegressionTests
    {
        /// <summary>
        /// REG-001: HashSetDeduplication_Fix
        /// REGRESSION TEST for bug fixed on 2026-02-04
        ///
        /// BUG: Before fix, multiple colliders on same target caused multiple TakeDamage calls
        /// FIX: Added HashSet to deduplicate IDamageable instances
        ///
        /// This test ensures the bug doesn't reoccur.
        /// </summary>
        [Test]
        public void REG001_HashSetDeduplication_MultipleColliders_SingleDamageApplication()
        {
            // Arrange
            var attackGO = new GameObject("TestAttack");
            var attack = attackGO.AddComponent<AnimalAttack>();
            var attackPointGO = new GameObject("AttackPoint");
            attackPointGO.transform.SetParent(attackGO.transform);

            SetPrivateField(attack, "_attackPoint", attackPointGO.transform);
            SetPrivateField(attack, "_radius", 1f);
            SetPrivateField(attack, "_forwardReach", 0.25f);
            SetPrivateField(attack, "_maxTargets", 10);

            // LayerMask needs explicit conversion from int
            LayerMask mask = LayerMask.GetMask("Default");
            SetPrivateField(attack, "_mask", mask);
            attack.SetDamage(10f);

            // Create target with 5 colliders (simulating complex mesh with multiple colliders)
            var target = AttackTestHelper.CreateMockDamageable(100f);
            target.transform.position = attackPointGO.transform.position + Vector3.forward * 0.5f;
            target.gameObject.layer = LayerMask.NameToLayer("Default");

            for (int i = 0; i < 4; i++)
            {
                var childGO = new GameObject($"ChildCollider_{i}");
                childGO.transform.SetParent(target.transform);
                childGO.AddComponent<BoxCollider>();
                childGO.layer = LayerMask.NameToLayer("Default");
            }

            // Act
            attack.AttackAnimationHandlerAsync().GetAwaiter().GetResult();

            // Assert - THE CRITICAL ASSERTION
            target.TakeDamageCallCount.Should().Be(1,
                "REGRESSION: HashSet deduplication should ensure TakeDamageAsync called only ONCE, not 5 times");

            target.Current.Should().Be(90f,
                "REGRESSION: Damage should be 10 (once), not 50 (5 times)");

            target.DamageReceived.Should().HaveCount(1,
                "REGRESSION: Should record only one damage instance");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(attackGO);
            UnityEngine.Object.DestroyImmediate(target.gameObject);
        }

        /// <summary>
        /// REG-002: AbilityPriorityOrder_Fix
        /// REGRESSION TEST for Bug #3 (fixed in this session)
        ///
        /// BUG: Used OrderBy(a => a.Priority) causing ASCENDING order (0, 1, 2...)
        /// FIX: Changed to OrderByDescending(a => a.Priority) for DESCENDING order (2, 1, 0...)
        ///
        /// Expected: High priority executes first
        /// </summary>
        [Test]
        public void REG002_AbilityPriorityOrder_HighPriorityExecutesFirst()
        {
            // Arrange
            var executionOrder = new System.Collections.Generic.List<string>();

            var dodgeAbility = HealthTestHelper.CreateMockAbility(
                canUse: true,
                isBlocking: true,
                priority: 1,
                onApply: () => executionOrder.Add("Dodge"));

            var counterAbility = HealthTestHelper.CreateMockAbility(
                canUse: true,
                isBlocking: false,
                priority: 0,
                onApply: () => executionOrder.Add("Counter"));

            var health = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: dodgeAbility);

            health.AddAbility(counterAbility);

            var attacker = AttackTestHelper.CreateMockAttack(damage: 20f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert - THE CRITICAL ASSERTION
            executionOrder.Should().HaveCount(2);
            executionOrder[0].Should().Be("Dodge",
                "REGRESSION: Dodge (priority 1) should execute FIRST (OrderByDescending fix)");
            executionOrder[1].Should().Be("Counter",
                "REGRESSION: Counter (priority 0) should execute SECOND");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// REG-003: FoxHealth_MergedAbilityBlocking_Fix
        /// REGRESSION TEST for Bug #1 (fixed in this session)
        ///
        /// BUG: FoxHealth.ApplyAbilities() returned true only if OWN ability CanUse=true
        ///      Ignored case where MERGED ability blocked damage
        /// FIX: Track isBlockedDamage for ANY ability (own or merged)
        ///
        /// Expected: Merged Dodge blocks damage even if own Dodge can't use
        /// </summary>
        [Test]
        public void REG003_FoxHealth_MergedAbilityBlocking_CorrectlyBlocksDamage()
        {
            // Arrange
            var foxGO = new GameObject("Fox");
            var foxHealth = foxGO.AddComponent<FoxHealth>();
            var foxAnimator = foxGO.AddComponent<AnimalAnimator>();

            foxHealth.Construct(new UnityEngine.Collider[] { foxGO.AddComponent<BoxCollider>() });

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(foxHealth, foxAnimator);

            foxHealth.SetMaxHealth(100f);

            // Own ability: CANNOT use (simulating it's on cooldown or failed RNG)
            var ownDodge = HealthTestHelper.CreateMockDodge(canUse: false);
            foxHealth.SetAbility(ownDodge);

            // Merged ability: CAN use and WILL block
            var mergedDodge = HealthTestHelper.CreateMockDodge(canUse: true);
            foxHealth.AddAbility(mergedDodge);

            var attacker = AttackTestHelper.CreateMockAttack(damage: 30f);

            // Act
            foxHealth.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert - THE CRITICAL ASSERTION
            foxHealth.Current.Should().Be(100f,
                "REGRESSION: Merged Dodge SHOULD block damage even if own Dodge can't use (Bug #1 fix)");

            mergedDodge.Received(1).Apply();
            ownDodge.DidNotReceive().Apply(); // Own dodge shouldn't be called (CanUse=false)

            // Cleanup
            UnityEngine.Object.DestroyImmediate(foxGO);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// REG-004: HedgehogHealth_OwnAbilityMissing_Fix
        /// REGRESSION TEST for Bug #2 (fixed in this session)
        ///
        /// BUG: Original Hedgehog's own CounterAttack ability was commented out
        ///      Only merged hedgehog abilities worked
        /// FIX: Uncommented and refactored ApplyAbilities() to apply own ability first
        ///
        /// Expected: Original Hedgehog (owner=true) counter-attacks with 100% chance
        /// </summary>
        [Test]
        public void REG004_HedgehogHealth_OwnAbility_CounterAttacks()
        {
            // Arrange
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<HedgehogHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            hedgehogHealth.Construct(new UnityEngine.Collider[] { hedgehogGO.AddComponent<BoxCollider>() });

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            hedgehogHealth.SetMaxHealth(50f);
            hedgehogAttack.SetDamage(15f);

            // Create attacker with health
            var attackerGO = new GameObject("Attacker");
            var attackerHealth = AttackTestHelper.CreateMockDamageable(100f);
            attackerHealth.transform.SetParent(attackerGO.transform);
            var attackerAttack = attackerGO.AddComponent<AnimalAttack>();
            attackerAttack.SetDamage(20f);

            // Set own CounterAttack ability (owner=true, 100% chance)
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true);

            hedgehogHealth.SetAbility(counterAttack);

            hedgehogHealth.LastAttack = attackerAttack;

            // Act
            hedgehogHealth.TakeDamageAsync(attackerAttack).GetAwaiter().GetResult();

            // Assert - THE CRITICAL ASSERTION
            hedgehogHealth.Current.Should().Be(30f,
                "Hedgehog takes damage (50 - 20 = 30)");

            attackerHealth.TakeDamageCallCount.Should().Be(1,
                "REGRESSION: Original Hedgehog SHOULD counter-attack (Bug #2 fix - own ability was commented out)");

            attackerHealth.Current.Should().Be(85f,
                "Attacker should take counter-attack damage (100 - 15 = 85)");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(hedgehogGO);
            UnityEngine.Object.DestroyImmediate(attackerGO);
        }

        #region Helper Methods

        private void SetPrivateField(object obj, string fieldName, object value)
        {
            var field = obj.GetType().GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field?.SetValue(obj, value);
        }

        #endregion
    }
}
