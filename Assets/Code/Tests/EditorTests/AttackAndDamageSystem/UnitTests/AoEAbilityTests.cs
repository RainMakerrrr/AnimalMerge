using Code.Abilities;
using Code.Animals;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class AoEAbilityTests
    {
        /// <summary>
        /// UT-AOE-DODGE-001: Dodge_AgainstAoE_ReturnsFalse
        /// Verifies that Dodge ability cannot be used against AoE attacks
        /// </summary>
        [Test]
        public void Dodge_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: true, initialCounter: 0);

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = dodge.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("Dodge should NOT work against AoE attacks");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-DODGE-002: Dodge_AgainstNonAoE_ReturnsNormalBehavior
        /// Verifies that Dodge ability works normally against non-AoE attacks
        /// </summary>
        [Test]
        public void Dodge_AgainstNonAoE_ReturnsNormalBehavior()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: true, initialCounter: 0);

            var attackerGO = new GameObject("NonAoEAttacker");
            var normalAttack = attackerGO.AddComponent<AnimalAttack>();
            normalAttack.SetIsAoE(false);

            // Act
            var canUse = dodge.CanUse(normalAttack);

            // Assert
            canUse.Should().BeTrue("First use of Dodge should always be 100% against non-AoE");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-DODGE-003: Dodge_AgainstNull_ReturnsNormalBehavior
        /// Verifies that Dodge ability works normally when attacker is null
        /// </summary>
        [Test]
        public void Dodge_AgainstNull_ReturnsNormalBehavior()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: true, initialCounter: 0);

            // Act
            var canUse = dodge.CanUse(null);

            // Assert
            canUse.Should().BeTrue("First use of Dodge should always be 100% when attacker is null");
        }

        /// <summary>
        /// UT-AOE-DODGE-004: InheritedDodge_AgainstAoE_ReturnsFalse
        /// Verifies that inherited Dodge also cannot be used against AoE
        /// </summary>
        [Test]
        public void InheritedDodge_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var inheritedDodge = AbilityTestMocks.CreateDodge(isOwner: false, initialCounter: 1);

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = inheritedDodge.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("Inherited Dodge should also NOT work against AoE");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-COUNTER-001: CounterAttack_AgainstAoE_ReturnsFalse
        /// Verifies that CounterAttack ability cannot be used against AoE attacks
        /// </summary>
        [Test]
        public void CounterAttack_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = counterAttack.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("CounterAttack should NOT work against AoE attacks");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-COUNTER-002: CounterAttack_AgainstNonAoE_ReturnsNormalBehavior
        /// Verifies that CounterAttack works normally against non-AoE attacks
        /// </summary>
        [Test]
        public void CounterAttack_AgainstNonAoE_ReturnsNormalBehavior()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            var attackerGO = new GameObject("NonAoEAttacker");
            var normalAttack = attackerGO.AddComponent<AnimalAttack>();
            normalAttack.SetIsAoE(false);

            // Act
            var canUse = counterAttack.CanUse(normalAttack);

            // Assert
            canUse.Should().BeTrue("Owner CounterAttack should always be 100% against non-AoE");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-COUNTER-003: CounterAttack_AgainstNull_ReturnsNormalBehavior
        /// Verifies that CounterAttack works normally when attacker is null
        /// </summary>
        [Test]
        public void CounterAttack_AgainstNull_ReturnsNormalBehavior()
        {
            // Arrange
            var counterAttack = AbilityTestMocks.CreateMockCounterAttack(isOwner: true);

            // Act
            var canUse = counterAttack.CanUse(null);

            // Assert
            canUse.Should().BeTrue("Owner CounterAttack should always be 100% when attacker is null");
        }

        /// <summary>
        /// UT-AOE-COUNTER-004: InheritedCounterAttack_AgainstAoE_ReturnsFalse
        /// Verifies that inherited CounterAttack also cannot be used against AoE
        /// </summary>
        [Test]
        public void InheritedCounterAttack_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var inheritedCounter = AbilityTestMocks.CreateMockCounterAttack(isOwner: false);

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = inheritedCounter.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("Inherited CounterAttack should also NOT work against AoE");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-ATTACK-001: AnimalAttack_SetIsAoE_StoresAndRetrievesCorrectly
        /// Verifies that IsAoE flag can be set and retrieved correctly
        /// </summary>
        [Test]
        public void AnimalAttack_SetIsAoE_StoresAndRetrievesCorrectly()
        {
            // Arrange
            var attackerGO = new GameObject("Attacker");
            var attack = attackerGO.AddComponent<AnimalAttack>();

            // Act - Test false
            attack.SetIsAoE(false);
            var isAoEFalse = attack.IsAoE;

            // Act - Test true
            attack.SetIsAoE(true);
            var isAoETrue = attack.IsAoE;

            // Assert
            isAoEFalse.Should().BeFalse("IsAoE should be false when set to false");
            isAoETrue.Should().BeTrue("IsAoE should be true when set to true");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-ATTACK-002: AnimalAttack_DefaultIsAoE_IsFalse
        /// Verifies that IsAoE defaults to false for new AnimalAttack components
        /// </summary>
        [Test]
        public void AnimalAttack_DefaultIsAoE_IsFalse()
        {
            // Arrange & Act
            var attackerGO = new GameObject("Attacker");
            var attack = attackerGO.AddComponent<AnimalAttack>();

            // Assert
            attack.IsAoE.Should().BeFalse("IsAoE should default to false");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-MOCK-001: MockDodge_AgainstAoE_ReturnsFalse
        /// Verifies that MockDodge (test helper) also respects AoE logic
        /// </summary>
        [Test]
        public void MockDodge_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var mockDodge = AbilityTestMocks.CreateMockDodgeWithRNG(
                isOwner: true,
                initialCounter: 0,
                randomValueProvider: () => 0); // Always succeed if allowed

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = mockDodge.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("MockDodge should NOT work against AoE attacks");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }

        /// <summary>
        /// UT-AOE-MOCK-002: MockCounterAttack_AgainstAoE_ReturnsFalse
        /// Verifies that MockCounterAttack (test helper) also respects AoE logic
        /// </summary>
        [Test]
        public void MockCounterAttack_AgainstAoE_ReturnsFalse()
        {
            // Arrange
            var mockCounter = AbilityTestMocks.CreateMockCounterAttackWithRNG(
                health: null,
                animator: null,
                attack: null,
                isOwner: true,
                randomValueProvider: () => 0); // Always succeed if allowed

            var attackerGO = new GameObject("AoEAttacker");
            var aoeAttack = attackerGO.AddComponent<AnimalAttack>();
            aoeAttack.SetIsAoE(true);

            // Act
            var canUse = mockCounter.CanUse(aoeAttack);

            // Assert
            canUse.Should().BeFalse("MockCounterAttack should NOT work against AoE attacks");

            // Cleanup
            Object.DestroyImmediate(attackerGO);
        }
    }
}
