using System.Threading.Tasks;
using Code.Battle.StateMachine;
using UnityEngine;

namespace Code.Battle.States
{
    /// <summary>
    /// Battle start state - confirms battle beginning and transitions to first turn
    /// All units (player and enemy) are already spawned in PreBattleState
    /// </summary>
    public class BattleStartState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;

        public BattleStartState(BattleStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        public Task Enter()
        {
            Debug.Log("[BattleStartState] Battle starting - transitioning to player turn");

            // All setup is done in PreBattleState, just start the battle
            return _stateMachine.ChangeStateAsync<PlayerTurnState>();
        }

        public Task Exit()
        {
            Debug.Log("[BattleStartState] Exiting");
            return Task.CompletedTask;
        }
    }
}
