using System.Threading;
using Code.Battle.Config;
using Code.Battle.Input;
using Code.Battle.Services;
using Code.Battle.StateMachine;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Battle.States
{
    public class BattleStartState : IBattleState
    {
        private readonly BattleStateMachine _stateMachine;
        private readonly BattleFlowController _flowController;
        private readonly IStageAnnouncer _stageAnnouncer;
        private readonly IPlayerInputLock _inputLock;

        private CancellationTokenSource _cancellationTokenSource;

        public BattleStartState(
            BattleStateMachine stateMachine,
            BattleFlowController flowController,
            IStageAnnouncer stageAnnouncer,
            IPlayerInputLock inputLock)
        {
            _stateMachine = stateMachine;
            _flowController = flowController;
            _stageAnnouncer = stageAnnouncer;
            _inputLock = inputLock;
        }

        public async UniTask Enter()
        {
            Debug.Log("[BattleStartState] Battle starting - announcing FIGHT before the first turn");

            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(_flowController.BattleToken);
            var stateToken = _cancellationTokenSource.Token;

            bool completed;

            using (_inputLock.Acquire())
                completed = await _stageAnnouncer.AnnounceAsync(StageBannerKind.Fight, stateToken);

            if (!completed)
            {
                Debug.Log("[BattleStartState] FIGHT announcement interrupted - first turn skipped");
                return;
            }

            await _stateMachine.ChangeStateAsync<PlayerTurnState>();
        }

        public UniTask Exit()
        {
            Debug.Log("[BattleStartState] Exiting");

            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            return UniTask.CompletedTask;
        }
    }
}
