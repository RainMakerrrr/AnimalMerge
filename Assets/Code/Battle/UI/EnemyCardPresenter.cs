using System;
using Code.Animals.Facades;
using Code.Animals.UI;
using Code.Battle.Selection;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Battle.UI
{
    public class EnemyCardPresenter : IInitializable, IDisposable
    {
        private readonly IEnemyCardView _view;
        private readonly IEnemySelectionService _selectionService;
        private readonly AnimalDatabase _database;
        private readonly IAbilityLinesProvider _abilityLinesProvider;

        private AnimalFacade _tracked;

        public EnemyCardPresenter(IEnemyCardView view, IEnemySelectionService selectionService,
            AnimalDatabase database, IAbilityLinesProvider abilityLinesProvider)
        {
            _view = view;
            _selectionService = selectionService;
            _database = database;
            _abilityLinesProvider = abilityLinesProvider;
        }

        public void Initialize() => _selectionService.SelectionChanged += OnSelectionChanged;

        public void Dispose()
        {
            _selectionService.SelectionChanged -= OnSelectionChanged;

            StopTracking();
        }

        private void OnSelectionChanged(AnimalFacade enemy)
        {
            StopTracking();

            if (enemy == null)
            {
                _view.Hide();
                return;
            }

            _tracked = enemy;

            if (_tracked.Health != null)
                _tracked.Health.HealthChanged += OnHealthChanged;

            _view.Show(
                enemy.transform,
                _database.GetIcon(enemy.Type),
                ReadDamage(enemy),
                ReadHealth(enemy),
                enemy.Type.ToString(),
                _abilityLinesProvider.Build(enemy));
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

        private int ReadDamage(AnimalFacade enemy) =>
            enemy.AttackInstance != null ? ToStatValue(enemy.GetDamage()) : 0;

        private int ReadHealth(AnimalFacade enemy) =>
            enemy.Health != null ? ToStatValue(enemy.GetCurrentHealth()) : 0;

        private int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
