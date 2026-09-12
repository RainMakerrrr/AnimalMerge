using System;
using System.Collections.Generic;
using Code.Animals;
using Code.Battle.Config;
using Code.Battle.PreBattle;
using Code.Battle.PreBattle.Rules;
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
    public class BattleReadinessServiceTests
    {
        private PreBattleConfig _config;
        private AllySpawnPool _pool;
        private IUnitTracker _unitTracker;
        private IAllySpawnService _spawnService;
        private SignalBus _signalBus;
        private MinAllyCountRule _minAllyRule;
        private PoolExhaustedRule _poolRule;
        private BattleReadinessService _service;

        [SetUp]
        public void SetUp()
        {
            _config = BattleTestHelper.CreatePreBattleConfig(2);

            _pool = new AllySpawnPool();
            _unitTracker = BattleTestHelper.CreateMockUnitTracker();
            _signalBus = BattleTestHelper.CreatePreBattleSignalBus();
            _minAllyRule = new MinAllyCountRule(_config, _unitTracker, _signalBus);

            _spawnService = Substitute.For<IAllySpawnService>();
            _spawnService.CanSpawn.Returns(true);

            _poolRule = new PoolExhaustedRule(_pool, _spawnService, _signalBus);

            var rules = new List<IBattleReadinessRule>
            {
                _poolRule,
                _minAllyRule
            };

            _service = new BattleReadinessService(rules, _unitTracker, _signalBus);
        }

        [TearDown]
        public void TearDown()
        {
            _service?.Dispose();
            _minAllyRule?.Dispose();
            _poolRule?.Dispose();

            if (_config != null)
                UnityEngine.Object.DestroyImmediate(_config);
        }

        /// <summary>UT-READY-001: The battle cannot start while animals are left in the starting pool</summary>
        [Test]
        public void PendingStartingPool_BlocksBattleStart()
        {
            // Arrange
            SeedStartingPool();
            SetAllyCount(1);

            // Act
            StartPhase(isStartingPool: true);

            // Assert
            _service.CanStartBattle.Should().BeFalse();
        }

        /// <summary>UT-READY-002: The first level is ready as soon as the starting pool is placed</summary>
        [Test]
        public void FirstLevel_WithDrainedPool_AllowsBattleStart()
        {
            // Arrange
            SeedStartingPool();
            SetAllyCount(0);
            StartPhase(isStartingPool: true);

            // Act
            DrainPool();
            SetAllyCount(3);
            RaisePlayerUnitsChanged();

            // Assert
            _service.CanStartBattle.Should().BeTrue();
        }

        /// <summary>UT-READY-003: Merging the whole starting pool into one animal must not lock the player out</summary>
        [Test]
        public void FirstLevel_MergedIntoSingleAlly_AllowsBattleStart()
        {
            // Arrange
            SeedStartingPool();
            SetAllyCount(0);
            StartPhase(isStartingPool: true);

            // Act
            DrainPool();
            SetAllyCount(1);
            RaisePlayerUnitsChanged();

            // Assert
            _service.CanStartBattle.Should().BeTrue();
        }

        /// <summary>
        /// UT-READY-004: A later level counts the allies the player has fielded, not the ones left
        /// after merging - merging two allies into one must never lock the player out
        /// </summary>
        [Test]
        public void LaterLevel_MergedDownToSingleAlly_AllowsBattleStart()
        {
            // Arrange - phase begins with two allies and no starting pool
            _pool.Clear();
            SetAllyCount(2);
            StartPhase();

            // Act - the player merges them into one
            SetAllyCount(1);
            RaisePlayerUnitsChanged();

            // Assert - the high-water mark of 2 was reached and one ally is still alive
            _service.CanStartBattle.Should().BeTrue();
        }

        /// <summary>
        /// UT-READY-005: A later level with allies already on the field is ready right away -
        /// its queued reinforcement is optional and must not gate the Battle button
        /// </summary>
        [Test]
        public void LaterLevel_WithAlliesOnField_AllowsBattleStartWithPendingReinforcement()
        {
            // Arrange
            _pool.Clear();
            _pool.Enqueue(AnimalType.Hedgehog);
            SetAllyCount(2);

            // Act
            StartPhase();

            // Assert
            _service.CanStartBattle.Should().BeTrue();
            _pool.Remaining.Should().Be(1, "the reinforcement is still available via the Add Animal button");
        }

        /// <summary>UT-READY-006: A stage that can only field one ally stays startable</summary>
        [Test]
        public void LaterStage_WithSingleAvailableAlly_AllowsBattleStart()
        {
            // Arrange
            _pool.Clear();
            SetAllyCount(1);

            // Act
            StartPhase();

            // Assert
            _service.CanStartBattle.Should().BeTrue();
        }

        /// <summary>UT-READY-007: The readiness signal is fired once per state flip, not per evaluation</summary>
        [Test]
        public void ReadinessChangedSignal_FiresOnlyOnFlip()
        {
            // Arrange
            _pool.Clear();
            SetAllyCount(2);

            var received = new List<bool>();
            _signalBus.Subscribe<BattleReadinessChangedSignal>(signal => received.Add(signal.CanStartBattle));

            // Act - merging down to one ally keeps the high-water mark, losing the last one flips
            StartPhase();
            _service.Evaluate();
            RaisePlayerUnitsChanged();

            SetAllyCount(1);
            RaisePlayerUnitsChanged();

            SetAllyCount(0);
            RaisePlayerUnitsChanged();
            RaisePlayerUnitsChanged();

            // Assert
            received.Should().Equal(true, false);
        }

        /// <summary>UT-READY-008: A deactivated phase never reports readiness</summary>
        [Test]
        public void Deactivate_BlocksBattleStart()
        {
            // Arrange
            _pool.Clear();
            SetAllyCount(2);
            StartPhase();

            // Act
            _service.Deactivate();

            // Assert
            _service.CanStartBattle.Should().BeFalse();
        }

        private void StartPhase(bool isStartingPool = false)
        {
            _signalBus.Fire(new PreBattlePhaseStartedSignal
            {
                IsLevelStart = true,
                IsStartingPool = isStartingPool,
                PoolRemaining = _pool.Remaining
            });

            _service.Activate();
        }

        private void SetAllyCount(int count) => _unitTracker.AlivePlayerUnitsCount.Returns(count);

        private void RaisePlayerUnitsChanged() => _unitTracker.PlayerUnitsChanged += Raise.Event<Action>();

        private void SeedStartingPool()
        {
            _pool.Clear();
            _pool.Enqueue(AnimalType.Cheetah);
            _pool.Enqueue(AnimalType.Fox);
            _pool.Enqueue(AnimalType.Elephant);
        }

        private void DrainPool()
        {
            while (_pool.TryTakeNext(out _))
            {
            }
        }
    }
}
