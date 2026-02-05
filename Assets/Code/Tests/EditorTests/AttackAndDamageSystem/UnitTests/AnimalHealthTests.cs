using System;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class AnimalHealthTests
    {
        /// <summary>
        /// UT-HP-001: NoAbilities_BasicDamage
        /// Verifies basic damage application without any abilities
        /// </summary>
        [Test]
        public void NoAbilities_BasicDamage_ReducesHealth()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 25f);

            var takenDamageTracker = new HealthTestHelper.EventTracker();
            health.TakenDamage += takenDamageTracker.Track;

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            health.Current.Should().Be(75f, "100 - 25 = 75");
            health.IsDead.Should().BeFalse();
            takenDamageTracker.CallCount.Should().Be(1, "TakenDamage event should fire once");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-002: DamageCausesDeathExactly
        /// Verifies death handling when damage reduces HP to exactly 0
        /// </summary>
        [Test]
        public void DamageCausesDeathExactly_FiresDeathEvent()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 30f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 30f);

            var diedTracker = new HealthTestHelper.EventTracker();
            health.Died += diedTracker.Track;

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            health.Current.Should().Be(0f);
            health.IsDead.Should().BeTrue();
            diedTracker.CallCount.Should().Be(1, "Died event should fire once");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-003: OverkillDamage
        /// Verifies handling when damage exceeds current HP
        /// </summary>
        [Test]
        public void OverkillDamage_HealthGoesNegative_MarksAsDead()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 20f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 50f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            health.Current.Should().Be(-30f, "20 - 50 = -30");
            health.IsDead.Should().BeTrue();

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-004: NoAbilities_ReturnsFalse
        /// Verifies that ApplyAbilities returns false when no abilities are present
        /// </summary>
        [Test]
        public void NoAbilities_AppliesDamageNormally()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);
            var attacker = AttackTestHelper.CreateMockAttack(damage: 10f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            health.Current.Should().Be(90f, "damage should be applied when no abilities exist");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-005: AbilityCannotUse_Skipped
        /// Verifies that abilities with CanUse=false are skipped
        /// </summary>
        [Test]
        public void AbilityCannotUse_Skipped_DamageApplied()
        {
            // Arrange
            var abilityApplyCalled = false;
            var ability = HealthTestHelper.CreateMockAbility(
                canUse: false,
                isBlocking: true,
                onApply: () => abilityApplyCalled = true);

            var health = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: ability);

            var attacker = AttackTestHelper.CreateMockAttack(damage: 15f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            abilityApplyCalled.Should().BeFalse("ability.Apply() should not be called when CanUse=false");
            health.Current.Should().Be(85f, "damage should be applied when ability cannot be used");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-006: BlockingAbility_BlocksDamage
        /// Verifies that abilities with IsBlockingDamage=true prevent damage
        /// </summary>
        [Test]
        public void BlockingAbility_BlocksDamage_HealthUnchanged()
        {
            // Arrange
            var abilityApplyCalled = false;
            var blockingAbility = HealthTestHelper.CreateMockAbility(
                canUse: true,
                isBlocking: true,
                priority: 1,
                onApply: () => abilityApplyCalled = true);

            var health = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: blockingAbility);

            var attacker = AttackTestHelper.CreateMockAttack(damage: 20f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            abilityApplyCalled.Should().BeTrue("blocking ability should be applied");
            health.Current.Should().Be(100f, "damage should be blocked");
            health.IsDead.Should().BeFalse();

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-007: NonBlockingAbility_DamageApplied
        /// Verifies that abilities with IsBlockingDamage=false still allow damage
        /// (e.g., CounterAttack)
        /// </summary>
        [Test]
        public void NonBlockingAbility_DamageApplied_AbilityExecutes()
        {
            // Arrange
            var abilityApplyCalled = false;
            var counterAbility = HealthTestHelper.CreateMockAbility(
                canUse: true,
                isBlocking: false,
                priority: 0,
                onApply: () => abilityApplyCalled = true);

            var health = HealthTestHelper.CreateAnimalHealth(
                maxHealth: 100f,
                ownAbility: counterAbility);

            var attacker = AttackTestHelper.CreateMockAttack(damage: 20f);

            // Act
            health.TakeDamageAsync(attacker).GetAwaiter().GetResult();

            // Assert
            abilityApplyCalled.Should().BeTrue("counter ability should be applied");
            health.Current.Should().Be(80f, "damage should still be applied (counter doesn't block)");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-008: MultipleAbilities_PriorityOrder
        /// Verifies that abilities execute in correct priority order (high to low)
        /// DOCUMENTS BUG: This test will verify the fix for OrderBy → OrderByDescending
        /// </summary>
        [Test]
        public void MultipleAbilities_PriorityOrder_HighPriorityFirst()
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

            // Assert
            executionOrder.Should().HaveCount(2, "both abilities should execute");
            executionOrder[0].Should().Be("Dodge",
                "Dodge (priority 1) should execute BEFORE Counter (priority 0)");
            executionOrder[1].Should().Be("Counter",
                "Counter (priority 0) should execute AFTER Dodge (priority 1)");

            health.Current.Should().Be(100f, "Dodge should block damage");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
            UnityEngine.Object.DestroyImmediate(attacker.gameObject);
        }

        /// <summary>
        /// UT-HP-009: Upgrade_Multiplier
        /// Verifies Upgrade() applies multiplier correctly and fully heals
        /// </summary>
        [Test]
        public void Upgrade_Multiplier_IncreasesMaxAndFullHeals()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);

            // Simulate taking damage first
            var currentField = typeof(AnimalHealth).GetProperty("Current");
            currentField?.SetValue(health, 50f);

            // Act
            health.Upgrade(multiplier: 1.5f);

            // Assert
            health.Max.Should().Be(150f, "100 * 1.5 = 150");
            health.Current.Should().Be(150f, "should fully heal to new max");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
        }

        /// <summary>
        /// UT-HP-010: SetMaxHealth_FullHeals
        /// Verifies SetMaxHealth() updates max and fully heals current HP
        /// </summary>
        [Test]
        public void SetMaxHealth_FullHeals_SetsMaxAndCurrent()
        {
            // Arrange
            var health = HealthTestHelper.CreateAnimalHealth(maxHealth: 100f);

            // Simulate taking damage
            var currentField = typeof(AnimalHealth).GetProperty("Current");
            currentField?.SetValue(health, 30f);

            // Act
            health.SetMaxHealth(200f);

            // Assert
            health.Max.Should().Be(200f);
            health.Current.Should().Be(200f, "should fully heal to new max");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(health.gameObject);
        }
    }
}
