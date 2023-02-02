using Framework.Code.Infrastructure.Services.Assets;

namespace Framework.Code.Infrastructure.States
{
	public class BootstrapState : IState
	{
		readonly GameStateMachine stateMachine;
		readonly IAssetProvider assetProvider;

		public BootstrapState(GameStateMachine stateMachine, IAssetProvider assetProvider)
		{
			this.stateMachine = stateMachine;
			this.assetProvider = assetProvider;
		}

		public void Enter()
		{
			assetProvider.Clear();
			stateMachine.Enter<LoadLevelState>();
		}

		public void Exit()
		{
		}
	}
}