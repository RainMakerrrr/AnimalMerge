using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Merge.Services;
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
        private readonly IMergeUndoService _mergeUndoService;

        private CancellationTokenSource _cancellationTokenSource;

        public PreBattleState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            AnimalSpawner animalSpawner,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker,
            StartBattleService startBattleService,
            IMergeUndoService mergeUndoService)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _animalSpawner = animalSpawner;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
            _startBattleService = startBattleService;
            _mergeUndoService = mergeUndoService;
        }

        public async Task Enter()
        {
            Debug.Log("[PreBattleState] Entering - spawning units and waiting for start confirmation");

            _cancellationTokenSource = new CancellationTokenSource();

            // Enable merge undo tracking during pre-battle phase
            _mergeUndoService.Enable();
            Debug.Log("[PreBattleState] Merge undo tracking enabled");

            // Handle unit spawning based on whether this is first stage of level
            if (_flowController.IsFirstStageOfLevel)
            {
                // This is the first stage of a level
                // IMPORTANT: Use UnitTracker instead of AnimalSpawner.Animals
                // because AnimalSpawner.Animals filters by activeInHierarchy which is unreliable
                bool hasExistingUnits = _unitTracker.AlivePlayerUnitsCount > 0;
                Debug.Log($"[PreBattleState] First stage of level - AnimalSpawner.Animals.Count = {_animalSpawner.Animals.Count}, UnitTracker alive = {_unitTracker.AlivePlayerUnitsCount}");

                if (!hasExistingUnits)
                {
                    // Very first level - spawn initial units
                    _animalSpawner.SpawnAnimals();

                    // Register spawned player units with tracker
                    foreach (var playerUnit in _animalSpawner.Animals)
                    {
                        _unitTracker.RegisterPlayerUnit(playerUnit);
                    }

                    Debug.Log($"[PreBattleState] First Level - Spawned and registered {_animalSpawner.Animals.Count} initial player units");
                }
                else
                {
                    // Subsequent level - spawn 1 random reinforcement
                    Debug.Log($"[PreBattleState] New Level - {_animalSpawner.Animals.Count} existing units in AnimalSpawner, spawning 1 random reinforcement");
                    Debug.Log($"[PreBattleState] Existing units in spawner: {string.Join(", ", _animalSpawner.Animals.Select(u => $"{u.name}(Active:{u.gameObject.activeInHierarchy})"))}");

                    var beforeCount = _animalSpawner.Animals.Count;
                    _animalSpawner.SpawnRandomAnimal();

                    // Register newly spawned unit with tracker
                    var newUnits = _animalSpawner.Animals.Skip(beforeCount).ToList();
                    foreach (var newUnit in newUnits)
                    {
                        _unitTracker.RegisterPlayerUnit(newUnit);
                    }

                    Debug.Log($"[PreBattleState] Spawned {newUnits.Count} reinforcement unit(s)");
                    Debug.Log($"[PreBattleState] Total units in tracker after registration: {_unitTracker.AlivePlayerUnitsCount}");
                }

                // Mark that we've processed the first stage of this level
                _flowController.MarkFirstStageProcessed();
            }
            else
            {
                // Subsequent stages within the same level - no new spawns
                Debug.Log($"[PreBattleState] Stage {_flowController.CurrentStageIndex + 1} of current level - no new spawns, using existing units");
                Debug.Log($"[PreBattleState] Current units in tracker: {_unitTracker.AlivePlayerUnitsCount}");
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

            // Disable merge undo tracking and clear stack when leaving pre-battle phase
            _mergeUndoService.Disable();
            Debug.Log("[PreBattleState] Merge undo tracking disabled and stack cleared");

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
