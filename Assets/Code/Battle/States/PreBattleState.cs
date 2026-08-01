using System.Threading;
using Cysharp.Threading.Tasks;
using Code.Animals.Merge.Services;
using Code.Battle.Input;
using Code.Battle.PreBattle;
using Code.Battle.Services;
using Code.Battle.Signals;
using Code.Battle.StateMachine;
using UnityEngine;
using Zenject;

namespace Code.Battle.States
{
    /// <summary>
    /// Pre-battle preparation phase
    /// </summary>
    public class PreBattleState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly BattleFlowController _flowController;
        private readonly IEnemySpawnService _enemySpawnService;
        private readonly IUnitTracker _unitTracker;
        private readonly StartBattleService _startBattleService;
        private readonly IMergeUndoService _mergeUndoService;
        private readonly IAllySpawnPool _allySpawnPool;
        private readonly IAllySpawnService _allySpawnService;
        private readonly IBattleReadinessService _battleReadiness;
        private readonly SignalBus _signalBus;

        private CancellationTokenSource _cancellationTokenSource;
        private bool _isStarting;

        public PreBattleState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker,
            StartBattleService startBattleService,
            IMergeUndoService mergeUndoService,
            IAllySpawnPool allySpawnPool,
            IAllySpawnService allySpawnService,
            IBattleReadinessService battleReadiness,
            SignalBus signalBus)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
            _startBattleService = startBattleService;
            _mergeUndoService = mergeUndoService;
            _allySpawnPool = allySpawnPool;
            _allySpawnService = allySpawnService;
            _battleReadiness = battleReadiness;
            _signalBus = signalBus;
        }

        public async UniTask Enter()
        {
            Debug.Log("[PreBattleState] Entering - preparing units and waiting for start confirmation");

            _cancellationTokenSource = new CancellationTokenSource();

            // Enable merge undo tracking during pre-battle phase
            _mergeUndoService.Enable();
            Debug.Log("[PreBattleState] Merge undo tracking enabled");

            bool isLevelStart = _flowController.IsFirstStageOfLevel;
            bool isStartingPool = false;

            if (isLevelStart)
            {
                bool hasExistingUnits = _unitTracker.AlivePlayerUnitsCount > 0;

                if (!hasExistingUnits)
                {
                    _allySpawnService.QueueStartingPool();
                    isStartingPool = true;
                    Debug.Log($"[PreBattleState] First level - starting pool holds {_allySpawnPool.Remaining} animals");
                }
                else
                {
                    _allySpawnPool.Clear();
                    int queued = _allySpawnService.QueueReinforcements();
                    Debug.Log($"[PreBattleState] New level - {queued} reinforcement(s) queued for the Add Animal button, allies in tracker: {_unitTracker.AlivePlayerUnitsCount}");
                }

                // Mark that we've processed the first stage of this level
                _flowController.MarkFirstStageProcessed();
            }
            else
            {
                // Subsequent stages within the same level - no new spawns
                _allySpawnPool.Clear();
                Debug.Log($"[PreBattleState] Stage {_flowController.CurrentStageIndex + 1} of current level - no new spawns, {_unitTracker.AlivePlayerUnitsCount} allies in tracker");
            }

            // Get current stage configuration
            var stageConfig = _flowController.GetCurrentStageConfig();
            if (stageConfig == null)
            {
                Debug.LogError("[PreBattleState] Failed to get stage config - transitioning to defeat");
                await _stateMachine.ChangeStateAsync<BattleEndState>();
                return;
            }

            Debug.Log($"[PreBattleState] Spawning enemies for stage {stageConfig.StageNumber} (Boss Stage: {stageConfig.IsBossStage})");

            // Spawn enemies for current stage
            var spawnedEnemies = await _enemySpawnService.SpawnEnemiesForStageAsync(
                stageConfig,
                _cancellationTokenSource.Token);

            if (spawnedEnemies == null || spawnedEnemies.Count == 0)
            {
                Debug.LogWarning("[PreBattleState] No enemies spawned");
            }

            // Register spawned enemies with tracker
            foreach (var enemy in spawnedEnemies)
            {
                _unitTracker.RegisterEnemyUnit(enemy);
            }

            Debug.Log($"[PreBattleState] Setup complete - {spawnedEnemies.Count} enemies registered");

            // Subscribe to battle start event
            _startBattleService.StartBattleRequested += OnStartBattleRequested;

            _signalBus.Fire(new PreBattlePhaseStartedSignal
            {
                IsLevelStart = isLevelStart,
                IsStartingPool = isStartingPool,
                PoolRemaining = _allySpawnPool.Remaining
            });

            _battleReadiness.Activate();
        }

        public UniTask Exit()
        {
            Debug.Log("[PreBattleState] Exiting");

            _battleReadiness.Deactivate();
            _signalBus.Fire(new PreBattlePhaseEndedSignal());

            // Disable merge undo tracking and clear stack when leaving pre-battle phase
            _mergeUndoService.Disable();
            Debug.Log("[PreBattleState] Merge undo tracking disabled and stack cleared");

            // Unsubscribe from event
            _startBattleService.StartBattleRequested -= OnStartBattleRequested;

            // Cancel and cleanup
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            _isStarting = false;

            return UniTask.CompletedTask;
        }

        private void OnStartBattleRequested()
        {
            if (_isStarting)
            {
                Debug.Log("[PreBattleState] Battle start ignored - a transition is already in flight");
                return;
            }

            if (!_battleReadiness.CanStartBattle)
            {
                Debug.Log("[PreBattleState] Battle start rejected - readiness conditions are not met");
                return;
            }

            _isStarting = true;
            StartBattleAsync().Forget();
        }

        private async UniTask StartBattleAsync()
        {
            Debug.Log("[PreBattleState] Battle start confirmed - transitioning to PlayerTurnState");
            await _stateMachine.ChangeStateAsync<PlayerTurnState>();
        }
    }
}
