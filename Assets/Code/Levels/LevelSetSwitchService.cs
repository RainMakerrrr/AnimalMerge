using Code.Battle.Services;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.Infrastructure.States;
using UnityEngine;

namespace Code.Levels
{
    public class LevelSetSwitchService : ILevelSetSwitchService
    {
        private readonly ILevelSetProvider _levelSetProvider;
        private readonly ILevelFactory _levelFactory;
        private readonly ICampaignProgressService _campaignProgress;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadService;
        private readonly IBattleResetService _battleResetService;
        private readonly GameStateMachine _stateMachine;

        public LevelSetSwitchService(
            ILevelSetProvider levelSetProvider,
            ILevelFactory levelFactory,
            ICampaignProgressService campaignProgress,
            IPersistentProgressService progressService,
            ISaveLoadService saveLoadService,
            IBattleResetService battleResetService,
            GameStateMachine stateMachine)
        {
            _levelSetProvider = levelSetProvider;
            _levelFactory = levelFactory;
            _campaignProgress = campaignProgress;
            _progressService = progressService;
            _saveLoadService = saveLoadService;
            _battleResetService = battleResetService;
            _stateMachine = stateMachine;
        }

        public void SwitchTo(LevelSet set)
        {
            if (set == null)
            {
                Debug.LogError("[LevelSetSwitchService] Cannot switch to an empty level set");
                return;
            }

            if (set.Levels.Count == 0 && set.TutorialLevels.Count == 0)
            {
                Debug.LogError(
                    $"[LevelSetSwitchService] '{set.DisplayName}' has no levels, " +
                    "the running game was left on the current set");
                return;
            }

            if (_levelSetProvider.ActiveSet == set)
                return;

            _levelSetProvider.SetActiveSet(set);

            _campaignProgress.ResetToFirstLevel();
            _saveLoadService.Save(_progressService.Progress);

            _levelFactory.Load();

            _battleResetService.ResetForNewRun();
            _stateMachine.Enter<LoadLevelState>();
        }
    }
}
