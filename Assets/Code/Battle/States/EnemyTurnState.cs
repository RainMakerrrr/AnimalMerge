using Cysharp.Threading.Tasks;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    public class EnemyTurnState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly ITurnExecutor _turnExecutor;

        public EnemyTurnState(
            BattleStateMachine stateMachine,
            ITurnExecutor turnExecutor)
        {
            _stateMachine = stateMachine;
            _turnExecutor = turnExecutor;
        }

        public async UniTask Enter()
        {
            Debug.Log("[EnemyTurnState] Entering - Executing enemy turns");

            await _turnExecutor.ExecuteEnemyTurnsAsync();

            Debug.Log("[EnemyTurnState] Enemy turns complete - checking victory conditions");
            await _stateMachine.ChangeStateAsync<CheckVictoryState>();
        }

        public UniTask Exit()
        {
            Debug.Log("[EnemyTurnState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
