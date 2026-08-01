using System;
using Cysharp.Threading.Tasks;
using Code.Battle;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using Framework.Code.Data;
using Framework.Code.Infrastructure.States;
using UnityEngine;

namespace Code.Battle.States
{
    public class BattleEndState : IBattleState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly BattleFlowController _flowController;
        private readonly IVictoryConditionChecker _victoryChecker;
        private readonly GameData _gameData;

        public BattleEndState(
            GameStateMachine gameStateMachine,
            BattleFlowController flowController,
            IVictoryConditionChecker victoryChecker,
            GameData gameData)
        {
            _gameStateMachine = gameStateMachine;
            _flowController = flowController;
            _victoryChecker = victoryChecker;
            _gameData = gameData;
        }

        public async UniTask Enter()
        {
            Debug.Log("[BattleEndState] Entering - Finalizing battle");

            var result = _victoryChecker.CheckBattleConditions();

            if (result != BattleResult.Victory && await WaitForDeathAnimations() == false)
            {
                Debug.Log("[BattleEndState] Canceled while waiting for death animations");
                return;
            }

            _flowController.CleanupLevel();

            if (result == BattleResult.Victory)
            {
                Debug.Log("[BattleEndState] Transitioning to WinState");
                _gameStateMachine.Enter<WinState>();
            }
            else
            {
                Debug.Log("[BattleEndState] Transitioning to LoseState");
                _gameStateMachine.Enter<LoseState>();
            }
        }

        private async UniTask<bool> WaitForDeathAnimations()
        {
            var isCanceled = await UniTask
                .Delay(TimeSpan.FromSeconds(_gameData.DeathAnimationDelay), cancellationToken: _flowController.BattleToken)
                .SuppressCancellationThrow();

            return isCanceled == false;
        }

        public UniTask Exit()
        {
            Debug.Log("[BattleEndState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
