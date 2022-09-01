using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code.Factories.Levels
{
	public class Level : MonoBehaviour
	{
		[SerializeField] string id;

		GameStateMachine stateMachine;
		public string Id => id;

		public float TimeSpent { get; private set; }


		[Inject]
		void Construct(GameStateMachine stateMachine)
		{
			this.stateMachine = stateMachine;
		}

		void OnEnable() => TimeSpent = 0f;

		void Update()
		{
			if (stateMachine.ActiveState is GameLoopState == false) return;

			TimeSpent += Time.deltaTime;
		}
	}
}