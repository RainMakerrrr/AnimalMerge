using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Battle.Services;
using Code.Battle.Signals;
using UnityEngine;
using Zenject;

namespace Code.Battle.PreBattle
{
    public class AllySpawnService : IAllySpawnService
    {
        private readonly IAllySpawnPool _pool;
        private readonly IAnimalSpawner _animalSpawner;
        private readonly IUnitTracker _unitTracker;
        private readonly PreBattleConfig _config;
        private readonly SignalBus _signalBus;

        public AllySpawnService(
            IAllySpawnPool pool,
            IAnimalSpawner animalSpawner,
            IUnitTracker unitTracker,
            PreBattleConfig config,
            SignalBus signalBus)
        {
            _pool = pool;
            _animalSpawner = animalSpawner;
            _unitTracker = unitTracker;
            _config = config;
            _signalBus = signalBus;
        }

        public int PoolRemaining => _pool.Remaining;

        public bool CanSpawn => _pool.TryPeekNext(out var next) && _animalSpawner.HasFreeCellFor(next);

        public bool RequestSpawn()
        {
            if (!_pool.TryPeekNext(out var next))
            {
                Debug.Log("[AllySpawnService] Ally pool is empty - nothing to spawn");
                return false;
            }

            if (!_animalSpawner.HasFreeCellFor(next))
            {
                Debug.LogWarning($"[AllySpawnService] No free merge cell for {next} - keeping it in the pool");
                return false;
            }

            _pool.TryTakeNext(out var type);
            return SpawnAndRegister(_animalSpawner.Spawn(type));
        }

        public int QueueStartingPool()
        {
            _pool.Clear();

            var startingPool = _config == null ? null : _config.StartingPool;
            if (startingPool == null || startingPool.Count == 0)
            {
                Debug.LogError("[AllySpawnService] PreBattleConfig has an empty starting pool");
                return 0;
            }

            foreach (var type in startingPool)
            {
                _pool.Enqueue(type);
            }

            Debug.Log($"[AllySpawnService] Queued {startingPool.Count} animal(s) from the starting pool");
            return startingPool.Count;
        }

        public int QueueReinforcements()
        {
            int requested = _config == null ? 0 : _config.ReinforcementsPerLevel;
            if (requested <= 0)
                return 0;

            var randomPool = _config.RandomPool;
            if (randomPool == null || randomPool.Count == 0)
            {
                Debug.LogWarning("[AllySpawnService] PreBattleConfig has an empty random pool - no reinforcements queued");
                return 0;
            }

            for (int i = 0; i < requested; i++)
            {
                _pool.Enqueue(randomPool[Random.Range(0, randomPool.Count)]);
            }

            Debug.Log($"[AllySpawnService] Queued {requested} random reinforcement(s) - the player places them with the Add Animal button");
            return requested;
        }

        private bool SpawnAndRegister(IReadOnlyList<AnimalFacade> spawned)
        {
            if (spawned == null || spawned.Count == 0)
            {
                Debug.LogWarning("[AllySpawnService] Spawn produced no units");
                return false;
            }

            foreach (var unit in spawned)
            {
                _unitTracker.RegisterPlayerUnit(unit);
            }

            _signalBus.Fire(new AllySpawnedSignal
            {
                Unit = spawned[0],
                PoolRemaining = _pool.Remaining
            });

            Debug.Log($"[AllySpawnService] Spawned and registered {spawned.Count} unit(s), {_pool.Remaining} left in pool");
            return true;
        }
    }
}
