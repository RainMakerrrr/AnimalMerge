using System;
using System.Collections.Generic;
using System.Linq;
using Code.Animals.Facades;
using UnityEngine;

namespace Code.Battle.Services
{
    public class UnitTracker : IUnitTracker, IDisposable
    {
        private readonly List<AnimalFacade> _playerUnits;
        private readonly List<AnimalFacade> _enemyUnits;

        public UnitTracker()
        {
            _playerUnits = new List<AnimalFacade>();
            _enemyUnits = new List<AnimalFacade>();
        }

        public void RegisterPlayerUnit(AnimalFacade unit)
        {
            if (unit == null)
                return;

            if (!_playerUnits.Contains(unit))
            {
                _playerUnits.Add(unit);
                unit.Health.Died += OnUnitDied;
                unit.OnRemoved += OnUnitRemoved;
            }
        }

        public void RegisterEnemyUnit(AnimalFacade unit)
        {
            if (unit == null)
                return;

            if (!_enemyUnits.Contains(unit))
            {
                _enemyUnits.Add(unit);
                unit.Health.Died += OnUnitDied;
                unit.OnRemoved += OnUnitRemoved;
            }
        }

        private void OnUnitDied()
        {
            // Event notification - counts are updated via properties
        }

        private void OnUnitRemoved(AnimalFacade unit)
        {
            // Remove from player or enemy list when unit is merged or destroyed
            bool removedFromPlayers = _playerUnits.Remove(unit);
            bool removedFromEnemies = _enemyUnits.Remove(unit);

            if (removedFromPlayers || removedFromEnemies)
            {
                // Unsubscribe from events to prevent memory leaks
                unit.Health.Died -= OnUnitDied;
                unit.OnRemoved -= OnUnitRemoved;
            }
        }

        public int AlivePlayerUnitsCount =>
            _playerUnits.Count(u => u != null && !u.Health.IsDead);

        public int AliveEnemyUnitsCount =>
            _enemyUnits.Count(u => u != null && !u.Health.IsDead);

        public bool HasAliveBoss =>
            _enemyUnits.Any(u => u != null && !u.Health.IsDead && u.IsBoss);

        public IReadOnlyList<AnimalFacade> GetAlivePlayerUnits() =>
            _playerUnits.Where(u => u != null && !u.Health.IsDead).ToList();

        public IReadOnlyList<AnimalFacade> GetAliveEnemyUnits() =>
            _enemyUnits.Where(u => u != null && !u.Health.IsDead).ToList();

        public void Reset()
        {
            Debug.LogWarning($"[UnitTracker] RESET called! Clearing {_playerUnits.Count} player units and {_enemyUnits.Count} enemy units");
            Debug.LogWarning($"[UnitTracker] Stack trace: {UnityEngine.StackTraceUtility.ExtractStackTrace()}");

            // Unsubscribe from all events
            foreach (var unit in _playerUnits.Where(u => u != null))
            {
                unit.Health.Died -= OnUnitDied;
                unit.OnRemoved -= OnUnitRemoved;
            }

            foreach (var unit in _enemyUnits.Where(u => u != null))
            {
                unit.Health.Died -= OnUnitDied;
                unit.OnRemoved -= OnUnitRemoved;
            }

            _playerUnits.Clear();
            _enemyUnits.Clear();

            Debug.LogWarning("[UnitTracker] Reset complete - all units cleared");
        }

        public void Dispose()
        {
            Reset();
        }
    }
}
