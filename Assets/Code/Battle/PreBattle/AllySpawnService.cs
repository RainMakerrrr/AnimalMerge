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
        private readonly IAnimalRosterService _roster;
        private readonly PreBattleConfig _config;
        private readonly SignalBus _signalBus;
        private readonly HashSet<AnimalType> _inspectedTypes = new HashSet<AnimalType>();

        public AllySpawnService(
            IAllySpawnPool pool,
            IAnimalSpawner animalSpawner,
            IUnitTracker unitTracker,
            IAnimalRosterService roster,
            PreBattleConfig config,
            SignalBus signalBus)
        {
            _pool = pool;
            _animalSpawner = animalSpawner;
            _unitTracker = unitTracker;
            _roster = roster;
            _config = config;
            _signalBus = signalBus;
        }

        public int PoolRemaining => _pool.Remaining;

        public int PoolTotal => _pool.Total;

        public bool CanSpawn => TryFindPlaceable(out _);

        public bool RequestSpawn()
        {
            if (_pool.Remaining == 0)
            {
                Debug.Log("[AllySpawnService] Ally pool is empty - nothing to spawn");
                return false;
            }

            if (!TryFindPlaceable(out int index))
            {
                Debug.LogWarning("[AllySpawnService] No free deployment cell for any pending animal - the pool stays intact");
                return false;
            }

            _pool.TryTakeAt(index, out var type);
            return SpawnAndRegister(_animalSpawner.Spawn(type));
        }

        public int QueueStartingPool()
        {
            _pool.Clear();

            var availableAnimals = _roster.AvailableAnimals;
            if (availableAnimals == null || availableAnimals.Count == 0)
            {
                Debug.LogError("[AllySpawnService] The animal roster is empty - nothing to queue as the starting pool");
                return 0;
            }

            foreach (var type in availableAnimals)
            {
                _pool.Enqueue(type);
            }

            Debug.Log($"[AllySpawnService] Queued {availableAnimals.Count} animal(s) from the level {_roster.CurrentLevel} roster");
            return availableAnimals.Count;
        }

        public int QueueReinforcements()
        {
            int requested = _config == null ? 0 : _config.ReinforcementsPerLevel;
            if (requested <= 0)
                return 0;

            var availableAnimals = _roster.AvailableAnimals;
            if (availableAnimals == null || availableAnimals.Count == 0)
            {
                Debug.LogWarning("[AllySpawnService] The animal roster is empty - no reinforcements queued");
                return 0;
            }

            int guaranteed = EnqueueNewlyUnlocked(requested);

            for (int i = guaranteed; i < requested; i++)
            {
                _pool.Enqueue(availableAnimals[Random.Range(0, availableAnimals.Count)]);
            }

            Debug.Log($"[AllySpawnService] Queued {requested} reinforcement(s), {guaranteed} of them newly unlocked - the player places them with the Add Animal button");
            return requested;
        }

        private int EnqueueNewlyUnlocked(int limit)
        {
            if (_config == null || !_config.GuaranteeNewlyUnlockedReinforcement)
                return 0;

            var newlyUnlocked = _roster.NewlyUnlockedAnimals;
            if (newlyUnlocked == null || newlyUnlocked.Count == 0)
                return 0;

            int count = Mathf.Min(limit, newlyUnlocked.Count);

            for (int i = 0; i < count; i++)
            {
                _pool.Enqueue(newlyUnlocked[i]);
            }

            return count;
        }

        private bool TryFindPlaceable(out int index)
        {
            var pending = _pool.PendingAnimals;

            _inspectedTypes.Clear();

            for (int i = 0; i < pending.Count; i++)
            {
                var type = pending[i];

                if (!_inspectedTypes.Add(type))
                    continue;

                if (_animalSpawner.HasFreeCellFor(type))
                {
                    index = i;
                    return true;
                }
            }

            index = -1;
            return false;
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
