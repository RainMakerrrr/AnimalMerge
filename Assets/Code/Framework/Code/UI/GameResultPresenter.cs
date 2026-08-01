using System;
using Framework.Code.Infrastructure.Services.GameRestart;
using Framework.Code.UI.Windows;
using Zenject;

namespace Framework.Code.UI
{
    public class GameResultPresenter : IInitializable, IDisposable
    {
        private readonly DefeatWindowView _defeatWindow;
        private readonly CampaignVictoryWindowView _campaignVictoryWindow;
        private readonly IGameRestartService _gameRestartService;

        public GameResultPresenter(DefeatWindowView defeatWindow, CampaignVictoryWindowView campaignVictoryWindow,
            IGameRestartService gameRestartService)
        {
            _defeatWindow = defeatWindow;
            _campaignVictoryWindow = campaignVictoryWindow;
            _gameRestartService = gameRestartService;
        }

        public void Initialize()
        {
            _defeatWindow.TryAgainClicked += OnTryAgainClicked;
            _campaignVictoryWindow.RestartClicked += OnRestartClicked;
        }

        public void Dispose()
        {
            _defeatWindow.TryAgainClicked -= OnTryAgainClicked;
            _campaignVictoryWindow.RestartClicked -= OnRestartClicked;
        }

        private void OnTryAgainClicked() => _gameRestartService.RetryCurrentLevel();

        private void OnRestartClicked() => _gameRestartService.RestartCampaign();
    }
}
