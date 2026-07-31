using System.Collections.Generic;
using Code.Animals;
using Code.Animals.Facades;
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
        private readonly SignalBus _signalBus;

        public AllySpawnService(
            IAllySpawnPool pool,
            IAnimalSpawner animalSpawner,
            IUnitTracker unitTracker,
            SignalBus signalBus)
        {
            _pool = pool;
            _animalSpawner = animalSpawner;
            _unitTracker = unitTracker;
            _signalBus = signalBus;
        }

        public int PoolRemaining => _pool.Remaining;

        public bool CanSpawn => _pool.TryPeekNext(out var next) && _animalSpawner.HasFreeCellFor(next);

        public bool RequestSpawn()
        {
            if (!_pool.TryPeekNext(out var next))
            {
                Debug.Log("[AllySpawnService] Starting pool is empty - nothing to spawn");
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

        public bool SpawnReinforcement() => SpawnAndRegister(_animalSpawner.SpawnRandom());

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
