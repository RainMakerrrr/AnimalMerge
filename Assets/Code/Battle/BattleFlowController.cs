using System.Threading.Tasks;
using Code.Battle.Config;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using Code.Battle.States;
using Code.Infrastructure;
using UnityEngine;

namespace Code.Battle
{
    public class BattleFlowController
    {
        private BattleStateMachine _stateMachine;
        private readonly IUnitTracker _unitTracker;
        private readonly IEnemySpawnService _enemySpawnService;

        private ExtendedLevel _currentLevel;
        private int _currentStageIndex;
        private bool _isFirstStageOfLevel;

        public ExtendedLevel CurrentLevel => _currentLevel;
        public int CurrentStageIndex => _currentStageIndex;
        public bool IsFirstStageOfLevel => _isFirstStageOfLevel;

        public BattleFlowController(
            IUnitTracker unitTracker,
            IEnemySpawnService enemySpawnService)
        {
            _unitTracker = unitTracker;
            _enemySpawnService = enemySpawnService;
        }

        public void SetStateMachine(BattleStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public async Task StartBattleAsync(ExtendedLevel level)
        {
            if (level == null)
            {
                Debug.LogError("[BattleFlowController] Cannot start battle with null level");
                return;
            }

            _currentLevel = level;
            _currentStageIndex = 0;
            _isFirstStageOfLevel = true;

            Debug.Log($"[BattleFlowController] Starting battle for level {level.Id} with {level.Stages?.Length ?? 0} stages");

            // Start with PreBattleState - it will handle spawning and registration
            await _stateMachine.ChangeStateAsync<PreBattleState>();
        }

        public LevelStageConfig GetCurrentStageConfig()
        {
            if (_currentLevel == null)
            {
                Debug.LogWarning("[BattleFlowController] No current level set");
                return null;
            }

            if (_currentLevel.Stages == null || _currentLevel.Stages.Length == 0)
            {
                Debug.LogError($"[BattleFlowController] Level {_currentLevel.Id} has no stages configured");
                return null;
            }

            if (_currentStageIndex >= _currentLevel.Stages.Length)
            {
                Debug.LogWarning($"[BattleFlowController] Stage index {_currentStageIndex} out of range");
                return null;
            }

            return _currentLevel.Stages[_currentStageIndex];
        }

        public void AdvanceToNextStage()
        {
            _currentStageIndex++;
            _isFirstStageOfLevel = false;
            Debug.Log($"[BattleFlowController] Advanced to stage {_currentStageIndex}");
        }

        public void MarkFirstStageProcessed()
        {
            _isFirstStageOfLevel = false;
        }

        public bool HasMoreStages()
        {
            if (_currentLevel == null || _currentLevel.Stages == null)
                return false;

            return _currentStageIndex < _currentLevel.Stages.Length;
        }

        /// <summary>
        /// Partial cleanup - resets level state but keeps player units alive for next level
        /// </summary>
        public void CleanupLevel()
        {
            Debug.Log($"[BattleFlowController] Cleaning up current level (preserving {_unitTracker.AlivePlayerUnitsCount} player units)");
            _enemySpawnService.ClearEnemies();
            _currentLevel = null;
            _currentStageIndex = 0;
            _isFirstStageOfLevel = false;
            Debug.Log($"[BattleFlowController] Level cleanup complete - {_unitTracker.AlivePlayerUnitsCount} player units still tracked");
        }

        /// <summary>
        /// Full cleanup - resets everything including player units (used when exiting battle completely)
        /// </summary>
        public void Cleanup()
        {
            Debug.Log("[BattleFlowController] Full cleanup - resetting all battle state");
            _unitTracker.Reset();
            _enemySpawnService.ClearEnemies();
            _currentLevel = null;
            _currentStageIndex = 0;
            _isFirstStageOfLevel = false;
        }

        public async Task CleanupAsync()
        {
            Debug.Log("[BattleFlowController] Async cleanup - exiting state machine and cleaning up battle");

            // First cleanup the state machine (exit current state properly)
            if (_stateMachine != null)
                await _stateMachine.CleanupAsync();

            // Then cleanup battle resources
            Cleanup();
        }
    }
}
