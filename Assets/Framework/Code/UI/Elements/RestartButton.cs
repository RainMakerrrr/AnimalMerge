using Framework.Code.Factories.Levels;
using Framework.Code.Infrastructure.Services.Analytics;
using Framework.Code.Infrastructure.States;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Framework.Code.UI.Elements
{
    public class RestartButton : MonoBehaviour
    {
        Button button;
        GameStateMachine stateMachine;
        IAnalyticsService analyticsService;
        ILevelFactory levelFactory;

        [Inject]
        void Construct(GameStateMachine stateMachine, IAnalyticsService analyticsService, ILevelFactory levelFactory)
        {
            this.stateMachine = stateMachine;
            this.analyticsService = analyticsService;
            this.levelFactory = levelFactory;
        }

        void Start()
        {
            button = GetComponent<Button>();
            button.onClick.AddListener(RestartLevel);
        }

        void OnDestroy() => button.onClick.RemoveListener(RestartLevel);

        void RestartLevel()
        {
            if (stateMachine.ActiveState is WinState) return;
            if (stateMachine.ActiveState is LoseState) return;

            analyticsService.LevelRestarted(levelFactory.CurrentLevel.Id, levelFactory.CurrentLevel.TimeSpent);
            
            stateMachine.Enter<LoadLevelState>();
        }
    }
}