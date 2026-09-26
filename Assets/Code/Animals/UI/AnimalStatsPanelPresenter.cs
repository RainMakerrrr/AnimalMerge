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
                BuildStats(animal),
                animal.Type.ToString(),
                _abilityLinesProvider.Build(animal));
        }

        private void OnUndoStackCountChanged(int stackCount)
        {
            if (_tracked == null)
                return;

            _view.UpdateAbilityLines(_abilityLinesProvider.Build(_tracked));
            _view.UpdateStats(BuildStats(_tracked));
        }

        private void OnHealthChanged()
        {
            if (_tracked == null)
                return;

            _view.UpdateStats(BuildStats(_tracked));
        }

        private void StopTracking()
        {
            if (ReferenceEquals(_tracked, null) == false && _tracked.Health != null)
                _tracked.Health.HealthChanged -= OnHealthChanged;

            _tracked = null;
        }

        private AnimalCardStats BuildStats(AnimalFacade animal) =>
            new AnimalCardStats(
                ReadDamage(animal),
                ReadHealth(animal),
                ReadTilesPerMove(animal),
                ReadHealthBonusPercent(animal));

        private int ReadDamage(AnimalFacade animal) =>
            animal.AttackInstance != null ? ToStatValue(animal.GetDamage()) : 0;

        private int ReadHealth(AnimalFacade animal) =>
            animal.Health != null ? ToStatValue(animal.GetCurrentHealth()) : 0;

        private int ReadTilesPerMove(AnimalFacade animal) =>
            animal.Movement != null ? animal.GetTilesPerMove() : 0;

        private int ReadHealthBonusPercent(AnimalFacade animal)
        {
            if (animal.Health == null)
                return 0;

            AnimalStats stats = _database.GetStats(animal.Type);

            if (stats == null || stats.Health <= 0)
                return 0;

            return Mathf.RoundToInt((animal.GetMaxHealth() / stats.Health - 1f) * 100f);
        }

        private int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
