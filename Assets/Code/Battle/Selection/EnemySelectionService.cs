using System;
using Code.Animals.Facades;
using Code.Battle.Services;
using Zenject;

namespace Code.Battle.Selection
{
    public class EnemySelectionService : IEnemySelectionService, ITickable, IDisposable
    {
        private readonly IUnitTracker _unitTracker;

        private AnimalFacade _selected;

        public EnemySelectionService(IUnitTracker unitTracker)
        {
            _unitTracker = unitTracker;
        }

        public AnimalFacade Selected => _selected;

        public event Action<AnimalFacade> SelectionChanged;

        public void Dispose() => ReleaseSelected();

        public void Tick() => DropSelectionWhenDestroyed();

        public void Toggle(AnimalFacade enemy)
        {
            DropSelectionWhenDestroyed();

            if (enemy == null) return;

            if (ReferenceEquals(_selected, enemy))
            {
                Clear();
                return;
            }

            if (IsAliveEnemyUnit(enemy) == false) return;

            ReleaseSelected();

            _selected = enemy;
            _selected.OnRemoved += OnSelectedRemoved;

            SelectionChanged?.Invoke(_selected);
        }

        public void Clear()
        {
            if (ReferenceEquals(_selected, null)) return;

            ReleaseSelected();

            SelectionChanged?.Invoke(null);
        }

        private void DropSelectionWhenDestroyed()
        {
            if (ReferenceEquals(_selected, null)) return;
            if (_selected != null) return;

            Clear();
        }

        private void ReleaseSelected()
        {
            if (ReferenceEquals(_selected, null) == false)
                _selected.OnRemoved -= OnSelectedRemoved;

            _selected = null;
        }

        private bool IsAliveEnemyUnit(AnimalFacade unit)
        {
            if (unit == null)
                return false;

            var enemies = _unitTracker.GetAliveEnemyUnits();

            if (enemies == null)
                return false;

            foreach (var enemy in enemies)
            {
                if (ReferenceEquals(enemy, unit))
                    return true;
            }

            return false;
        }

        private void OnSelectedRemoved(AnimalFacade enemy) => Clear();
    }
}
