using System;
using Code.Animals.Facades;
using Code.Animals.Merge.Services;
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
        private readonly IAbilityLinesProvider _abilityLinesProvider;
        private readonly IMergeUndoService _mergeUndoService;

        private AnimalFacade _tracked;

        public AnimalStatsPanelPresenter(IAnimalSelectionService selectionService, IAnimalStatsPanelView view,
            AnimalDatabase database, IAbilityLinesProvider abilityLinesProvider, IMergeUndoService mergeUndoService)
        {
            _selectionService = selectionService;
            _view = view;
            _database = database;
            _abilityLinesProvider = abilityLinesProvider;
            _mergeUndoService = mergeUndoService;
        }

        public void Initialize()
        {
            _selectionService.SelectionChanged += OnSelectionChanged;
            _mergeUndoService.OnStackCountChanged += OnUndoStackCountChanged;
        }

        public void Dispose()
        {
            _selectionService.SelectionChanged -= OnSelectionChanged;
            _mergeUndoService.OnStackCountChanged -= OnUndoStackCountChanged;

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

            _view.Show(
                animal.transform,
                _database.GetIcon(animal.Type),
                ReadDamage(animal),
                ReadHealth(animal),
                animal.Type.ToString(),
                _abilityLinesProvider.Build(animal));
        }

        private void OnUndoStackCountChanged(int stackCount)
        {
            if (_tracked == null)
                return;

            _view.UpdateAbilityLines(_abilityLinesProvider.Build(_tracked));
            _view.UpdateStats(ReadDamage(_tracked), ReadHealth(_tracked));
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
