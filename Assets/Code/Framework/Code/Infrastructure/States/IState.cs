namespace Framework.Code.Infrastructure.States
{
	public interface IState : IBaseState
	{
		void Enter();
	}

	public interface IBaseState
	{
		void Exit();
	}

	public interface IPayloadedState<TPayload> : IBaseState
	{
		void Enter(TPayload payload);
	}
}