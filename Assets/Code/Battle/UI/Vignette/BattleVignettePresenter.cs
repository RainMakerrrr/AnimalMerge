using System;
using Code.Battle.Config;
using Code.Battle.Services;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.UI.Vignette
{
    public class BattleVignettePresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly IBattleVignetteView _view;
        private readonly IUnitTracker _unitTracker;
        private readonly UnitBattleFeedbackConfig _config;

        public BattleVignettePresenter(
            SignalBus signalBus,
            IBattleVignetteView view,
            IUnitTracker unitTracker,
            UnitBattleFeedbackConfig config)
        {
            _signalBus = signalBus;
            _view = view;
            _unitTracker = unitTracker;
            _config = config;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<BattleStartedSignal>(OnBattleStarted);
            _signalBus.Subscribe<BattleEndedSignal>(Hide);
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(Hide);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<BattleStartedSignal>(OnBattleStarted);
            _signalBus.Unsubscribe<BattleEndedSignal>(Hide);
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(Hide);
        }

        private void OnBattleStarted()
        {
            if (!HasVignetteEnemy())
                return;

            _view.FadeIn(
                _config.VignetteDelay,
                _config.VignetteFadeInDuration,
                _config.VignetteFadeInEase,
                _config.VignetteMaxAlpha);
        }

        private void Hide() =>
            _view.FadeOut(_config.VignetteFadeOutDuration, _config.VignetteFadeOutEase);

        private bool HasVignetteEnemy()
        {
            foreach (var enemy in _unitTracker.GetAliveEnemyUnits())
            {
                if (enemy != null
                    && _config.TryGetFeedback(enemy.Type, out var entry)
                    && entry.ShowBattleVignette)
                    return true;
            }

            return false;
        }
    }
}
