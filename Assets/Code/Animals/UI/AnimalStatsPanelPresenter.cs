using System;
using Code.Animals.Facades;
using Code.Animals.Selection;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Animals.UI
{
    public class AnimalStatsPanelPresenter : IInitializable, IDisposable
    {
        private readonly IAnimalSelectionService _selectionService;
        private readonly IAnimalStatsPanelView _view;
        private readonly AnimalDatabase _database;

        private AnimalFacade _tracked;

        public AnimalStatsPanelPresenter(IAnimalSelectionService selectionService, IAnimalStatsPanelView view,
            AnimalDatabase database)
        {
            _selectionService = selectionService;
            _view = view;
            _database = database;
        }

        public void Initialize() => _selectionService.SelectionChanged += OnSelectionChanged;

        public void Dispose()
        {
            _selectionService.SelectionChanged -= OnSelectionChanged;

            StopTracking();
        }

        private void OnSelectionChanged(AnimalFacade animal)
        {
            StopTracking();

            if (animal == null)
            {
                _view.Hide();
                return;
            }

            _tracked = animal;

            if (_tracked.Health != null)
                _tracked.Health.HealthChanged += OnHealthChanged;

            _view.Show(animal.transform, _database.GetIcon(animal.Type), ReadDamage(animal), ReadHealth(animal));
        }

        private void OnHealthChanged()
        {
            if (_tracked == null)
                return;

            _view.UpdateStats(ReadDamage(_tracked), ReadHealth(_tracked));
        }

        private void StopTracking()
        {
            if (ReferenceEquals(_tracked, null) == false && _tracked.Health != null)
                _tracked.Health.HealthChanged -= OnHealthChanged;

            _tracked = null;
        }

        private int ReadDamage(AnimalFacade animal) =>
            animal.AttackInstance != null ? ToStatValue(animal.GetDamage()) : 0;

        private int ReadHealth(AnimalFacade animal) =>
            animal.Health != null ? ToStatValue(animal.GetCurrentHealth()) : 0;

        private int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
