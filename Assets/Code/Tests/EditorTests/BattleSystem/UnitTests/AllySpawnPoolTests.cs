using Code.Animals;
using Code.Battle.PreBattle;
using FluentAssertions;
using NUnit.Framework;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class AllySpawnPoolTests
    {
        private AllySpawnPool _pool;

        [SetUp]
        public void SetUp()
        {
            _pool = new AllySpawnPool();
        }

        /// <summary>UT-POOL-001: Enqueue hands out the types in insertion order</summary>
        [Test]
        public void Enqueue_KeepsInsertionOrder()
        {
            // Act
            SeedPool();

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
            SeedPool();

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
            SeedPool();

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
            SeedPool();

            // Act
            _pool.Clear();

            // Assert
            _pool.Remaining.Should().Be(0);
            _pool.HasNext.Should().BeFalse();
        }

        /// <summary>UT-POOL-006: A cleared pool can be refilled for the next level</summary>
        [Test]
        public void Enqueue_AfterClear_RestoresFullPool()
        {
            // Arrange
            SeedPool();
            _pool.TryTakeNext(out _);
            _pool.Clear();

            // Act
            SeedPool();

            // Assert
            _pool.Remaining.Should().Be(3);
            _pool.TryPeekNext(out var next).Should().BeTrue();
            next.Should().Be(AnimalType.Cheetah);
        }

        private void SeedPool()
        {
            _pool.Enqueue(AnimalType.Cheetah);
            _pool.Enqueue(AnimalType.Fox);
            _pool.Enqueue(AnimalType.Elephant);
        }
    }
}
