using DG.Tweening;
using Framework.Code.Data;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.UI;

namespace Framework.Code.Infrastructure.States
{
	public class WinState : IState
	{
		private readonly GameStateMachine _stateMachine;
		private readonly WindowPool _windowPool;
		private readonly IPersistentProgressService _progressService;
		private readonly ISaveLoadService _saveLoadService;
		private readonly IAnalyticsService _analyticsService;
		private readonly ILevelFactory _levelFactory;
		private readonly ICampaignProgressService _campaignProgress;
		private readonly GameData _gameData;

		public WinState(GameStateMachine stateMachine, WindowPool windowPool,
			IPersistentProgressService progressService, ISaveLoadService saveLoadService,
			IAnalyticsService analyticsService, ILevelFactory levelFactory,
			ICampaignProgressService campaignProgress, GameData gameData)
		{
			_stateMachine = stateMachine;
			_windowPool = windowPool;
			_progressService = progressService;
			_saveLoadService = saveLoadService;
			_analyticsService = analyticsService;
			_levelFactory = levelFactory;
			_campaignProgress = campaignProgress;
			_gameData = gameData;
		}

		public void Enter()
		{
			UpdatePlayerProgress();

			_saveLoadService.Save(_progressService.Progress);

			_analyticsService.LevelCompleted(_levelFactory.CurrentLevel.Id, _progressService.Progress.Level - 1, true,
				_progressService.Progress.Collectables.Amount, _levelFactory.CurrentLevel.TimeSpent);

			if (_campaignProgress.IsCampaignCompleted)
			{
				_stateMachine.Enter<CampaignVictoryState>();
				return;
			}

			_windowPool.EnableWindows(WindowType.Win);

			DOVirtual.DelayedCall(_gameData.StateSwitchDelay, () => _stateMachine.Enter<LoadLevelState>());
		}

		public void Exit()
		{
		}

		private void UpdatePlayerProgress()
		{
			_progressService.Progress.Level++;
			_progressService.Progress.Collectables.LevelAmount = 0;
		}
	}
}
