using System;
using System.Collections.Generic;
using Code.Animals.Facades;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Data.Animals;
using Framework.Code.Infrastructure.Signals;
using Framework.Code.Infrastructure.States;
using Zenject;

namespace Code.Battle.UI
{
    public class TurnOrderPresenter : IInitializable, IDisposable
    {
        private readonly ITurnOrderView _view;
        private readonly ITurnOrderProvider _turnOrderProvider;
        private readonly IUnitTracker _unitTracker;
        private readonly AnimalDatabase _database;
        private readonly SignalBus _signalBus;
        private readonly List<TurnOrderEntryData> _entries = new List<TurnOrderEntryData>();

        private AnimalFacade _activeUnit;
        private bool _hasActiveTurn;

        public TurnOrderPresenter(
            ITurnOrderView view,
            ITurnOrderProvider turnOrderProvider,
            IUnitTracker unitTracker,
            AnimalDatabase database,
            SignalBus signalBus)
        {
            _view = view;
            _turnOrderProvider = turnOrderProvider;
            _unitTracker = unitTracker;
            _database = database;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _unitTracker.PlayerUnitsChanged += Refresh;

            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Subscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Subscribe<AllyMergedSignal>(OnAllyMerged);
            _signalBus.Subscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Subscribe<UnitTurnCompletedSignal>(OnUnitTurnCompleted);
            _signalBus.Subscribe<TurnRoundStartedSignal>(OnTurnRoundStarted);
            _signalBus.Subscribe<StateChangedSignal>(OnGameStateChanged);

            _view.Hide();
        }

        public void Dispose()
        {
            _unitTracker.PlayerUnitsChanged -= Refresh;

            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Unsubscribe<AllySpawnedSignal>(OnAllySpawned);
            _signalBus.Unsubscribe<AllyMergedSignal>(OnAllyMerged);
            _signalBus.Unsubscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Unsubscribe<UnitTurnCompletedSignal>(OnUnitTurnCompleted);
            _signalBus.Unsubscribe<TurnRoundStartedSignal>(OnTurnRoundStarted);
            _signalBus.Unsubscribe<StateChangedSignal>(OnGameStateChanged);
        }

        private void OnPreBattlePhaseStarted(PreBattlePhaseStartedSignal signal)
        {
            _activeUnit = null;
            _hasActiveTurn = false;

            _view.Show();
            Refresh();
        }

        private void OnAllySpawned(AllySpawnedSignal signal) => Refresh();

        private void OnAllyMerged(AllyMergedSignal signal) => Refresh();

        private void OnUnitTurnStarted(UnitTurnStartedSignal signal)
        {
            _activeUnit = signal.Unit;
            _hasActiveTurn = true;

            Refresh();
        }

        private void OnUnitTurnCompleted(UnitTurnCompletedSignal signal)
        {
            _hasActiveTurn = false;

            Refresh();
        }

        private void OnTurnRoundStarted(TurnRoundStartedSignal signal)
        {
            _activeUnit = null;
            _hasActiveTurn = false;

            Refresh();
        }

        private void OnGameStateChanged(StateChangedSignal signal)
        {
            if (IsBattleOverState(signal.State) == false)
                return;

            _activeUnit = null;
            _hasActiveTurn = false;
            _view.Hide();
        }

        private bool IsBattleOverState(IBaseState state) =>
            state is WinState || state is LoseState || state is CampaignVictoryState;

        private void Refresh()
        {
            var order = _turnOrderProvider.GetRoundOrder();

            _entries.Clear();

            var activeIndex = ResolveActiveIndex(order);

            for (int i = 0; i < order.Count; i++)
            {
                var turnOrderUnit = order[(activeIndex + i) % order.Count];
                var unit = turnOrderUnit.Unit;

                _entries.Add(new TurnOrderEntryData(
                    unit.GetInstanceID(),
                    _database.GetPortrait(unit.Type),
                    _hasActiveTurn && i == 0,
                    turnOrderUnit.IsEnemy));
            }

            _view.Render(_entries);
        }

        private int ResolveActiveIndex(IReadOnlyList<TurnOrderUnit> order)
        {
            if (ReferenceEquals(_activeUnit, null))
                return 0;

            for (int i = 0; i < order.Count; i++)
            {
                if (ReferenceEquals(order[i].Unit, _activeUnit))
                    return i;
            }

            return 0;
        }
    }
}
