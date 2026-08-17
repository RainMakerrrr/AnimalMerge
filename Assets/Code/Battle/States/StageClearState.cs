using System;
using Cysharp.Threading.Tasks;
using Code.Battle;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using Framework.Code.Data;
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
        private readonly GameData _gameData;

        public StageClearState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            IHealthRestorationService healthRestorationService,
            IEnemySpawnService enemySpawnService,
            IUnitTracker unitTracker,
            IUnitRepositioningService unitRepositioningService,
            GameData gameData)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _healthRestorationService = healthRestorationService;
            _enemySpawnService = enemySpawnService;
            _unitTracker = unitTracker;
            _unitRepositioningService = unitRepositioningService;
            _gameData = gameData;
        }

        public async UniTask Enter()
        {
            Debug.Log("[StageClearState] Entering - Stage complete!");

            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(_gameData.DeathAnimationDelay), cancellationToken: _flowController.BattleToken)
                .SuppressCancellationThrow();

            if (isCanceled)
            {
                Debug.Log("[StageClearState] Canceled while waiting for death animations");
                return;
            }

            var playerUnits = _unitTracker.GetAlivePlayerUnits();

            _healthRestorationService.RestoreHealthForSurvivingUnits(playerUnits);
            Debug.Log($"[StageClearState] Restored HP for {playerUnits.Count} units");

            _unitRepositioningService.RepositionUnitsToDeploymentZone(playerUnits);
            Debug.Log($"[StageClearState] Repositioned {playerUnits.Count} units to the deployment zone");

            _enemySpawnService.ClearEnemies();

            _flowController.AdvanceToNextStage();

            if (_flowController.HasMoreStages())
            {
                Debug.Log("[StageClearState] More stages - transitioning to PreBattleState");
                await _stateMachine.ChangeStateAsync<PreBattleState>();
            }
            else
            {
                Debug.Log("[StageClearState] All stages complete - VICTORY!");
                await _stateMachine.ChangeStateAsync<BattleEndState>();
            }
        }

        public UniTask Exit()
        {
            Debug.Log("[StageClearState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
