using Code.Battle;
using Code.Battle.Services;
using Code.Tests.EditorTests.BattleSystem.Helpers;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class VictoryConditionCheckerTests
    {
        private VictoryConditionChecker _checker;
        private UnitTracker _unitTracker;

        [SetUp]
        public void SetUp()
        {
            BattleTestHelper.CleanScene();
            _unitTracker = BattleTestHelper.CreateRealUnitTracker();
            _checker = BattleTestHelper.CreateRealVictoryChecker(_unitTracker);
        }

        [TearDown]
        public void TearDown()
        {
            _unitTracker?.Dispose();
            BattleTestHelper.CleanScene();
        }

        /// <summary>
        /// UT-VIC-001: AllEnemiesDead_ReturnsVictory
        /// Verifies that victory is declared when all enemies are dead
        /// </summary>
        [Test]
        public void AllEnemiesDead_ReturnsVictory()
        {
            // Arrange - 3 alive players, 0 alive enemies, no boss
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");
            var player3 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(2, 0), isPlayer: true, customName: "Player3");

            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, isDead: true, customName: "DeadEnemy1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isDead: true, customName: "DeadEnemy2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterPlayerUnit(player3);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(3, "all players should be alive");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(0, "all enemies should be dead");
            _unitTracker.HasAliveBoss.Should().BeFalse("no boss should be alive");
            result.Should().Be(BattleResult.Victory, "all enemies dead should result in Victory");
        }

        /// <summary>
        /// UT-VIC-002: AllPlayersDead_ReturnsDefeat
        /// Verifies that defeat is declared when all player units are dead
        /// </summary>
        [Test]
        public void AllPlayersDead_ReturnsDefeat()
        {
            // Arrange - 0 alive players, 2 alive enemies
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, isDead: true, customName: "DeadPlayer1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, isDead: true, customName: "DeadPlayer2");

            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "AliveEnemy1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, customName: "AliveEnemy2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(0, "all players should be dead");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "enemies should be alive");
            result.Should().Be(BattleResult.Defeat, "all players dead should result in Defeat");
        }

        /// <summary>
        /// UT-VIC-003: BothAlive_ReturnsOngoing
        /// Verifies that battle continues when both sides have living units
        /// </summary>
        [Test]
        public void BothAlive_ReturnsOngoing()
        {
            // Arrange - 2 alive players, 2 alive enemies
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");

            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, customName: "Enemy1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, customName: "Enemy2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(2, "players should be alive");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "enemies should be alive");
            result.Should().Be(BattleResult.Ongoing, "both sides alive should result in Ongoing");
        }

        /// <summary>
        /// UT-VIC-004: BossDead_ReturnsVictory_EvenIfOtherEnemiesAlive
        /// Verifies that victory is declared when boss is dead, regardless of regular enemies
        /// According to spec: boss death is the primary victory condition
        /// </summary>
        [Test]
        public void BossDead_ReturnsVictory_EvenIfOtherEnemiesAlive()
        {
            // Arrange - 2 alive players, boss dead, but 2 regular enemies alive
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");

            var boss = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, isDead: true, isBoss: true, customName: "Boss");
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, customName: "RegularEnemy1");
            var enemy2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(7, 7), isPlayer: false, customName: "RegularEnemy2");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterEnemyUnit(boss);
            _unitTracker.RegisterEnemyUnit(enemy1);
            _unitTracker.RegisterEnemyUnit(enemy2);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(2, "players should be alive");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(2, "regular enemies should still be alive");
            _unitTracker.HasAliveBoss.Should().BeFalse("boss should be dead");
            result.Should().Be(BattleResult.Victory,
                "boss death should trigger victory regardless of regular enemies");
        }

        /// <summary>
        /// UT-VIC-005: BossAlive_OngoingBattle
        /// Verifies that battle continues while boss is alive, even if other enemies are dead
        /// </summary>
        [Test]
        public void BossAlive_OngoingBattle()
        {
            // Arrange - 2 alive players, boss alive, regular enemies dead
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, customName: "Player1");
            var player2 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(1, 0), isPlayer: true, customName: "Player2");

            var boss = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, isBoss: true, customName: "Boss");
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(6, 6), isPlayer: false, isDead: true, customName: "DeadEnemy1");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterPlayerUnit(player2);
            _unitTracker.RegisterEnemyUnit(boss);
            _unitTracker.RegisterEnemyUnit(enemy1);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(2, "players should be alive");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(1, "only boss should be alive");
            _unitTracker.HasAliveBoss.Should().BeTrue("boss should be alive");
            result.Should().Be(BattleResult.Ongoing, "battle should continue while boss is alive");
        }

        /// <summary>
        /// UT-VIC-006: AllDead_Stalemate
        /// Verifies behavior when all units on both sides are dead
        /// Expected: Defeat (players failed to survive)
        /// </summary>
        [Test]
        public void AllDead_Stalemate_ReturnsDefeat()
        {
            // Arrange - all units dead
            var player1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(0, 0), isPlayer: true, isDead: true, customName: "DeadPlayer");
            var enemy1 = BattleTestHelper.CreateMockAnimalFacade(
                new Vector2Int(5, 5), isPlayer: false, isDead: true, customName: "DeadEnemy");

            _unitTracker.RegisterPlayerUnit(player1);
            _unitTracker.RegisterEnemyUnit(enemy1);

            // Act
            var result = _checker.CheckBattleConditions();

            // Assert
            _unitTracker.AlivePlayerUnitsCount.Should().Be(0, "all players should be dead");
            _unitTracker.AliveEnemyUnitsCount.Should().Be(0, "all enemies should be dead");
            result.Should().Be(BattleResult.Defeat,
                "when all units dead, players failed so result should be Defeat");
        }
    }
}
