using Framework.Code.Infrastructure.Signals;
using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code
{
    public class GameStateDebugger : MonoBehaviour
    {
        SignalBus signalBus;
        IBaseState currentState;
        
        [Inject]
        void Construct(SignalBus signalBus)
        {
            this.signalBus = signalBus;
        }

        private void OnEnable() => signalBus.Subscribe<StateChangedSignal>(OnStateChanged);

        private void OnStateChanged(StateChangedSignal stateChangedSignal)
        {
            if (currentState == null)
            {
                currentState = stateChangedSignal.State;
                return;
            }
            
            IBaseState nextState = stateChangedSignal.State;
            
            if(nextState == currentState)
                Debug.LogError("You enter the same state");
            if(nextState is LoseState && currentState is WinState)
                Debug.LogError("You enter Lose State from Win State");
            if(nextState is WinState && currentState is LoseState)
                Debug.LogError("You enter Win State from Lose State");
            if((currentState is WinState || currentState is LoseState) && nextState is BootstrapState)
                Debug.LogError("You enter Bootstrap State from Win or Lose State");

            currentState = nextState;
        }

        private void OnDisable() => signalBus.Unsubscribe<StateChangedSignal>(OnStateChanged);
    }
}