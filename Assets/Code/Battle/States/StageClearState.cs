using Cysharp.Threading.Tasks;
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
        private readonly IUnitRepositioningService _unitRepositioningService;

        public StageClearState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            IHealthRestorationService healthRestorationService,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker,
            IUnitRepositioningService unitRepositioningService)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _healthRestorationService = healthRestorationService;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
            _unitRepositioningService = unitRepositioningService;
        }

        public UniTask Enter()
        {
            Debug.Log("[StageClearState] Entering - Stage complete!");

            // 1. Get surviving player units
            var playerUnits = _unitTracker.GetAlivePlayerUnits();

            // 2. Restore HP
            _healthRestorationService.RestoreHealthForSurvivingUnits(playerUnits);
            Debug.Log($"[StageClearState] Restored HP for {playerUnits.Count} units");

            // 3. Reposition to merge grid
            _unitRepositioningService.RepositionUnitsToMergeGrid(playerUnits);
            Debug.Log($"[StageClearState] Repositioned {playerUnits.Count} units to merge grid");

            // 4. Clear enemies
            _enemySpawnService.ClearEnemies();

            // 5. Advance stage and transition
            _flowController.AdvanceToNextStage();

            if (_flowController.HasMoreStages())
            {
                Debug.Log("[StageClearState] More stages - transitioning to PreBattleState");
                return _stateMachine.ChangeStateAsync<PreBattleState>();
            }
            else
            {
                Debug.Log("[StageClearState] All stages complete - VICTORY!");
                return _stateMachine.ChangeStateAsync<BattleEndState>();
            }
        }

        public UniTask Exit()
        {
            Debug.Log("[StageClearState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
