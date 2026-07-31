using Code.Animals;
using Code.Battle.Config;
using Code.Battle.PreBattle;
using Code.Tests.EditorTests.BattleSystem.Helpers;
using FluentAssertions;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class AllySpawnPoolTests
    {
        private PreBattleConfig _config;
        private AllySpawnPool _pool;

        [SetUp]
        public void SetUp()
        {
            _config = BattleTestHelper.CreatePreBattleConfig(
                2, AnimalType.Cheetah, AnimalType.Fox, AnimalType.Elephant);
            _pool = new AllySpawnPool(_config);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
                Object.DestroyImmediate(_config);
        }

        /// <summary>UT-POOL-001: RefillFromConfig hands out the types in config order</summary>
        [Test]
        public void RefillFromConfig_KeepsConfigOrder()
        {
            // Act
            _pool.RefillFromConfig();

            // Assert
            _pool.Remaining.Should().Be(3);

            _pool.TryTakeNext(out var first).Should().BeTrue();
            _pool.TryTakeNext(out var second).Should().BeTrue();
            _pool.TryTakeNext(out var third).Should().BeTrue();

            first.Should().Be(AnimalType.Cheetah);
            second.Should().Be(AnimalType.Fox);
            third.Should().Be(AnimalType.Elephant);
        }

        /// <summary>UT-POOL-002: Taking an animal decreases the remaining count</summary>
        [Test]
        public void TryTakeNext_DecrementsRemaining()
        {
            // Arrange
            _pool.RefillFromConfig();

            // Act
            _pool.TryTakeNext(out _);

            // Assert
            _pool.Remaining.Should().Be(2);
            _pool.HasNext.Should().BeTrue();
        }

        /// <summary>UT-POOL-003: Peeking does not consume the animal</summary>
        [Test]
        public void TryPeekNext_DoesNotConsume()
        {
            // Arrange
            _pool.RefillFromConfig();

            // Act
            _pool.TryPeekNext(out var peeked).Should().BeTrue();

            // Assert
            peeked.Should().Be(AnimalType.Cheetah);
            _pool.Remaining.Should().Be(3);
        }

        /// <summary>UT-POOL-004: An empty pool reports no next animal</summary>
        [Test]
        public void TryTakeNext_OnEmptyPool_ReturnsFalse()
        {
            // Act
            bool taken = _pool.TryTakeNext(out _);

            // Assert
            taken.Should().BeFalse();
            _pool.HasNext.Should().BeFalse();
            _pool.Remaining.Should().Be(0);
        }

        /// <summary>UT-POOL-005: Clear drops every pending animal</summary>
        [Test]
        public void Clear_EmptiesPool()
        {
            // Arrange
            _pool.RefillFromConfig();

            // Act
            _pool.Clear();

            // Assert
            _pool.Remaining.Should().Be(0);
            _pool.HasNext.Should().BeFalse();
        }

        /// <summary>UT-POOL-006: A cleared pool can be refilled for the next level</summary>
        [Test]
        public void RefillFromConfig_AfterClear_RestoresFullPool()
        {
            // Arrange
            _pool.RefillFromConfig();
            _pool.TryTakeNext(out _);
            _pool.Clear();

            // Act
            _pool.RefillFromConfig();

            // Assert
            _pool.Remaining.Should().Be(3);
            _pool.TryPeekNext(out var next).Should().BeTrue();
            next.Should().Be(AnimalType.Cheetah);
        }
    }
}
