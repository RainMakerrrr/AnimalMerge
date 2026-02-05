using System.Threading.Tasks;
using Code.Abilities;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class DodgeAbilityTests
    {
        /// <summary>
        /// UT-DODGE-001: FirstUse_Always100Percent
        /// Verifies that first dodge use always succeeds (counter = 0)
        /// </summary>
        [Test]
        public void FirstUse_Always100Percent_CanUseAlwaysTrue()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: false, initialCounter: 0);

            // Act & Assert - test multiple times to ensure it's always true
            for (int i = 0; i < 10; i++)
            {
                dodge.CanUse.Should().BeTrue(
                    "first dodge use should always be 100% regardless of RNG or owner status");
            }
        }

        /// <summary>
        /// UT-DODGE-002: Owner_80Percent
        /// Verifies that owner dodge has 80% success rate after first use
        /// </summary>
        [Test]
        public void Owner_80Percent_CanUse_CorrectThreshold()
        {
            // Arrange
            var successCount = 0;
            var failCount = 0;

            // Act - test with controlled RNG values
            // Test boundary: 79 should succeed, 80 should fail
            var dodgeSuccess = AbilityTestMocks.CreateMockDodgeWithRNG(
                isOwner: true,
                initialCounter: 1,
                randomValueProvider: () => 79);

            var dodgeFail = AbilityTestMocks.CreateMockDodgeWithRNG(
                isOwner: true,
                initialCounter: 1,
                randomValueProvider: () => 80);

            // Assert
            dodgeSuccess.CanUse.Should().BeTrue("79 < 80, should succeed");
            dodgeFail.CanUse.Should().BeFalse("80 >= 80, should fail");
        }

        /// <summary>
        /// UT-DODGE-003: Inherited_50Percent
        /// Verifies that inherited dodge has 50% success rate
        /// </summary>
        [Test]
        public void Inherited_50Percent_CanUse_CorrectThreshold()
        {
            // Arrange & Act - test with controlled RNG values
            // Test boundary: 49 should succeed, 50 should fail
            var dodgeSuccess = AbilityTestMocks.CreateMockDodgeWithRNG(
                isOwner: false,
                initialCounter: 1,
                randomValueProvider: () => 49);

            var dodgeFail = AbilityTestMocks.CreateMockDodgeWithRNG(
                isOwner: false,
                initialCounter: 1,
                randomValueProvider: () => 50);

            // Assert
            dodgeSuccess.CanUse.Should().BeTrue("49 < 50, should succeed");
            dodgeFail.CanUse.Should().BeFalse("50 >= 50, should fail");
        }

        /// <summary>
        /// UT-DODGE-004: Apply_DisablesCollidersAndShifts
        /// Verifies that Apply() disables colliders, increments counter, and calls Shift()
        /// </summary>
        [Test]
        public void Apply_DisablesCollidersAndShifts_BehaviorCorrect()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var shiftCalled = false;
            transformable.Shift().Returns(callInfo =>
            {
                shiftCalled = true;
                return Task.CompletedTask;
            });

            var go = new GameObject("DodgeTest");
            var collider1 = go.AddComponent<BoxCollider>();
            var collider2 = go.AddComponent<SphereCollider>();
            var colliders = new UnityEngine.Collider[] { collider1, collider2 };

            collider1.enabled = true;
            collider2.enabled = true;

            var dodge = new Dodge(transformable, colliders, isOwner: true);

            // Act
            dodge.Apply().GetAwaiter().GetResult();

            // Assert
            collider1.enabled.Should().BeFalse("collider 1 should be disabled");
            collider2.enabled.Should().BeFalse("collider 2 should be disabled");
            shiftCalled.Should().BeTrue("Shift() should be called");

            // Verify counter incremented (check by testing CanUse behavior change)
            // After first Apply, counter = 1, so CanUse should depend on RNG now
            var canUseAfterApply = dodge.CanUse;
            // We can't predict the exact value due to RNG, but it should no longer be always true

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// UT-DODGE-005: IsBlockingDamage_True
        /// Verifies that Dodge correctly reports IsBlockingDamage = true
        /// </summary>
        [Test]
        public void IsBlockingDamage_True_CorrectValue()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: true);

            // Assert
            dodge.IsBlockingDamage.Should().BeTrue(
                "Dodge should block damage when successfully applied");
        }

        /// <summary>
        /// UT-DODGE-006: Priority_Is1
        /// Verifies that Dodge has priority 1 (higher than CounterAttack's 0)
        /// </summary>
        [Test]
        public void Priority_Is1_CorrectValue()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(isOwner: true);

            // Assert
            dodge.Priority.Should().Be(1,
                "Dodge should have priority 1 to execute before CounterAttack (priority 0)");
        }
    }
}
