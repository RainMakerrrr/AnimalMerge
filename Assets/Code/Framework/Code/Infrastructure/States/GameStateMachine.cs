using System;
using System.Collections.Generic;
using Code.Battle;
using Code.Infrastructure.States;
using Framework.Code.Data;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.Assets;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.Infrastructure.Services.Progression;
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

        private readonly Dictionary<Type, IBaseState> _states;
        private readonly SignalBus _signalBus;

        public GameStateMachine(ISaveLoadService saveLoad, IPersistentProgressService progressService,
            ILevelFactory levelFactory, WindowPool windowPool,
            UIRoot uiRoot, IAnalyticsService analyticsService, IAssetProvider assetProvider, SignalBus signalBus,
            BattleFlowController battleFlowController, ICampaignProgressService campaignProgress,
            GameData gameData)
        {
            _signalBus = signalBus;
            _states = new Dictionary<Type, IBaseState>
            {
                {
                    typeof(BootstrapState),
                    new BootstrapState(this, assetProvider, campaignProgress, progressService, saveLoad)
                },
                {
                    typeof(LoadLevelState),
                    new LoadLevelState(this, levelFactory, windowPool, uiRoot, progressService,
                        analyticsService)
                },
                {typeof(LoadProgressState), new LoadProgressState(this, progressService, saveLoad)},
                {typeof(BattleLoopState), new BattleLoopState(battleFlowController, levelFactory)},
                {
                    typeof(WinState),
                    new WinState(this, windowPool, progressService, saveLoad, analyticsService, levelFactory,
                        campaignProgress, gameData)
                },
                {
                    typeof(LoseState),
                    new LoseState(windowPool, analyticsService, progressService, levelFactory)
                },
                {typeof(CampaignVictoryState), new CampaignVictoryState(windowPool)}
            };
        }

        public void Enter<TState>() where TState : class, IState
        {
            var state = ChangeState<TState>();

            _signalBus.Fire(new StateChangedSignal {State = state});

            state.Enter();
        }

        public void Enter<TState, TPayload>(TPayload payload) where TState : class, IPayloadedState<TPayload>
        {
            var state = ChangeState<TState>();
            state.Enter(payload);
        }

        private TState ChangeState<TState>() where TState : class, IBaseState
        {
            ActiveState?.Exit();

            var nextState = _states[typeof(TState)] as TState;
            ActiveState = nextState;

            return nextState;
        }
    }
}
