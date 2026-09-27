using System;
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Battle.Signals;
using UnityEngine;
using Zenject;

namespace Code.Battle.Vfx
{
    public class UnitSpawnVfxPresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly IVfxSpawner _vfxSpawner;
        private readonly BattleVfxConfig _config;

        public UnitSpawnVfxPresenter(SignalBus signalBus, IVfxSpawner vfxSpawner, BattleVfxConfig config)
        {
            _signalBus = signalBus;
            _vfxSpawner = vfxSpawner;
            _config = config;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Subscribe<EnemiesSpawnedSignal>(OnEnemiesSpawned);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Unsubscribe<EnemiesSpawnedSignal>(OnEnemiesSpawned);
        }

        private void OnAllySpawned(AllySpawnedSignal signal)
        {
            if (signal.Units != null)
            {
                PlayForUnits(signal.Units);
                return;
            }

            PlayForUnit(signal.Unit);
        }

        private void OnEnemiesSpawned(EnemiesSpawnedSignal signal)
        {
            if (!_config.PlaySpawnPuffForEnemies || signal.Units == null)
                return;

            PlayForUnits(signal.Units);
        }

        private void PlayForUnits(IReadOnlyList<AnimalFacade> units)
        {
            foreach (var unit in units)
            {
                PlayForUnit(unit);
            }
        }

        private void PlayForUnit(AnimalFacade unit)
        {
            if (unit == null)
                return;

            var position = unit.transform.position + Vector3.up * _config.SpawnPuffHeightOffset;
            var scale = UnitVfxScale.Calculate(unit, _config.SpawnPuffScalePerCell);
            _vfxSpawner.Spawn(_config.SpawnPuffPrefab, position, Quaternion.identity, scale);
        }
    }
}
