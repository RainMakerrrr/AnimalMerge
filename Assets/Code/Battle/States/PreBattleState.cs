using System.Threading;
using System.Threading.Tasks;
using Code.Animals;
using Code.Battle.Input;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    /// <summary>
    /// Pre-battle preparation phase
    /// Spawns player units and enemy units
    /// Player can merge and arrange units on merge grid
    /// Transitions to BattleStartState when player confirms ready
    /// </summary>
    public class PreBattleState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly BattleFlowController _flowController;
        private readonly AnimalSpawner _animalSpawner;
        private readonly IEnemySpawnService _enemySpawnService;
        private readonly IUnitTracker _unitTracker;
        private readonly StartBattleService _startBattleService;

        private CancellationTokenSource _cancellationTokenSource;

        public PreBattleState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            AnimalSpawner animalSpawner,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker,
            StartBattleService startBattleService)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _animalSpawner = animalSpawner;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
            _startBattleService = startBattleService;
        }

        public async Task Enter()
        {
            Debug.Log("[PreBattleState] Entering - spawning units and waiting for start confirmation");

            _cancellationTokenSource = new CancellationTokenSource();

            // Spawn player units only if they don't exist yet (first stage)
            var existingPlayerUnits = _unitTracker.GetAlivePlayerUnits();
            if (existingPlayerUnits.Count == 0)
            {
                _animalSpawner.SpawnAnimals();

                // Register spawned player units with tracker
                foreach (var playerUnit in _animalSpawner.Animals)
                {
                    _unitTracker.RegisterPlayerUnit(playerUnit);
                }

                Debug.Log($"[PreBattleState] Spawned and registered {_animalSpawner.Animals.Count} player units");
            }
            else
            {
                Debug.Log($"[PreBattleState] {existingPlayerUnits.Count} player units already exist - skipping spawn");
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

            Debug.Log($"[PreBattleState] Setup complete - {spawnedEnemies.Count} enemies registered. Press Enter or click 'Start Battle' to begin");

            // Subscribe to battle start event
            _startBattleService.StartBattleRequested += OnStartBattleRequested;
        }

        public Task Exit()
        {
            Debug.Log("[PreBattleState] Exiting");

            // Unsubscribe from event
            _startBattleService.StartBattleRequested -= OnStartBattleRequested;

            // Cancel and cleanup
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            return Task.CompletedTask;
        }

        private void OnStartBattleRequested()
        {
            StartBattleAsync();
        }

        private async Task StartBattleAsync()
        {
            Debug.Log("[PreBattleState] Battle start confirmed - transitioning to BattleStartState");
            await _stateMachine.ChangeStateAsync<PlayerTurnState>();
        } 
    }
}
