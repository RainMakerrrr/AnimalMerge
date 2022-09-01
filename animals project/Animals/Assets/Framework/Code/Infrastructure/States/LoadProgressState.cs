using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;

namespace Framework.Code.Infrastructure.States
{
	public class LoadProgressState : IState
	{
		readonly GameStateMachine stateMachine;
		readonly IPersistentProgressService progressService;
		readonly ISaveLoadService saveLoadService;

		public LoadProgressState(GameStateMachine stateMachine, IPersistentProgressService progressService, ISaveLoadService saveLoadService)
		{
			this.stateMachine = stateMachine;
			this.progressService = progressService;
			this.saveLoadService = saveLoadService;
		}

		public void Enter()
		{
			progressService.Progress = saveLoadService.LoadProgress();
			progressService.Data = saveLoadService.LoadData();
			stateMachine.Enter<LoadLevelState>();
		}

		public void Exit()
		{
		}
	}
}