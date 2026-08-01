using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;

namespace Framework.Code.Infrastructure.States
{
	public class LoadProgressState : IState
	{
		private readonly GameStateMachine _stateMachine;
		private readonly IPersistentProgressService _progressService;
		private readonly ISaveLoadService _saveLoadService;

		public LoadProgressState(GameStateMachine stateMachine, IPersistentProgressService progressService,
			ISaveLoadService saveLoadService)
		{
			_stateMachine = stateMachine;
			_progressService = progressService;
			_saveLoadService = saveLoadService;
		}

		public void Enter()
		{
			_progressService.Progress = _saveLoadService.LoadProgress();
			_progressService.Data = _saveLoadService.LoadData();
			_stateMachine.Enter<LoadLevelState>();
		}

		public void Exit()
		{
		}
	}
}
