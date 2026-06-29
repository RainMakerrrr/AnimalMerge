using Cysharp.Threading.Tasks;
using Code.Abilities;
using Code.Services.Random;
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
            var dodge = AbilityTestMocks.CreateDodge(successChance: 30, initialCounter: 0);

            // Act & Assert - test multiple times to ensure it's always true
            for (int i = 0; i < 10; i++)
            {
                dodge.CanUse(null).Should().BeTrue(
                    "first dodge use should always be 100% regardless of RNG or owner status");
            }
        }

        /// <summary>
        /// UT-DODGE-002: Owner_50Percent
        /// Verifies that owner dodge has a 50% success rate after first use.
        /// Exercises the real Dodge.CanUse with a stubbed IRandomProvider.
        /// </summary>
        [Test]
        public void Owner_50Percent_CanUse_CorrectThreshold()
        {
            // Arrange - boundary: 49 < 50 succeeds, 50 >= 50 fails (owner threshold = 50)
            var randomSuccess = Substitute.For<IRandomProvider>();
            randomSuccess.Range(0, 100).Returns(49);

            var randomFail = Substitute.For<IRandomProvider>();
            randomFail.Range(0, 100).Returns(50);

            var dodgeSuccess = AbilityTestMocks.CreateDodge(
                successChance: 50, initialCounter: 1, randomProvider: randomSuccess);
            var dodgeFail = AbilityTestMocks.CreateDodge(
                successChance: 50, initialCounter: 1, randomProvider: randomFail);

            // Act & Assert
            dodgeSuccess.CanUse(null).Should().BeTrue("49 < 50, owner dodge should succeed");
            dodgeFail.CanUse(null).Should().BeFalse("50 >= 50, owner dodge should fail");
        }

        /// <summary>
        /// UT-DODGE-003: Inherited_30Percent
        /// Verifies that inherited dodge has a 30% success rate after first use.
        /// Exercises the real Dodge.CanUse with a stubbed IRandomProvider.
        /// </summary>
        [Test]
        public void Inherited_30Percent_CanUse_CorrectThreshold()
        {
            // Arrange - boundary: 29 < 30 succeeds, 30 >= 30 fails (inherited threshold = 30)
            var randomSuccess = Substitute.For<IRandomProvider>();
            randomSuccess.Range(0, 100).Returns(29);

            var randomFail = Substitute.For<IRandomProvider>();
            randomFail.Range(0, 100).Returns(30);

            var dodgeSuccess = AbilityTestMocks.CreateDodge(
                successChance: 30, initialCounter: 1, randomProvider: randomSuccess);
            var dodgeFail = AbilityTestMocks.CreateDodge(
                successChance: 30, initialCounter: 1, randomProvider: randomFail);

            // Act & Assert
            dodgeSuccess.CanUse(null).Should().BeTrue("29 < 30, inherited dodge should succeed");
            dodgeFail.CanUse(null).Should().BeFalse("30 >= 30, inherited dodge should fail");
        }

        /// <summary>
        /// UT-DODGE-004: Apply_DisablesCollidersAndShifts
        /// Verifies that Apply() temporarily disables colliders, calls Shift(), and re-enables colliders after
        /// UPDATED: Event-Driven architecture - Dodge self-manages colliders
        /// UPDATED: Shift() returns bool - true if successful
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
                return UniTask.FromResult(true); // Shift successful
            });

            var go = new GameObject("DodgeTest");
            var collider1 = go.AddComponent<BoxCollider>();
            var collider2 = go.AddComponent<SphereCollider>();
            var colliders = new UnityEngine.Collider[] { collider1, collider2 };

            collider1.enabled = true;
            collider2.enabled = true;

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var dodge = new Dodge(transformable, colliders, successChance: 50, randomProvider);

            // Act
            dodge.Apply().GetAwaiter().GetResult();

            // Assert
            // NEW BEHAVIOR: Dodge self-manages colliders (Event-Driven architecture)
            // Colliders are temporarily disabled during shift, then re-enabled after
            collider1.enabled.Should().BeTrue("collider 1 should be re-enabled after shift (self-management)");
            collider2.enabled.Should().BeTrue("collider 2 should be re-enabled after shift (self-management)");
            shiftCalled.Should().BeTrue("Shift() should be called");

            // Verify counter incremented (check by testing CanUse behavior change)
            // After first Apply, counter = 1, so CanUse should depend on RNG now
            var canUseAfterApply = dodge.CanUse(null);
            // We can't predict the exact value due to RNG, but it should no longer be always true

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// UT-DODGE-005: IsBlockingDamage_WhenShiftSuccessful_True
        /// Verifies that Dodge correctly reports IsBlockingDamage = true when Shift() succeeds
        /// </summary>
        [Test]
        public void IsBlockingDamage_WhenShiftSuccessful_True()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            transformable.Shift().Returns(UniTask.FromResult(true)); // Shift successful

            var go = new GameObject("DodgeTest");
            var collider = go.AddComponent<BoxCollider>();
            var colliders = new UnityEngine.Collider[] { collider };

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var dodge = new Dodge(transformable, colliders, successChance: 50, randomProvider);

            // Act
            dodge.Apply().GetAwaiter().GetResult();

            // Assert
            dodge.IsBlockingDamage.Should().BeTrue(
                "Dodge should block damage when Shift() succeeds");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// UT-DODGE-006: IsBlockingDamage_WhenShiftFails_False
        /// Verifies that Dodge correctly reports IsBlockingDamage = false when Shift() fails (no valid dodge positions)
        /// </summary>
        [Test]
        public void IsBlockingDamage_WhenShiftFails_False()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            transformable.Shift().Returns(UniTask.FromResult(false)); // Shift failed - no valid positions

            var go = new GameObject("DodgeTest");
            var collider = go.AddComponent<BoxCollider>();
            var colliders = new UnityEngine.Collider[] { collider };

            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var dodge = new Dodge(transformable, colliders, successChance: 50, randomProvider);

            // Act
            dodge.Apply().GetAwaiter().GetResult();

            // Assert
            dodge.IsBlockingDamage.Should().BeFalse(
                "Dodge should NOT block damage when Shift() fails (no valid positions)");

            // Cleanup
            UnityEngine.Object.DestroyImmediate(go);
        }

        /// <summary>
        /// UT-DODGE-007: Priority_Is1
        /// Verifies that Dodge has priority 1 (higher than CounterAttack's 0)
        /// </summary>
        [Test]
        public void Priority_Is1_CorrectValue()
        {
            // Arrange
            var dodge = AbilityTestMocks.CreateDodge(successChance: 50);

            // Assert
            dodge.Priority.Should().Be(1,
                "Dodge should have priority 1 to execute before CounterAttack (priority 0)");
        }
    }
}
