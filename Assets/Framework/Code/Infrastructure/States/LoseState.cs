using DG.Tweening;
using Framework.Code.Data;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.UI;

namespace Framework.Code.Infrastructure.States
{
	public class LoseState : IState
	{
		readonly WindowPool windowPool;
		readonly GameStateMachine stateMachine;
		readonly IAnalyticsService analyticsService;
		readonly IPersistentProgressService progressService;
		readonly ILevelFactory levelFactory;
		readonly GameData gameData;

		public LoseState(WindowPool windowPool, GameStateMachine stateMachine, IAnalyticsService analyticsService,
			IPersistentProgressService progressService, ILevelFactory levelFactory, IAssetProvider assetProvider)
		{
			this.windowPool = windowPool;
			this.stateMachine = stateMachine;
			this.analyticsService = analyticsService;
			this.progressService = progressService;
			this.levelFactory = levelFactory;
			
			gameData = assetProvider.Load<GameData>(AssetPath.GAME_DATA);
		}

		public void Enter()
		{
			analyticsService.LevelCompleted(levelFactory.CurrentLevel.Id, progressService.Progress.Level, false,
				progressService.Progress.Collectables.LevelAmount, levelFactory.CurrentLevel.TimeSpent);
			
			ResetCollectablesProgress();

			windowPool.EnableWindows(WindowType.Lose);

			DOVirtual.DelayedCall(gameData.StateSwitchDelay, () => stateMachine.Enter<LoadLevelState>());
		}

		public void Exit()
		{
		}

		void ResetCollectablesProgress()
		{
			progressService.Progress.Collectables.ResetAmount();
			progressService.Progress.Collectables.LevelAmount = 0;
		}
	}
}