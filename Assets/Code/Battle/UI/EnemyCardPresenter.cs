using System;
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Data.Animals;
using UnityEngine;
using Zenject;

namespace Code.Battle.UI
{
    public class EnemyCardPresenter : IInitializable, IDisposable
    {
        private const IReadOnlyList<string> KeepPrefabAbilityLines = null;

        private readonly IEnemyCardView _view;
        private readonly IUnitTracker _unitTracker;
        private readonly AnimalDatabase _database;
        private readonly SignalBus _signalBus;

        private AnimalFacade _tracked;

        public EnemyCardPresenter(
            IEnemyCardView view,
            IUnitTracker unitTracker,
            AnimalDatabase database,
            SignalBus signalBus)
        {
            _view = view;
            _unitTracker = unitTracker;
            _database = database;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Subscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Subscribe<BattleEndedSignal>(OnBattleEnded);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Unsubscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Unsubscribe<BattleEndedSignal>(OnBattleEnded);

            StopTracking();
        }

        private void OnPreBattlePhaseStarted(PreBattlePhaseStartedSignal signal) => ShowFeaturedEnemy();

        private void OnBattleEnded() => HideCard();

        private void OnUnitTurnStarted(UnitTurnStartedSignal signal)
        {
            if (IsAliveEnemyUnit(signal.Unit) == false)
                return;

            if (ReferenceEquals(_tracked, signal.Unit))
                return;

            ShowCard(signal.Unit);
        }

        private void OnHealthChanged()
        {
            if (_tracked == null)
                return;

            if (IsAlive(_tracked) == false)
            {
                ShowFeaturedEnemy();
                return;
            }

            _view.UpdateStats(ReadDamage(_tracked), ReadHealth(_tracked));
        }

        private void ShowFeaturedEnemy()
        {
            var enemies = _unitTracker.GetAliveEnemyUnits();

            if (enemies == null || enemies.Count == 0)
            {
                HideCard();
                return;
            }

            ShowCard(ResolveFeaturedEnemy(enemies));
        }

        private void ShowCard(AnimalFacade enemy)
        {
            if (enemy == null)
            {
                HideCard();
                return;
            }

            StopTracking();

            _tracked = enemy;

            if (_tracked.Health != null)
                _tracked.Health.HealthChanged += OnHealthChanged;

            _view.Show(
                enemy.transform,
                _database.GetIcon(enemy.Type),
                ReadDamage(enemy),
                ReadHealth(enemy),
                enemy.Type.ToString(),
                KeepPrefabAbilityLines);
        }

        private void HideCard()
        {
            StopTracking();

            _view.Hide();
        }

        private void StopTracking()
        {
            if (ReferenceEquals(_tracked, null) == false && _tracked.Health != null)
                _tracked.Health.HealthChanged -= OnHealthChanged;

            _tracked = null;
        }

        private AnimalFacade ResolveFeaturedEnemy(IReadOnlyList<AnimalFacade> enemies)
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.IsBoss)
                    return enemy;
            }

            return enemies[0];
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

        private bool IsAlive(AnimalFacade unit) =>
            unit != null && unit.Health != null && unit.Health.IsDead == false;

        private int ReadDamage(AnimalFacade enemy) =>
            enemy.AttackInstance != null ? ToStatValue(enemy.GetDamage()) : 0;

        private int ReadHealth(AnimalFacade enemy) =>
            enemy.Health != null ? ToStatValue(enemy.GetCurrentHealth()) : 0;

        private int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
