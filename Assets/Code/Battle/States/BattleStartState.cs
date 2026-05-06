using Cysharp.Threading.Tasks;
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

        public UniTask Enter()
        {
            Debug.Log("[BattleStartState] Battle starting - transitioning to player turn");

            // All setup is done in PreBattleState, just start the battle
            return _stateMachine.ChangeStateAsync<PlayerTurnState>();
        }

        public UniTask Exit()
        {
            Debug.Log("[BattleStartState] Exiting");
            return UniTask.CompletedTask;
        }
    }
}
