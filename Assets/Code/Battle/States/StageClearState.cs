using System.Threading.Tasks;
using Code.Battle;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    public class StageClearState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly BattleFlowController _flowController;
        private readonly IHealthRestorationService _healthRestorationService;
        private readonly IEnemySpawnService _enemySpawnService;
        private readonly IUnitTracker _unitTracker;

        public StageClearState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            IHealthRestorationService healthRestorationService,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _healthRestorationService = healthRestorationService;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
        }

        public Task Enter()
        {
            Debug.Log("[StageClearState] Entering - Stage complete!");

            // Restore HP of surviving player units
            var playerUnits = _unitTracker.GetAlivePlayerUnits();
            _healthRestorationService.RestoreHealthForSurvivingUnits(playerUnits);
            Debug.Log($"[StageClearState] Restored HP for {playerUnits.Count} surviving player units");

            // Clear enemy units from grid
            _enemySpawnService.ClearEnemies();
            Debug.Log("[StageClearState] Cleared enemy units");

            // Check if more stages exist
            _flowController.AdvanceToNextStage();

            if (_flowController.HasMoreStages())
            {
                Debug.Log("[StageClearState] More stages remaining - showing enemies for next stage");
                return _stateMachine.ChangeStateAsync<PreBattleState>();
            }
            else
            {
                Debug.Log("[StageClearState] All stages complete - VICTORY!");
                return _stateMachine.ChangeStateAsync<BattleEndState>();
            }
        }

        public Task Exit()
        {
            Debug.Log("[StageClearState] Exiting");
            return Task.CompletedTask;
        }
    }
}
