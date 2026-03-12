using System.Linq;
using Code.Battle.Services;
using Code.Tests.EditorTests.BattleSystem.Helpers;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class UnitTrackerTests
    {
        private UnitTracker _unitTracker;

        [SetUp]
        public void SetUp()
        {
            BattleTestHelper.CleanScene();
            _unitTracker = BattleTestHelper.CreateRealUnitTracker();
        }

        [TearDown]
        public void TearDown()
        {
            _unitTracker?.Dispose();
            BattleTestHelper.CleanScene();
        }

        /// <summary>
        /// UT-TRACK-001: RegisterPlayer_IncreasesCount
        /// Verifies that registering player units increases the alive player count
        /// </summary>
        [Test]
        public void RegisterPlayer_IncreasesCount()
        {
            // Arrange
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");
            var player3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(2, 0), isPlayer: true, customName: "Player3");

            // Act
            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterPlayerUnit(player3);

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(3, "3 player units should be registered");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(0, "no enemy units should be registered");
        }

        /// <summary>
        /// UT-TRACK-002: RegisterEnemy_IncreasesCount
        /// Verifies that registering enemy units increases the alive enemy count
        /// </summary>
        [Test]
        public void RegisterEnemy_IncreasesCount()
        {
            // Arrange
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "Enemy1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, customName: "Enemy2");

            // Act
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Assert
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "2 enemy units should be registered");
            _unitTracker.AlivePlayerUnitsCount.Should().Be(0, "no player units should be registered");
        }

        /// <summary>
        /// UT-TRACK-003: UnitDies_DecreasesCount
        /// Verifies that when a unit dies, the alive count decreases
        /// </summary>
        [Test]
        public void UnitDies_DecreasesCount()
        {
            // Arrange - create alive player
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);

            _unitTracker.AlivePlayerUnitsCount.Should().Be(2, "initially 2 players alive");

            // Act - kill one unit
            player1.Health.SetMaxHealth(0); // Sets health to 0, marking unit as dead

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(1,
                "after one player dies, count should decrease to 1");
            player1.Health.IsDead.Should().BeTrue("player1 should be marked as dead");
            player2.Health.IsDead.Should().BeFalse("player2 should still be alive");
        }

        /// <summary>
        /// UT-TRACK-004: Reset_ClearsAllUnits
        /// Verifies that Reset() clears all registered units
        /// </summary>
        [Test]
        public void Reset_ClearsAllUnits()
        {
            // Arrange - register multiple units
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "Enemy1");
            var boss = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isBoss: true, customName: "Boss");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(boss);

            _unitTracker.AlivePlayerUnitsCount.Should().Be(2);
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2);
            _unitTracker.HasAliveBoss.Should().BeTrue();

            // Act
            _unitTracker.Reset();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(0, "all player units should be cleared");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(0, "all enemy units should be cleared");
            _unitTracker.HasAliveBoss.Should().BeFalse("boss flag should be cleared");
            _unitTracker.GetAlivePlayerUnits().Should().BeEmpty("player list should be empty");
            _unitTracker.GetAliveEnemyUnits().Should().BeEmpty("enemy list should be empty");
        }

        /// <summary>
        /// UT-TRACK-005: BossRegistration_SetsBossFlag
        /// Verifies that registering a boss unit sets the HasAliveBoss flag
        /// </summary>
        [Test]
        public void BossRegistration_SetsBossFlag()
        {
            // Arrange
            var regularEnemy = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, isBoss: false, customName: "RegularEnemy");
            var boss = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isBoss: true, customName: "Boss");

            // Act
            _unitTracker.RegisterEnemyUnit(regularEnemy);
            _unitTracker.HasAliveBoss.Should().BeFalse("no boss registered yet");

            _unitTracker.RegisterEnemyUnit(boss);

            // Assert
            _unitTracker.HasAliveBoss.Should().BeTrue("boss should be registered");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "both regular enemy and boss should be counted");
            boss.IsBoss.Should().BeTrue("boss flag should be set");
        }

        /// <summary>
        /// UT-TRACK-006: BossDies_ClearsBossFlag
        /// Verifies that when boss dies, HasAliveBoss flag becomes false
        /// </summary>
        [Test]
        public void BossDies_ClearsBossFlag()
        {
            // Arrange - register alive boss
            var boss = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isBoss: true, customName: "Boss");

            _unitTracker.RegisterEnemyUnit(boss);
            _unitTracker.HasAliveBoss.Should().BeTrue("boss should be alive initially");

            // Act - kill boss
            boss.Health.SetMaxHealth(0); // Sets health to 0, marking boss as dead

            // Assert
            _unitTracker.HasAliveBoss.Should().BeFalse(
                "after boss dies, HasAliveBoss should be false");
            boss.Health.IsDead.Should().BeTrue("boss should be marked as dead");
        }

        /// <summary>
        /// UT-TRACK-007: GetAliveUnits_ReturnsOnlyAlive
        /// Verifies that GetAlivePlayerUnits/GetAliveEnemyUnits return only living units
        /// </summary>
        [Test]
        public void GetAliveUnits_ReturnsOnlyAlive()
        {
            // Arrange - register 3 players, mark one as dead
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "AlivePlayer1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, isDead: true, customName: "DeadPlayer");
            var player3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(2, 0), isPlayer: true, customName: "AlivePlayer2");

            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "AliveEnemy");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isDead: true, customName: "DeadEnemy");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterPlayerUnit(player3);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act
            var alivePlayers = _unitTracker.GetAlivePlayerUnits();
            var aliveEnemies = _unitTracker.GetAliveEnemyUnits();

            // Assert
            alivePlayers.Should().HaveCount(2, "only 2 alive players should be returned");
            alivePlayers.Should().Contain(player1);
            alivePlayers.Should().Contain(player3);
            alivePlayers.Should().NotContain(player2, "dead player should not be included");

            aliveEnemies.Should().HaveCount(1, "only 1 alive enemy should be returned");
            aliveEnemies.Should().Contain(enemy1);
            aliveEnemies.Should().NotContain(enemy2, "dead enemy should not be included");

            _unitTracker.AlivePlayerUnitsCount.Should().Be(2);
            _unitTracker.AliveEnemyUnitsCount.Should().Be(1);
        }

        /// <summary>
        /// Additional test: RegisterNull_HandledGracefully
        /// Verifies that registering null units doesn't cause exceptions
        /// </summary>
        [Test]
        public void RegisterNull_HandledGracefully()
        {
            // Act - register null
            _unitTracker.RegisterPlayerUnit(null);
            _unitTracker.RegisterEnemyUnit(null);

            // Assert - no exceptions, counts remain 0
            _unitTracker.AlivePlayerUnitsCount.Should().Be(0);
            _unitTracker.AliveEnemyUnitsCount.Should().Be(0);
        }

        /// <summary>
        /// Additional test: RegisterSameUnit_Twice_OnlyCountsOnce
        /// Verifies that registering the same unit twice doesn't double-count
        /// </summary>
        [Test]
        public void RegisterSameUnit_Twice_OnlyCountsOnce()
        {
            // Arrange
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");

            // Act - register same unit twice
            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player1);

            // Assert - should only count once
            _unitTracker.AlivePlayerUnitsCount.Should().Be(1,
                "registering same unit twice should only count once");
        }
    }
}
