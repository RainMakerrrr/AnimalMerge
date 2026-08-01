using Code.Battle.Services;
using Code.Infrastructure.States;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.Infrastructure.States;

namespace Framework.Code.Infrastructure.Services.GameRestart
{
    public class GameRestartService : IGameRestartService
    {
        private readonly GameStateMachine _stateMachine;
        private readonly ICampaignProgressService _campaignProgress;
        private readonly IPersistentProgressService _progressService;
        private readonly ISaveLoadService _saveLoadService;
        private readonly IAnalyticsService _analyticsService;
        private readonly ILevelFactory _levelFactory;
        private readonly IBattleResetService _battleResetService;

        public GameRestartService(GameStateMachine stateMachine, ICampaignProgressService campaignProgress,
            IPersistentProgressService progressService, ISaveLoadService saveLoadService,
            IAnalyticsService analyticsService, ILevelFactory levelFactory, IBattleResetService battleResetService)
        {
            _stateMachine = stateMachine;
            _campaignProgress = campaignProgress;
            _progressService = progressService;
            _saveLoadService = saveLoadService;
            _analyticsService = analyticsService;
            _levelFactory = levelFactory;
            _battleResetService = battleResetService;
        }

        public void RetryCurrentLevel()
        {
            if (CanRetryCurrentLevel == false)
                return;

            ReportLevelRestarted();

            _battleResetService.ResetForNewRun();
            _stateMachine.Enter<LoadLevelState>();
        }

        public void RestartCampaign()
        {
            if (_stateMachine.ActiveState is CampaignVictoryState == false)
                return;

            _campaignProgress.ResetToFirstLevel();
            _saveLoadService.Save(_progressService.Progress);

            _battleResetService.ResetForNewRun();
            _stateMachine.Enter<LoadLevelState>();
        }

        private bool CanRetryCurrentLevel =>
            _stateMachine.ActiveState is LoseState || _stateMachine.ActiveState is BattleLoopState;

        private void ReportLevelRestarted()
        {
            if (_levelFactory.CurrentLevel == null)
                return;

            _analyticsService.LevelRestarted(_levelFactory.CurrentLevel.Id, _levelFactory.CurrentLevel.TimeSpent);
        }
    }
}
