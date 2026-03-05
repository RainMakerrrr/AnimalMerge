using System.Threading.Tasks;
using Code.Battle;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    public class CheckVictoryState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly IVictoryConditionChecker _victoryChecker;
        private readonly BattleFlowController _flowController;

        public CheckVictoryState(
            BattleStateMachine stateMachine,
            IVictoryConditionChecker victoryChecker,
            BattleFlowController flowController)
        {
            _stateMachine = stateMachine;
            _victoryChecker = victoryChecker;
            _flowController = flowController;
        }

        public Task Enter()
        {
            Debug.Log("[CheckVictoryState] Entering - Checking battle conditions");

            var result = _victoryChecker.CheckBattleConditions();

            switch (result)
            {
                case BattleResult.Victory:
                    Debug.Log("[CheckVictoryState] Stage cleared! Checking for more stages...");
                    return _stateMachine.ChangeStateAsync<StageClearState>();

                case BattleResult.Defeat:
                    Debug.Log("[CheckVictoryState] Defeat! All player units dead");
                    return _stateMachine.ChangeStateAsync<BattleEndState>();

                case BattleResult.Ongoing:
                    Debug.Log("[CheckVictoryState] Battle ongoing - continuing to next turn round");
                    return _stateMachine.ChangeStateAsync<PlayerTurnState>();

                default:
                    return Task.CompletedTask;
            }
        }

        public Task Exit()
        {
            Debug.Log("[CheckVictoryState] Exiting");
            return Task.CompletedTask;
        }
    }
}
