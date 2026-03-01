using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code.Infrastructure
{
    public class GameBootstrapper : MonoBehaviour
    {
        GameStateMachine stateMachine;

        [Inject]
        void Construct(GameStateMachine stateMachine)
        {
            this.stateMachine = stateMachine;
        }

        void Awake()
        {
            Application.targetFrameRate = 60;
            stateMachine.Enter<BootstrapState>();
        }
    }
}