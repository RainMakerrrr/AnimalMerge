using Cysharp.Threading.Tasks;
using Code.Battle;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using Framework.Code.Infrastructure.States;
using UnityEngine;

namespace Code.Battle.States
{
    public class BattleEndState : IBattleState
    {
        private readonly GameStateMachine _gameStateMachine;
        private readonly BattleFlowController _flowController;
        private readonly IVictoryConditionChecker _victoryChecker;

        public BattleEndState(
            GameStateMachine gameStateMachine,
            BattleFlowController flowController,
            IVictoryConditionChecker victoryChecker)
        {
            _gameStateMachine = gameStateMachine;
            _flowController = flowController;
            _victoryChecker = victoryChecker;
        }

        public UniTask Enter()
        {
            Debug.Log("[BattleEndState] Entering - Finalizing battle");

            var result = _victoryChecker.CheckBattleConditions();

            // Cleanup current level but preserve player units for next level
            // Full cleanup will happen when player exits battle completely
            _flowController.CleanupLevel();

            // Transition to appropriate game state
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

            return UniTask.CompletedTask;
        }

        public UniTask Exit()
        {
            Debug.Log("[BattleEndState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
