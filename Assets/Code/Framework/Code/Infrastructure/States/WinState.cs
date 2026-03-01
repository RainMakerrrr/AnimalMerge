using DG.Tweening;
using Framework.Code.Data;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.UI;

namespace Framework.Code.Infrastructure.States
{
	public class WinState : IState
	{
		readonly GameStateMachine stateMachine;
		readonly WindowPool windowPool;
		readonly IPersistentProgressService progressService;
		readonly ISaveLoadService saveLoadService;
		readonly IAnalyticsService analyticsService;
		readonly ILevelFactory levelFactory;
		readonly GameData gameData;
		
		public WinState(GameStateMachine stateMachine, WindowPool windowPool,
			IPersistentProgressService progressService, ISaveLoadService saveLoadService,
			IAnalyticsService analyticsService, ILevelFactory levelFactory, IAssetProvider assetProvider)
		{
			this.stateMachine = stateMachine;
			this.windowPool = windowPool;
			this.progressService = progressService;
			this.saveLoadService = saveLoadService;
			this.analyticsService = analyticsService;
			this.levelFactory = levelFactory;
			
			gameData = assetProvider.Load<GameData>(AssetPath.GAME_DATA);
		}

		public void Enter()
		{
			UpdatePlayerProgress();

			saveLoadService.Save(progressService.Progress);

			analyticsService.LevelCompleted(levelFactory.CurrentLevel.Id, progressService.Progress.Level - 1, true,
				progressService.Progress.Collectables.Amount, levelFactory.CurrentLevel.TimeSpent);

			windowPool.EnableWindows(WindowType.Win);

			DOVirtual.DelayedCall(gameData.StateSwitchDelay, () => stateMachine.Enter<LoadLevelState>());
		}

		public void Exit()
		{
		}

		void UpdatePlayerProgress()
		{
			progressService.Progress.Level++;
			progressService.Progress.Collectables.LevelAmount = 0;
		}
	}
}