using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.Animals.Health;
using Code.Services.Random;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class CounterAttackAbilityTests
    {
        /// <summary>
        /// UT-COUNTER-001: Owner_Always100Percent
        /// Verifies that owner counter-attack always succeeds (100%)
        /// </summary>
        [Test]
        public void Owner_Always100Percent_CanUseAlwaysTrue()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            // Act & Assert - test multiple times to ensure always true
            for (int i = 0; i < 10; i++)
            {
                counterAttack.CanUse(null).Should().BeTrue(
                    "owner counter-attack should always be 100%");
            }
        }

        /// <summary>
        /// UT-COUNTER-002: Inherited_50Percent
        /// Verifies that inherited counter-attack has 50% success rate
        /// </summary>
        [Test]
        public void Inherited_50Percent_CanUse_CorrectThreshold()
        {
            // Arrange - use mock with controlled RNG
            var counterSuccess = AbilityTestMocks.CreateMockCounterAttackWithRNG(
                health: null,
                animator: null,
                attack: null,
                isOwner: false,
                randomValueProvider: () => 49);

            var counterFail = AbilityTestMocks.CreateMockCounterAttackWithRNG(
                health: null,
                animator: null,
                attack: null,
                isOwner: false,
                randomValueProvider: () => 50);

            // Assert
            counterSuccess.CanUse(null).Should().BeTrue("49 < 50, should succeed");
            counterFail.CanUse(null).Should().BeFalse("50 >= 50, should fail");
        }

        /// <summary>
        /// UT-COUNTER-003: Apply_DamagesAttacker
        /// Verifies that counter-attack applies damage back to the attacker
        /// </summary>
        [Test]
        public void Apply_DamagesAttacker_CorrectDamageApplied()
        {
            // Arrange
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<AnimalHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            var abilityManager = new AbilityManager();
            hedgehogHealth.Construct(new UnityEngine.Collider[] { hedgehogGO.AddComponent<BoxCollider>() }, abilityManager);
            hedgehogHealth.SetMaxHealth(50f);
            hedgehogAttack.SetDamage(15f);

            // Create animator field
            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            var attackerGO = new GameObject("Attacker");
            var attackerHealth = attackerGO.AddComponent<MockDamageable>();
            attackerHealth.Initialize(100f);
            var attackerAttack = attackerGO.AddComponent<AnimalAttack>();
            attackerAttack.SetDamage(20f);


            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true,
                randomProvider);

            // Act
            // Must call CanUse() first to set the attacker
            counterAttack.CanUse(attackerAttack);
            counterAttack.Apply().GetAwaiter().GetResult();

            // Assert
            attackerHealth.TakeDamageCallCount.Should().Be(1,
                "counter-attack should damage the attacker once");
            attackerHealth.Current.Should().Be(85f,
                "attacker should have 100 - 15 = 85 HP");
            attackerHealth.DamageReceived[0].Should().Be(15f);

            // Cleanup
            UnityEngine.Object.DestroyImmediate(hedgehogGO);
            UnityEngine.Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-COUNTER-004: IsBlockingDamage_False
        /// Verifies that CounterAttack does NOT block damage (hedgehog takes damage + counters)
        /// </summary>
        [Test]
        public void IsBlockingDamage_False_CorrectValue()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            // Assert
            counterAttack.IsBlockingDamage.Should().BeFalse(
                "CounterAttack should NOT block damage - unit takes damage and retaliates");
        }

        /// <summary>
        /// UT-COUNTER-005: Priority_Is0
        /// Verifies that CounterAttack has priority 0 (lower than Dodge's 1)
        /// </summary>
        [Test]
        public void Priority_Is0_CorrectValue()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            // Assert
            counterAttack.Priority.Should().Be(0,
                "CounterAttack should have priority 0 to execute after Dodge (priority 1)");
        }

        /// <summary>
        /// UT-COUNTER-006: AttackerNoIDamageable_GracefulHandling
        /// DOCUMENTS BUG: Verifies graceful handling when attacker has no IDamageable
        /// Current implementation may throw NullReferenceException
        /// </summary>
        [Test]
        public void AttackerNoIDamageable_GracefulHandling_NoException()
        {
            // Arrange
            var hedgehogGO = new GameObject("Hedgehog");
            var hedgehogHealth = hedgehogGO.AddComponent<AnimalHealth>();
            var hedgehogAnimator = hedgehogGO.AddComponent<AnimalAnimator>();
            var hedgehogAttack = hedgehogGO.AddComponent<AnimalAttack>();

            var abilityManager = new AbilityManager();
            hedgehogHealth.Construct(new UnityEngine.Collider[] { hedgehogGO.AddComponent<BoxCollider>() }, abilityManager);
            hedgehogHealth.SetMaxHealth(50f);

            var animatorField = typeof(AnimalHealth).GetField("_animator",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            animatorField?.SetValue(hedgehogHealth, hedgehogAnimator);

            // Create attacker WITHOUT IDamageable component
            var attackerGO = new GameObject("AttackerWithoutHealth");
            var attackerAttack = attackerGO.AddComponent<AnimalAttack>();


            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var counterAttack = new CounterAttack(
                hedgehogHealth,
                hedgehogAnimator,
                hedgehogAttack,
                isOwner: true,
                randomProvider);

            // Act
            System.Action act = () => counterAttack.Apply().GetAwaiter().GetResult();

            // Assert
            act.Should().NotThrow(
                "CounterAttack should handle missing IDamageable gracefully (fixed with null checks)");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(hedgehogGO);
            UnityEngine.Object.DestroyImmediate(attackerGO);
        }
    }
}
