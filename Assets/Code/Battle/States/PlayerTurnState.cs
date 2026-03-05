using System.Threading.Tasks;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    public class PlayerTurnState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly ITurnExecutor _turnExecutor;

        public PlayerTurnState(
            BattleStateMachine stateMachine,
            ITurnExecutor turnExecutor)
        {
            _stateMachine = stateMachine;
            _turnExecutor = turnExecutor;
        }

        public async Task Enter()
        {
            Debug.Log("[PlayerTurnState] Entering - Executing player turns");

            await _turnExecutor.ExecutePlayerTurnsAsync();

            Debug.Log("[PlayerTurnState] Player turns complete - transitioning to enemy turn");
            await _stateMachine.ChangeStateAsync<EnemyTurnState>();
        }

        public Task Exit()
        {
            Debug.Log("[PlayerTurnState] Exiting");
            return Task.CompletedTask;
        }
    }
}
