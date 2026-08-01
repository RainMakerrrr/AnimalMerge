using Framework.Code.Infrastructure.Signals;
using Framework.Code.Infrastructure.States;
using UnityEngine;
using Zenject;

namespace Framework.Code
{
    public class GameStateDebugger : MonoBehaviour
    {
        private SignalBus _signalBus;
        private IBaseState _currentState;

        [Inject]
        private void Construct(SignalBus signalBus)
        {
            _signalBus = signalBus;
        }

        private void OnEnable() => _signalBus.Subscribe<StateChangedSignal>(OnStateChanged);

        private void OnStateChanged(StateChangedSignal stateChangedSignal)
        {
            if (_currentState == null)
            {
                _currentState = stateChangedSignal.State;
                return;
            }

            IBaseState nextState = stateChangedSignal.State;

            if (nextState == _currentState)
                Debug.LogError("You enter the same state");
            if (nextState is LoseState && _currentState is WinState)
                Debug.LogError("You enter Lose State from Win State");
            if (nextState is WinState && _currentState is LoseState)
                Debug.LogError("You enter Win State from Lose State");
            if (nextState is LoseState && _currentState is CampaignVictoryState)
                Debug.LogError("You enter Lose State from Campaign Victory State");
            if (IsResultState(_currentState) && nextState is BootstrapState)
                Debug.LogError("You enter Bootstrap State from a result state");

            _currentState = nextState;
        }

        private void OnDisable() => _signalBus.Unsubscribe<StateChangedSignal>(OnStateChanged);

        private static bool IsResultState(IBaseState state) =>
            state is WinState || state is LoseState || state is CampaignVictoryState;
    }
}
