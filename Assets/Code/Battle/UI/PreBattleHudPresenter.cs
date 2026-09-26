using System;
using Code.Battle.Input;
using Code.Battle.PreBattle;
using Code.Battle.Services;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.UI
{
    public class PreBattleHudPresenter : IInitializable, IDisposable
    {
        private readonly AddAnimalButtonView _addAnimalButton;
        private readonly BattleButtonView _battleButton;
        private readonly IAllySpawnService _allySpawnService;
        private readonly StartBattleService _startBattleService;
        private readonly IBattleReadinessService _battleReadiness;
        private readonly IUnitTracker _unitTracker;
        private readonly SignalBus _signalBus;
        private readonly IPlayerInputLock _inputLock;

        private bool _isPhaseActive;

        public PreBattleHudPresenter(
            AddAnimalButtonView addAnimalButton,
            BattleButtonView battleButton,
            IAllySpawnService allySpawnService,
            StartBattleService startBattleService,
            IBattleReadinessService battleReadiness,
            IUnitTracker unitTracker,
            SignalBus signalBus,
            IPlayerInputLock inputLock)
        {
            _addAnimalButton = addAnimalButton;
            _battleButton = battleButton;
            _allySpawnService = allySpawnService;
            _startBattleService = startBattleService;
            _battleReadiness = battleReadiness;
            _unitTracker = unitTracker;
            _signalBus = signalBus;
            _inputLock = inputLock;
        }

        public void Initialize()
        {
            _addAnimalButton.Clicked += OnAddAnimalClicked;
            _battleButton.Clicked += OnBattleClicked;

            _unitTracker.PlayerUnitsChanged += OnPlayerUnitsChanged;
            _inputLock.LockChanged += OnInputLockChanged;

            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);
            _signalBus.Subscribe<PreBattlePhaseEndedSignal>(OnPhaseEnded);
            _signalBus.Subscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Subscribe<BattleReadinessChangedSignal>(OnReadinessChanged);

            HideButtons();
        }

        public void Dispose()
        {
            _addAnimalButton.Clicked -= OnAddAnimalClicked;
            _battleButton.Clicked -= OnBattleClicked;

            _unitTracker.PlayerUnitsChanged -= OnPlayerUnitsChanged;
            _inputLock.LockChanged -= OnInputLockChanged;

            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPhaseStarted);
            _signalBus.Unsubscribe<PreBattlePhaseEndedSignal>(OnPhaseEnded);
            _signalBus.Unsubscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Unsubscribe<BattleReadinessChangedSignal>(OnReadinessChanged);
        }

        private void OnAddAnimalClicked()
        {
            if (_inputLock.IsLocked)
                return;

            _allySpawnService.RequestSpawn();
        }

        private void OnBattleClicked() => _startBattleService.RequestStart();

        private void OnPhaseStarted(PreBattlePhaseStartedSignal signal)
        {
            _isPhaseActive = true;

            _addAnimalButton.SetVisible(true);
            _battleButton.SetVisible(true);
            Refresh();
        }

        private void OnPhaseEnded()
        {
            _isPhaseActive = false;
            HideButtons();
        }

        private void OnAllySpawned(AllySpawnedSignal signal) => Refresh();

        private void OnReadinessChanged(BattleReadinessChangedSignal signal) => Refresh();

        private void OnPlayerUnitsChanged()
        {
            if (!_isPhaseActive)
                return;

            Refresh();
        }

        private void OnInputLockChanged()
        {
            if (!_isPhaseActive)
                return;

            Refresh();
        }

        private void Refresh()
        {
            var isUnlocked = !_inputLock.IsLocked;

            _addAnimalButton.SetInteractable(isUnlocked && _allySpawnService.CanSpawn);
            _addAnimalButton.SetRemaining(_allySpawnService.PoolRemaining, _allySpawnService.PoolTotal);
            _battleButton.SetReady(isUnlocked && _battleReadiness.CanStartBattle);
        }

        private void HideButtons()
        {
            _addAnimalButton.SetVisible(false);
            _battleButton.SetVisible(false);
        }
    }
}
