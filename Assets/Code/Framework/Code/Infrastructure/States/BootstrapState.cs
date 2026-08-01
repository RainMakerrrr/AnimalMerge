using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
using Framework.Code.Infrastructure.Services.SaveSystem;

namespace Framework.Code.Infrastructure.States
{
	public class BootstrapState : IState
	{
		private readonly GameStateMachine _stateMachine;
		private readonly IAssetProvider _assetProvider;
		private readonly ICampaignProgressService _campaignProgress;
		private readonly IPersistentProgressService _progressService;
		private readonly ISaveLoadService _saveLoadService;

		public BootstrapState(GameStateMachine stateMachine, IAssetProvider assetProvider,
			ICampaignProgressService campaignProgress, IPersistentProgressService progressService,
			ISaveLoadService saveLoadService)
		{
			_stateMachine = stateMachine;
			_assetProvider = assetProvider;
			_campaignProgress = campaignProgress;
			_progressService = progressService;
			_saveLoadService = saveLoadService;
		}

		public void Enter()
		{
			_assetProvider.Clear();

			if (_campaignProgress.IsCampaignCompleted)
			{
				_campaignProgress.ResetToFirstLevel();
				_saveLoadService.Save(_progressService.Progress);
			}

			_stateMachine.Enter<LoadLevelState>();
		}

		public void Exit()
		{
		}
	}
}
