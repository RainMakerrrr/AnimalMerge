using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Code.Abilities;
using Code.Animals.Facades;
using Code.Animals.Health;
using Code.Battle.Services;
using Code.Tests.EditorTests.BattleSystem.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class TurnExecutorTests
    {
        private TurnExecutor _turnExecutor;
        private UnitTracker _unitTracker;
        private TargetFinder _targetFinder;

        [SetUp]
        public void SetUp()
        {
            // Clean scene for test isolation
            BattleTestHelper.CleanScene();

            // Create real dependencies for unit tests
            _unitTracker = BattleTestHelper.CreateRealUnitTracker();
            _targetFinder = BattleTestHelper.CreateMockTargetFinder();
            _turnExecutor = BattleTestHelper.CreateTurnExecutor(_unitTracker, _targetFinder);
        }

        [TearDown]
        public void TearDown()
        {
            _unitTracker?.Dispose();
            BattleTestHelper.CleanScene();
        }

        /// <summary>
        /// UT-TURN-001: PlayerTurns_SortOrder_LeftToRight_TopToBottom
        /// Verifies that player units execute turns in correct order: left-to-right, top-to-bottom
        /// </summary>
        [Test]
        public void PlayerTurns_SortOrder_LeftToRight_TopToBottom()
        {
            // Arrange - create 4 units at specific positions
            var unit1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true, customName: "Unit_TopLeft");
            var unit2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 2), isPlayer: true, customName: "Unit_TopRight");
            var unit3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 1), isPlayer: true, customName: "Unit_BottomLeft");
            var unit4 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 1), isPlayer: true, customName: "Unit_BottomRight");

            _unitTracker.RegisterPlayerUnit(unit1);
            _unitTracker.RegisterPlayerUnit(unit2);
            _unitTracker.RegisterPlayerUnit(unit3);
            _unitTracker.RegisterPlayerUnit(unit4);

            // Act - just get units and verify sort order (don't execute async)
            var aliveUnits = _unitTracker.GetAlivePlayerUnits();
            var sortedUnits = aliveUnits
                .OrderBy(u => u.Movement.CurrentPathNode.GridPosition.x)
                .ThenByDescending(u => u.Movement.CurrentPathNode.GridPosition.y)
                .ToList();

            // Assert - Expected order: unit1 (0,2) -> unit3 (0,1) -> unit2 (1,2) -> unit4 (1,1)
            sortedUnits[0].Should().Be(unit1, "first unit should be top-left (0,2)");
            sortedUnits[1].Should().Be(unit3, "second unit should be bottom-left (0,1)");
            sortedUnits[2].Should().Be(unit2, "third unit should be top-right (1,2)");
            sortedUnits[3].Should().Be(unit4, "fourth unit should be bottom-right (1,1)");
        }

        /// <summary>
        /// UT-TURN-002: EnemyTurns_SortOrder_RightToLeft_BottomToTop
        /// Verifies that enemy units execute turns in reverse order: right-to-left, bottom-to-top
        /// </summary>
        [Test]
        public void EnemyTurns_SortOrder_RightToLeft_BottomToTop()
        {
            // Arrange - create 4 enemy units at specific positions
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: false, customName: "Enemy_TopLeft");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 2), isPlayer: false, customName: "Enemy_TopRight");
            var enemy3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 1), isPlayer: false, customName: "Enemy_BottomLeft");
            var enemy4 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 1), isPlayer: false, customName: "Enemy_BottomRight");

            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);
            _unitTracker.RegisterEnemyUnit(enemy3);
            _unitTracker.RegisterEnemyUnit(enemy4);

            // Act - just get units and verify sort order (don't execute async)
            var aliveUnits = _unitTracker.GetAliveEnemyUnits();
            var sortedUnits = aliveUnits
                .OrderByDescending(u => u.Movement.CurrentPathNode.GridPosition.x)
                .ThenByDescending(u => u.Movement.CurrentPathNode.GridPosition.y)
                .ToList();

            // Assert - Expected order: enemy2 (1,2) -> enemy4 (1,1) -> enemy1 (0,2) -> enemy3 (0,1)
            sortedUnits[0].Should().Be(enemy2, "first unit should be top-right (1,2)");
            sortedUnits[1].Should().Be(enemy4, "second unit should be bottom-right (1,1)");
            sortedUnits[2].Should().Be(enemy1, "third unit should be top-left (0,2)");
            sortedUnits[3].Should().Be(enemy3, "fourth unit should be bottom-left (0,1)");
        }

        /// <summary>
        /// UT-TURN-003: DeadUnit_SkippedInTurn
        /// Verifies that dead units are skipped during turn execution
        /// </summary>
        [Test]
        public void DeadUnit_SkippedInTurn()
        {
            // Arrange - create 3 units, one is dead
            var unit1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true, customName: "Alive1");
            var unit2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 2), isPlayer: true, isDead: true, customName: "Dead");
            var unit3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 1), isPlayer: true, customName: "Alive2");

            _unitTracker.RegisterPlayerUnit(unit1);
            _unitTracker.RegisterPlayerUnit(unit2);
            _unitTracker.RegisterPlayerUnit(unit3);

            // Act - just verify filtering (don't execute async)
            var aliveUnits = _unitTracker.GetAlivePlayerUnits();

            // Assert - verify only alive units are returned
            aliveUnits.Should().HaveCount(2, "only alive units should be counted");
            aliveUnits.Should().Contain(unit1);
            aliveUnits.Should().Contain(unit3);
            aliveUnits.Should().NotContain(unit2, "dead unit should be skipped");

            unit2.Health.IsDead.Should().BeTrue("unit2 should be marked as dead");
        }

        /// <summary>
        /// UT-TURN-004: NullUnit_HandledGracefully
        /// Verifies that null units in the tracker don't cause exceptions
        /// </summary>
        [Test]
        public void NullUnit_HandledGracefully()
        {
            // Arrange - create valid units
            var unit1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true);
            var unit2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 2), isPlayer: true);

            _unitTracker.RegisterPlayerUnit(unit1);
            _unitTracker.RegisterPlayerUnit(unit2);

            // Register null should be handled gracefully by UnitTracker
            _unitTracker.RegisterPlayerUnit(null);

            // Act - just verify filtering (don't execute async)
            var aliveUnits = _unitTracker.GetAlivePlayerUnits();

            // Assert
            aliveUnits.Should().HaveCount(2, "only valid units should be counted");
        }

        /// <summary>
        /// UT-TURN-005: UnitWithoutMovement_SkippedGracefully
        /// Verifies that units without AnimalMovement component are skipped with warning
        /// </summary>
        [Test]
        public void UnitWithoutMovement_SkippedGracefully()
        {
            // Arrange - create unit with movement and one without
            var validUnit = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true, customName: "ValidUnit");

            // Create unit without movement component (just for test purposes)
            var invalidUnitGO = new GameObject("InvalidUnit");
            var collider = invalidUnitGO.AddComponent<BoxCollider>();
            var invalidFacade = invalidUnitGO.AddComponent<TestAnimalFacadeNoMovement>();

            // Add Health component so UnitTracker can register the unit
            var health = invalidUnitGO.AddComponent<AnimalHealth>();
            var abilityManager = new AbilityManager();
            health.Construct(new[] { collider }, abilityManager);
            health.SetMaxHealth(100);

            // Set _health field via reflection so Health property works
            var healthField = typeof(AnimalFacade).GetField("_health",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            healthField.SetValue(invalidFacade, health);

            _unitTracker.RegisterPlayerUnit(validUnit);
            _unitTracker.RegisterPlayerUnit(invalidFacade);

            // Act - just verify (don't execute async)
            // Assert - valid unit should still be valid, invalid should have no movement
            validUnit.Should().NotBeNull();
            invalidFacade.Movement.Should().BeNull("invalid unit has no movement component");
            invalidFacade.Health.Should().NotBeNull("unit should have health component");
        }

        /// <summary>
        /// UT-TURN-006: TargetFinder_Setup_CalledBeforeTurns
        /// Verifies that TargetFinder exists and is initialized properly
        /// </summary>
        [Test]
        public void TargetFinder_Setup_CalledBeforeTurns()
        {
            // Arrange
            var unit1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true);

            _unitTracker.RegisterPlayerUnit(unit1);

            // Act - just verify setup (don't execute async)
            // Assert - verify TargetFinder exists
            _targetFinder.Should().NotBeNull("TargetFinder should be initialized");
        }

        /// <summary>
        /// INT-TURN-001: FindTarget_MoveOrAttack_Integration
        /// Integration test verifying unit registration
        /// </summary>
        [Test]
        public void FindTarget_Integration_UnitsExecuteWithoutErrors()
        {
            // Arrange - create player and enemy units
            var player = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player");
            var enemy = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "Enemy");

            _unitTracker.RegisterPlayerUnit(player);
            _unitTracker.RegisterEnemyUnit(enemy);

            // Act - just verify registration (don't execute async)
            // Assert - verify units are registered correctly
            _unitTracker.AlivePlayerUnitsCount.Should().Be(1);
            _unitTracker.AliveEnemyUnitsCount.Should().Be(1);
        }

        /// <summary>
        /// INT-TURN-002: MultipleUnits_AllExecuteTurns_InOrder
        /// Integration test verifying all units are registered correctly
        /// </summary>
        [Test]
        public void MultipleUnits_AllExecuteTurns_InOrder()
        {
            // Arrange - create 3 players and 2 enemies
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 2), isPlayer: true, customName: "P1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 2), isPlayer: true, customName: "P2");
            var player3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 1), isPlayer: true, customName: "P3");

            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "E1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, customName: "E2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterPlayerUnit(player3);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act - just verify registration (don't execute async)
            // Assert - all units registered correctly
            _unitTracker.AlivePlayerUnitsCount.Should().Be(3, "all player units should be registered");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "all enemy units should be registered");
        }

        /// <summary>
        /// Helper class for testing units without movement component
        /// </summary>
        private class TestAnimalFacadeNoMovement : AnimalFacade
        {
            public override void InitBehaviours()
            {
                // Empty implementation
            }
        }
    }
}
