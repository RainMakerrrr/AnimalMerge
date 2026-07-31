using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Code.Animals.Merge.Services;
using Code.Battle.Input;
using Code.Battle.PreBattle;
using Code.Battle.Services;
using Code.Battle.States;
using UnityEngine;
using Zenject;

namespace Code.Battle.StateMachine
{
    public class BattleStateMachine
    {
        private Dictionary<Type, IBattleState> _states;
        private IBattleState _activeState;

        public BattleStateMachine()
        {
            _states = new Dictionary<Type, IBattleState>();
        }

        [Inject]
        public void Initialize(
            BattleFlowController flowController,
            IUnitTracker unitTracker,
            IEnemySpawnService enemySpawnService,
            ITurnExecutor turnExecutor,
            IVictoryConditionChecker victoryChecker,
            IHealthRestorationService healthRestoration,
            IUnitRepositioningService unitRepositioning,
            Framework.Code.Infrastructure.States.GameStateMachine gameStateMachine,
            StartBattleService startBattleService,
            IMergeUndoService mergeUndoService,
            IAllySpawnPool allySpawnPool,
            IAllySpawnService allySpawnService,
            IBattleReadinessService battleReadiness,
            SignalBus signalBus)
        {
            // Resolve circular dependency: FlowController needs StateMachine, StateMachine needs FlowController
            flowController.SetStateMachine(this);

            // Create all battle states with their dependencies
            _states = new Dictionary<Type, IBattleState>
            {
                { typeof(PreBattleState), new PreBattleState(this, flowController, enemySpawnService, unitTracker, startBattleService, mergeUndoService, allySpawnPool, allySpawnService, battleReadiness, signalBus) },
                { typeof(BattleStartState), new BattleStartState(this) },
                { typeof(PlayerTurnState), new PlayerTurnState(this, turnExecutor) },
                { typeof(EnemyTurnState), new EnemyTurnState(this, turnExecutor) },
                { typeof(CheckVictoryState), new CheckVictoryState(this, victoryChecker, flowController) },
                { typeof(StageClearState), new StageClearState(this, flowController, healthRestoration, enemySpawnService, unitTracker, unitRepositioning) },
                { typeof(BattleEndState), new BattleEndState(gameStateMachine, flowController, victoryChecker) }
            };

            Debug.Log("[BattleStateMachine] Initialized with 7 battle states");
        }

        public async UniTask ChangeStateAsync<TState>() where TState : class, IBattleState
        {
            var stateType = typeof(TState);

            if (!_states.ContainsKey(stateType))
            {
                Debug.LogError($"[BattleStateMachine] State {stateType.Name} not found in state machine!");
                return;
            }

            var previousStateName = _activeState?.GetType().Name ?? "None";
            var newStateName = stateType.Name;

            Debug.Log($"[BattleStateMachine] State transition: {previousStateName} → {newStateName}");

            if (_activeState != null)
                await _activeState.Exit();

            _activeState = _states[stateType];
            await _activeState.Enter();
        }

        public async UniTask CleanupAsync()
        {
            var currentStateName = _activeState?.GetType().Name ?? "None";
            Debug.Log($"[BattleStateMachine] Cleanup - exiting state: {currentStateName}");

            if (_activeState != null)
                await _activeState.Exit();

            _activeState = null;
        }
    }
}
