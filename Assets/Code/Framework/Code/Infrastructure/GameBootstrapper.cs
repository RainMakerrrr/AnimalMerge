using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code.Infrastructure
{
    public class GameBootstrapper : MonoBehaviour
    {
        private GameStateMachine _stateMachine;

        [Inject]
        private void Construct(GameStateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            _stateMachine.Enter<BootstrapState>();
        }
    }
}
