using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.UI;

namespace Framework.Code.Infrastructure.States
{
	public class LoseState : IState
	{
		private readonly WindowPool _windowPool;
		private readonly IAnalyticsService _analyticsService;
		private readonly IPersistentProgressService _progressService;
		private readonly ILevelFactory _levelFactory;

		public LoseState(WindowPool windowPool, IAnalyticsService analyticsService,
			IPersistentProgressService progressService, ILevelFactory levelFactory)
		{
			_windowPool = windowPool;
			_analyticsService = analyticsService;
			_progressService = progressService;
			_levelFactory = levelFactory;
		}

		public void Enter()
		{
			_analyticsService.LevelCompleted(_levelFactory.CurrentLevel.Id, _progressService.Progress.Level, false,
				_progressService.Progress.Collectables.LevelAmount, _levelFactory.CurrentLevel.TimeSpent);

			_progressService.Progress.Collectables.RevertLevelAmount();

			_windowPool.EnableWindows(WindowType.Lose);
		}

		public void Exit()
		{
		}
	}
}
