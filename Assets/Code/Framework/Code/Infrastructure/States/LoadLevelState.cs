using Code.Infrastructure.States;
using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.Services.PersistentProgress;
using Framework.Code.UI;
using Framework.Code.UI.Elements;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Framework.Code.Infrastructure.States
{
    public class LoadLevelState : IState
    {
        private readonly GameStateMachine _stateMachine;
        private readonly ILevelFactory _levelFactory;
        private readonly WindowPool _windowPool;
        private readonly UIRoot _uiRoot;
        private readonly IPersistentProgressService _progressService;
        private readonly IAnalyticsService _analyticsService;

        private Level _currentLevel;

        public LoadLevelState(GameStateMachine stateMachine, ILevelFactory levelFactory, WindowPool windowPool,
            UIRoot uiRoot,
            IPersistentProgressService progressService, IAnalyticsService analyticsService)
        {
            _stateMachine = stateMachine;
            _levelFactory = levelFactory;
            _windowPool = windowPool;
            _uiRoot = uiRoot;
            _progressService = progressService;
            _analyticsService = analyticsService;

            _levelFactory.Load();
        }

        public void Enter()
        {
            UpdateUI();
            InitLevel();

            if (_currentLevel == null)
                return;

            _analyticsService.LevelStarted(_currentLevel.Id, _progressService.Progress.Level);

            _windowPool.DisableWindows(WindowType.Tutorial);
            _stateMachine.Enter<BattleLoopState>();
        }

        public void Exit()
        {
        }

        private void UpdateUI()
        {
            _windowPool.DisableAllWindows();

            foreach (IViewUpdatable updater in _uiRoot.ViewUpdaters)
            {
                updater.UpdateView();
            }
        }

        private void InitLevel()
        {
            if (_currentLevel != null)
                Object.Destroy(_currentLevel.gameObject);

            _currentLevel = _levelFactory.Create();

            if (_currentLevel != null)
                _analyticsService.LevelLoaded(_currentLevel.Id, _progressService.Progress.Level);
            else Debug.LogError("Level is not loaded");
        }
    }
}
