using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Battle.PreBattle;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Tests.EditorTests.BattleSystem.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using Zenject;

namespace Code.Tests.EditorTests.BattleSystem.UnitTests
{
    [TestFixture]
    public class AllySpawnServiceTests
    {
        private PreBattleConfig _config;
        private AllySpawnPool _pool;
        private IAnimalSpawner _animalSpawner;
        private IUnitTracker _unitTracker;
        private SignalBus _signalBus;
        private AllySpawnService _service;

        [SetUp]
        public void SetUp()
        {
            BattleTestHelper.CleanScene();

            _config = BattleTestHelper.CreatePreBattleConfig(2);

            _pool = new AllySpawnPool();
            _pool.Enqueue(AnimalType.Cheetah);
            _pool.Enqueue(AnimalType.Fox);
            _pool.Enqueue(AnimalType.Elephant);

            _animalSpawner = BattleTestHelper.CreateMockAnimalSpawner();
            _unitTracker = BattleTestHelper.CreateMockUnitTracker();
            _signalBus = BattleTestHelper.CreatePreBattleSignalBus();

            _service = new AllySpawnService(_pool, _animalSpawner, _unitTracker, _config, _signalBus);
        }

        [TearDown]
        public void TearDown()
        {
            if (_config != null)
                Object.DestroyImmediate(_config);

            BattleTestHelper.CleanScene();
        }

        /// <summary>UT-SPAWNSRV-001: Every click spawns exactly one animal, in pool order</summary>
        [Test]
        public void RequestSpawn_SpawnsOneAnimalInPoolOrder()
        {
            // Arrange
            StubSpawnResult(CreateFacade("Ally1"));

            // Act
            bool firstSpawn = _service.RequestSpawn();

            // Assert
            firstSpawn.Should().BeTrue();
            _animalSpawner.Received(1).Spawn(AnimalType.Cheetah);
            _service.PoolRemaining.Should().Be(2, "one click consumes exactly one animal");
        }

        /// <summary>UT-SPAWNSRV-002: Every facade a single spawn produced is tracked (Chicken brings companions)</summary>
        [Test]
        public void RequestSpawn_RegistersEverySpawnedFacade()
        {
            // Arrange
            var main = CreateFacade("Chicken");
            var companions = new[] { CreateFacade("Chick1"), CreateFacade("Chick2"), CreateFacade("Chick3") };
            StubSpawnResult(main, companions[0], companions[1], companions[2]);

            // Act
            _service.RequestSpawn();

            // Assert
            _unitTracker.Received(1).RegisterPlayerUnit(main);
            foreach (var companion in companions)
            {
                _unitTracker.Received(1).RegisterPlayerUnit(companion);
            }
        }

        /// <summary>UT-SPAWNSRV-003: An empty pool spawns nothing</summary>
        [Test]
        public void RequestSpawn_WithEmptyPool_DoesNotSpawn()
        {
            // Arrange
            _pool.Clear();

            // Act
            bool spawned = _service.RequestSpawn();

            // Assert
            spawned.Should().BeFalse();
            _service.CanSpawn.Should().BeFalse();
            _animalSpawner.DidNotReceiveWithAnyArgs().Spawn(default);
        }

        /// <summary>UT-SPAWNSRV-004: A full merge grid blocks the spawn and keeps the animal in the pool</summary>
        [Test]
        public void RequestSpawn_WithoutFreeCell_KeepsPoolIntact()
        {
            // Arrange
            _animalSpawner.HasFreeCellFor(default).ReturnsForAnyArgs(false);

            // Act
            bool spawned = _service.RequestSpawn();

            // Assert
            spawned.Should().BeFalse();
            _service.CanSpawn.Should().BeFalse();
            _service.PoolRemaining.Should().Be(3, "a rejected spawn must not eat the animal");
            _animalSpawner.DidNotReceiveWithAnyArgs().Spawn(default);
        }

        /// <summary>UT-SPAWNSRV-005: A successful spawn reports the remaining pool size</summary>
        [Test]
        public void RequestSpawn_FiresAllySpawnedSignal()
        {
            // Arrange
            var unit = CreateFacade("Ally1");
            StubSpawnResult(unit);

            AllySpawnedSignal received = null;
            _signalBus.Subscribe<AllySpawnedSignal>(signal => received = signal);

            // Act
            _service.RequestSpawn();

            // Assert
            received.Should().NotBeNull();
            received.Unit.Should().Be(unit);
            received.PoolRemaining.Should().Be(2);
        }

        /// <summary>UT-SPAWNSRV-006: A reinforcement goes into the pool and is placed by an Add Animal click, not spawned outright</summary>
        [Test]
        public void QueueReinforcements_AddsToThePoolWithoutSpawning()
        {
            // Arrange
            _pool.Clear();

            // Act
            int queued = _service.QueueReinforcements();

            // Assert
            queued.Should().Be(1);
            _service.PoolRemaining.Should().Be(1, "the reinforcement waits for an Add Animal click");
            _animalSpawner.DidNotReceiveWithAnyArgs().Spawn(default);
            _unitTracker.DidNotReceiveWithAnyArgs().RegisterPlayerUnit(null);

            _pool.TryPeekNext(out var queuedType).Should().BeTrue();
            queuedType.Should().Be(AnimalType.Hedgehog, "the random type is drawn from PreBattleConfig.RandomPool");
        }

        private void StubSpawnResult(params AnimalFacade[] facades)
        {
            _animalSpawner.Spawn(default).ReturnsForAnyArgs(new List<AnimalFacade>(facades));
        }

        private static AnimalFacade CreateFacade(string name) =>
            BattleTestHelper.CreateMockAnimalFacade(Vector2Int.zero, isPlayer: true, customName: name);
    }
}
