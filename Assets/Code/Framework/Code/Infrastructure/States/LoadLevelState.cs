using Code.Infrastructure.States;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.UI;
using Framework.Code.UI.Elements;
using Lean.Touch;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Framework.Code.Infrastructure.States
{
    public class LoadLevelState : IState
    {
        readonly GameStateMachine stateMachine;
        readonly ILevelFactory levelFactory;
        readonly WindowPool windowPool;
        readonly UIRoot uiRoot;
        readonly IPersistentProgressService progressService;
        readonly IAnalyticsService analyticsService;

        Level currentLevel;

        public LoadLevelState(GameStateMachine stateMachine, ILevelFactory levelFactory, WindowPool windowPool,
            UIRoot uiRoot,
            IPersistentProgressService progressService, IAnalyticsService analyticsService)
        {
            this.stateMachine = stateMachine;
            this.levelFactory = levelFactory;
            this.windowPool = windowPool;
            this.uiRoot = uiRoot;
            this.progressService = progressService;
            this.analyticsService = analyticsService;

            this.levelFactory.Load();
        }

        public void Enter()
        {
            UpdateUI();
            InitLevel();

            LeanTouch.OnFingerDown += OnFingerDown;
        }

        public void Exit()
        {
            LeanTouch.OnFingerDown -= OnFingerDown;
        }

        void OnFingerDown(LeanFinger finger)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;

            if (currentLevel != null)
                analyticsService.LevelStarted(currentLevel.Id, progressService.Progress.Level);

            windowPool.DisableWindows(WindowType.Tutorial);
            stateMachine.Enter<BattleLoopState>();
        }

        void UpdateUI()
        {
            windowPool.DisableAllWindows();
            windowPool.EnableWindows(WindowType.Tutorial);

            foreach (IViewUpdatable updater in uiRoot.ViewUpdaters)
            {
                updater.UpdateView();
            }
        }

        void InitLevel()
        {
            if (currentLevel != null)
                Object.Destroy(currentLevel.gameObject);
            
            currentLevel = levelFactory.Create();

            if (currentLevel != null)
                analyticsService.LevelLoaded(currentLevel.Id, progressService.Progress.Level);
            else Debug.LogError("Level is not loaded");
        }
    }
}