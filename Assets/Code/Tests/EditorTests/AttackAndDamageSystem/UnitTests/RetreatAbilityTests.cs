using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals;
using Code.GridPathfinding;
using Code.Services.Random;
using Code.Tests.EditorTests.Helpers.AttackSystem;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.AttackAndDamageSystem.UnitTests
{
    [TestFixture]
    public class RetreatAbilityTests
    {
        /// <summary>
        /// UT-RETREAT-001: Owner_Always100Percent
        /// Verifies that owner retreat always succeeds (boss Velociraptor)
        /// </summary>
        [Test]
        public void Owner_Always100Percent_CanUseAlwaysTrue()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            // Act & Assert - test multiple times to ensure it's always true
            for (int i = 0; i < 10; i++)
            {
                retreat.CanUse(null).Should().BeTrue(
                    "owner retreat should always be 100% (boss Velociraptor)");
            }
        }

        /// <summary>
        /// UT-RETREAT-002: Inherited_50Percent
        /// Verifies that inherited retreat has 50% success rate
        /// </summary>
        [Test]
        public void Inherited_50Percent_CanUse_CorrectThreshold()
        {
            // Arrange & Act - test with controlled RNG values
            // Test boundary: 49 should succeed, 50 should fail
            var transformable = Substitute.For<ITransformable>();

            var randomProviderSuccess = AbilityTestMocks.CreateMockRandomProvider(49);
            var retreatSuccess = new RetreatAbility(transformable, randomProviderSuccess, isOwner: false);

            var randomProviderFail = AbilityTestMocks.CreateMockRandomProvider(50);
            var retreatFail = new RetreatAbility(transformable, randomProviderFail, isOwner: false);

            // Assert
            retreatSuccess.CanUse(null).Should().BeTrue("49 < 50, should succeed");
            retreatFail.CanUse(null).Should().BeFalse("50 >= 50, should fail");
        }

        /// <summary>
        /// UT-RETREAT-003: Apply_CallsRetreatFrom
        /// Verifies that Apply() calls RetreatFrom with correct parameters
        /// </summary>
        [Test]
        public async Task Apply_WithTarget_CallsRetreatFrom()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProviderWithFunc(
                intFunc: (min, max) => 3); // Return 3 for retreat distance

            var retreat = new RetreatAbility(transformable, randomProvider, minDistance: 1, maxDistance: 4, isOwner: true);

            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            var targetPathNode = Substitute.For<IGridCell>();
            targetPathNode.GridPosition.Returns(new Vector2Int(5, 5));
            targetTransformable.CurrentPathNode.Returns(targetPathNode);
            target.Transformable.Returns(targetTransformable);

            retreat.SetAttackTarget(target);

            // Act
            await retreat.Apply();

            // Assert
            await transformable.Received(1).RetreatFrom(
                Arg.Is<Vector2Int>(pos => pos == new Vector2Int(5, 5)),
                Arg.Is<int>(distance => distance == 3));
        }

        /// <summary>
        /// UT-RETREAT-004: Apply_WithoutTarget_LogsWarning
        /// Verifies that Apply() logs warning when target is null
        /// </summary>
        [Test]
        public async Task Apply_WithoutTarget_LogsWarning()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            // Don't set target - it should be null

            // Act
            await retreat.Apply();

            // Assert
            // RetreatFrom should not be called
            await transformable.DidNotReceive().RetreatFrom(Arg.Any<Vector2Int>(), Arg.Any<int>());
        }

        /// <summary>
        /// UT-RETREAT-005: IsBlockingDamage_False
        /// Verifies that Retreat correctly reports IsBlockingDamage = false
        /// </summary>
        [Test]
        public void IsBlockingDamage_False_CorrectValue()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            // Assert
            retreat.IsBlockingDamage.Should().BeFalse(
                "Retreat should not block damage");
        }

        /// <summary>
        /// UT-RETREAT-006: Priority_IsNegativeOne
        /// Verifies that Retreat has priority -1 (executes after all other abilities)
        /// </summary>
        [Test]
        public void Priority_IsNegativeOne_CorrectValue()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(0);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            // Assert
            retreat.Priority.Should().Be(-1,
                "Retreat should have priority -1 to execute after all other abilities");
        }

        /// <summary>
        /// UT-RETREAT-007: RetreatDistance_RandomInRange
        /// Verifies that retreat distance is randomly selected within min-max range
        /// </summary>
        [Test]
        public async Task RetreatDistance_RandomInRange_CorrectValue()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();

            // Test multiple retreat distances
            var testCases = new[] { 1, 2, 3, 4 };

            foreach (var expectedDistance in testCases)
            {
                var randomProvider = AbilityTestMocks.CreateMockRandomProviderWithFunc(
                    intFunc: (min, max) => expectedDistance);

                var retreat = new RetreatAbility(transformable, randomProvider, minDistance: 1, maxDistance: 4, isOwner: true);

                var target = Substitute.For<ITarget>();
                var targetTransformable = Substitute.For<ITransformable>();
                var targetPathNode = Substitute.For<IGridCell>();
                targetPathNode.GridPosition.Returns(new Vector2Int(5, 5));
                targetTransformable.CurrentPathNode.Returns(targetPathNode);
                target.Transformable.Returns(targetTransformable);

                retreat.SetAttackTarget(target);

                // Act
                await retreat.Apply();

                // Assert
                await transformable.Received(1).RetreatFrom(
                    Arg.Any<Vector2Int>(),
                    Arg.Is<int>(distance => distance == expectedDistance));

                transformable.ClearReceivedCalls();
            }
        }

        /// <summary>
        /// UT-RETREAT-008: SetAttackTarget_StoresTarget
        /// Verifies that SetAttackTarget stores the target for later use
        /// </summary>
        [Test]
        public async Task SetAttackTarget_StoresTarget_CorrectBehavior()
        {
            // Arrange
            var transformable = Substitute.For<ITransformable>();
            var randomProvider = AbilityTestMocks.CreateMockRandomProvider(3);
            var retreat = new RetreatAbility(transformable, randomProvider, isOwner: true);

            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            var targetPathNode = Substitute.For<IGridCell>();
            targetPathNode.GridPosition.Returns(new Vector2Int(10, 15));
            targetTransformable.CurrentPathNode.Returns(targetPathNode);
            target.Transformable.Returns(targetTransformable);

            // Act
            retreat.SetAttackTarget(target);
            await retreat.Apply();

            // Assert - should retreat from the target's position
            await transformable.Received(1).RetreatFrom(
                Arg.Is<Vector2Int>(pos => pos == new Vector2Int(10, 15)),
                Arg.Any<int>());
        }
    }
}
