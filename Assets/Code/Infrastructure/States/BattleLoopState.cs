using Code.Battle;
using Code.Infrastructure;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.States;
using UnityEngine;

namespace Code.Infrastructure.States
{
    /// <summary>
    /// Battle-enabled version of GameLoopState
    /// Integrates BattleFlowController with game loop
    /// </summary>
    public class BattleLoopState : IState
    {
        private readonly BattleFlowController _battleFlowController;
        private readonly ILevelFactory _levelFactory;

        public BattleLoopState(
            BattleFlowController battleFlowController,
            ILevelFactory levelFactory)
        {
            _battleFlowController = battleFlowController;
            _levelFactory = levelFactory;
        }

        public async void Enter()
        {
            Debug.Log("[BattleLoopState] Entering - Starting battle");

            var currentLevel = _levelFactory.CurrentLevel;

            if (currentLevel == null)
            {
                Debug.LogError("[BattleLoopState] No current level found!");
                return;
            }

            // Check if level is ExtendedLevel with battle stages
            if (currentLevel is ExtendedLevel extendedLevel)
            {
                Debug.Log($"[BattleLoopState] ExtendedLevel detected with {extendedLevel.Stages?.Length ?? 0} stages");
                await _battleFlowController.StartBattleAsync(extendedLevel);
            }
            else
            {
                Debug.LogWarning("[BattleLoopState] Regular Level detected, no battle system available");
            }
        }

        public async void Exit()
        {
            Debug.Log("[BattleLoopState] Exiting");
            // NOTE: Do NOT call CleanupAsync() here!
            // BattleEndState already handles cleanup:
            // - CleanupLevel() on victory (preserves player units for next level)
            // - Cleanup() on defeat (full reset)
            // Calling CleanupAsync() here would incorrectly clear player units between levels
        }
    }
}
