using System;
using System.Collections.Generic;
using System.Threading;
using Code.Animals.Facades;
using Code.Animals.Merge.Services;
using Code.Animals.Selection;
using Code.Animals.UI.MergeStats;
using Code.Battle.Signals;
using Code.Data.Animals;
using Cysharp.Threading.Tasks;
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
        private readonly SignalBus _signalBus;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();

        private AnimalFacade _tracked;
        private AnimalFacade _animationTarget;
        private CancellationTokenSource _animationCts;
        private IReadOnlyList<MergeStatChange> _pendingChanges;
        private bool _isAnimationPlaying;

        public AnimalStatsPanelPresenter(IAnimalSelectionService selectionService, IAnimalStatsPanelView view,
            AnimalDatabase database, IAbilityLinesProvider abilityLinesProvider, IMergeUndoService mergeUndoService,
            SignalBus signalBus)
        {
            _selectionService = selectionService;
            _view = view;
            _database = database;
            _abilityLinesProvider = abilityLinesProvider;
            _mergeUndoService = mergeUndoService;
            _signalBus = signalBus;
        }

        private bool HasAnimation => ReferenceEquals(_animationTarget, null) == false;

        private bool IsAnimatingTracked => HasAnimation && ReferenceEquals(_tracked, _animationTarget);

        public void Initialize()
        {
            _selectionService.SelectionChanged += OnSelectionChanged;
            _mergeUndoService.OnStackCountChanged += OnUndoStackCountChanged;
            _signalBus.Subscribe<AllyMergedSignal>(OnAllyMerged);
            _signalBus.Subscribe<AllyMergeUndoneSignal>(OnAllyMergeUndone);
        }

        public void Dispose()
        {
            _selectionService.SelectionChanged -= OnSelectionChanged;
            _mergeUndoService.OnStackCountChanged -= OnUndoStackCountChanged;
            _signalBus.TryUnsubscribe<AllyMergedSignal>(OnAllyMerged);
            _signalBus.TryUnsubscribe<AllyMergeUndoneSignal>(OnAllyMergeUndone);

            DisposeAnimationCts();
            ClearAnimationState();

            _lifetime.Cancel();
            _lifetime.Dispose();

            StopTracking();
        }

        private void OnSelectionChanged(AnimalFacade animal)
        {
            if (ShouldCancelMergeAnimation(animal))
                CancelMergeAnimation();

            StopTracking();

            if (animal == null)
            {
                _view.Hide();
                return;
            }

            _tracked = animal;

            if (_tracked.Health != null)
                _tracked.Health.HealthChanged += OnHealthChanged;

            var stats = ReferenceEquals(animal, _animationTarget)
                ? BuildInitialStats(animal, _pendingChanges)
                : BuildStats(animal);

            _view.Show(
                animal.transform,
                _database.GetIcon(animal.Type),
                stats,
                animal.Type.ToString(),
                _abilityLinesProvider.Build(animal));
        }

        private bool ShouldCancelMergeAnimation(AnimalFacade animal)
        {
            if (HasAnimation == false || ReferenceEquals(animal, _animationTarget))
                return false;

            return _isAnimationPlaying || animal != null;
        }

        private void OnAllyMerged(AllyMergedSignal signal)
        {
            if (signal.Target == null)
                return;

            var changes = MergeStatChangeBuilder.Build(signal.StatsBefore, signal.StatsAfter);

            if (changes.Count == 0)
                return;

            CancelMergeAnimation();

            _animationTarget = signal.Target;
            _pendingChanges = changes;
            _animationCts = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);

            if (ReferenceEquals(_tracked, _animationTarget))
                _view.UpdateStats(BuildInitialStats(_animationTarget, changes));

            RunMergeAnimationAsync(_animationTarget, changes, _animationCts).Forget();
        }

        private async UniTaskVoid RunMergeAnimationAsync(AnimalFacade target, IReadOnlyList<MergeStatChange> changes,
            CancellationTokenSource animationCts)
        {
            var token = animationCts.Token;

            try
            {
                var canceled = await UniTask.NextFrame(token).SuppressCancellationThrow();

                if (canceled || target == null || target.gameObject.activeInHierarchy == false)
                    return;

                _selectionService.Select(target);

                if (ReferenceEquals(_selectionService.Selected, target) == false)
                    return;

                _isAnimationPlaying = true;
                _view.UpdateStats(BuildInitialStats(target, changes));

                await _view.PlayStatChangesAsync(changes, token);
            }
            finally
            {
                if (ReferenceEquals(_animationCts, animationCts))
                {
                    DisposeAnimationCts();
                    ClearAnimationState();

                    if (_tracked != null)
                        _view.UpdateStats(BuildStats(_tracked));
                }
            }
        }

        private void OnAllyMergeUndone()
        {
            CancelMergeAnimation();

            if (_tracked != null)
                _view.UpdateStats(BuildStats(_tracked));
        }

        private void OnUndoStackCountChanged(int stackCount)
        {
            if (_tracked == null)
                return;

            _view.UpdateAbilityLines(_abilityLinesProvider.Build(_tracked));

            if (IsAnimatingTracked == false)
                _view.UpdateStats(BuildStats(_tracked));
        }

        private void OnHealthChanged()
        {
            if (_tracked == null || IsAnimatingTracked)
                return;

            _view.UpdateStats(BuildStats(_tracked));
        }

        private void CancelMergeAnimation()
        {
            if (HasAnimation == false && _animationCts == null)
                return;

            DisposeAnimationCts();
            ClearAnimationState();

            _view.StopStatChanges();
        }

        private void DisposeAnimationCts()
        {
            if (_animationCts == null)
                return;

            var animationCts = _animationCts;
            _animationCts = null;

            animationCts.Cancel();
            animationCts.Dispose();
        }

        private void ClearAnimationState()
        {
            _animationTarget = null;
            _pendingChanges = null;
            _isAnimationPlaying = false;
        }

        private void StopTracking()
        {
            if (ReferenceEquals(_tracked, null) == false && _tracked.Health != null)
                _tracked.Health.HealthChanged -= OnHealthChanged;

            _tracked = null;
        }

        private AnimalCardStats BuildInitialStats(AnimalFacade animal, IReadOnlyList<MergeStatChange> changes)
        {
            var stats = BuildStats(animal);

            if (changes == null)
                return stats;

            var attack = stats.Attack;
            var health = stats.Health;
            var tilesPerMove = stats.TilesPerMove;

            foreach (var change in changes)
            {
                switch (change.Kind)
                {
                    case MergeStatKind.Damage:
                        attack = change.From;
                        break;
                    case MergeStatKind.Health:
                        health = change.From;
                        break;
                    case MergeStatKind.Moves:
                        tilesPerMove = change.From;
                        break;
                }
            }

            return new AnimalCardStats(attack, health, tilesPerMove);
        }

        private AnimalCardStats BuildStats(AnimalFacade animal) =>
            new AnimalCardStats(
                ReadDamage(animal),
                ReadHealth(animal),
                ReadTilesPerMove(animal));

        private int ReadDamage(AnimalFacade animal) =>
            animal.AttackInstance != null ? ToStatValue(animal.GetDamage()) : 0;

        private int ReadHealth(AnimalFacade animal) =>
            animal.Health != null ? ToStatValue(animal.GetCurrentHealth()) : 0;

        private int ReadTilesPerMove(AnimalFacade animal) =>
            animal.Movement != null ? animal.GetTilesPerMove() : 0;

        private int ToStatValue(float value) => Mathf.Max(0, Mathf.RoundToInt(value));
    }
}
