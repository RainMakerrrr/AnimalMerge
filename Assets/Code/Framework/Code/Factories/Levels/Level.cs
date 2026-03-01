using Code.Infrastructure.States;
using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code.Factories.Levels
{
	public class Level : MonoBehaviour
	{
		[SerializeField] private string id;

		private GameStateMachine stateMachine;

		public string Id => id;
		public float TimeSpent { get; private set; }


		[Inject]
		private void Construct(GameStateMachine stateMachine)
		{
			this.stateMachine = stateMachine;
		}

		private void OnEnable() => TimeSpent = 0f;

		private void Update()
		{
			if (stateMachine.ActiveState is BattleLoopState == false) return;

			TimeSpent += Time.deltaTime;
		}
	}
}