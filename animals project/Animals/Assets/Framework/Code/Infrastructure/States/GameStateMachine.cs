using System;
using System.Collections.Generic;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.SaveSystem;
using Framework.Code.Infrastructure.Signals;
using Framework.Code.UI;
using Framework.Code.UI.Elements;
using Zenject;

namespace Framework.Code.Infrastructure.States
{
    public class GameStateMachine
    {
        public IBaseState ActiveState { get; private set; }

        readonly Dictionary<Type, IBaseState> states;
        readonly SignalBus signalBus;

        public GameStateMachine(ISaveLoadService saveLoad, IPersistentProgressService progressService,
            ILevelFactory levelFactory, WindowPool windowPool,
            UIRoot uiRoot, IAnalyticsService analyticsService, IAssetProvider assetProvider, SignalBus signalBus)
        {
            this.signalBus = signalBus;
            states = new Dictionary<Type, IBaseState>
            {
                {typeof(BootstrapState), new BootstrapState(this, assetProvider)},
                {
                    typeof(LoadLevelState),
                    new LoadLevelState(this, levelFactory, windowPool, uiRoot, progressService,
                        analyticsService)
                },
                {typeof(LoadProgressState), new LoadProgressState(this, progressService, saveLoad)},
                {typeof(GameLoopState), new GameLoopState()},
                {
                    typeof(WinState),
                    new WinState(this, windowPool, progressService, saveLoad, analyticsService, levelFactory,
                        assetProvider)
                },
                {
                    typeof(LoseState),
                    new LoseState(windowPool, this, analyticsService, progressService, levelFactory, assetProvider)
                }
            };
        }

        public void Enter<TState>() where TState : class, IState
        {
            TState state = ChangeState<TState>();
            state.Enter();

            signalBus.Fire(new StateChangedSignal {State = state});
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            TState state = ChangeState<TState>();
            state.Enter(payload);
        }

        TState ChangeState<TState>() where TState : class, IBaseState
        {
            ActiveState?.Exit();

            TState nextState = states[typeof(TState)] as TState;
            ActiveState = nextState;

            return nextState;
        }
    }
}