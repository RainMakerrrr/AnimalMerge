using System;
using System.Threading;
using Code.Battle.Signals;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Code.Battle.CameraControl
{
    public class BattleCameraPresenter : IInitializable, IDisposable
    {
        private readonly IBattleCameraService _cameraService;
        private readonly SignalBus _signalBus;
        private readonly CancellationTokenSource _lifetimeCts = new CancellationTokenSource();

        public BattleCameraPresenter(IBattleCameraService cameraService, SignalBus signalBus)
        {
            _cameraService = cameraService;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<BattleStartedSignal>(OnBattleStarted);
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Subscribe<BattleEndedSignal>(OnBattleEnded);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<BattleStartedSignal>(OnBattleStarted);
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(OnPreBattlePhaseStarted);
            _signalBus.Unsubscribe<BattleEndedSignal>(OnBattleEnded);

            _lifetimeCts.Cancel();
            _lifetimeCts.Dispose();
        }

        private void OnBattleStarted() =>
            _cameraService.ZoomInAsync(_lifetimeCts.Token).SuppressCancellationThrow().Forget();

        private void OnPreBattlePhaseStarted(PreBattlePhaseStartedSignal signal) => ZoomOut();

        private void OnBattleEnded() => ZoomOut();

        private void ZoomOut() =>
            _cameraService.ZoomOutAsync(_lifetimeCts.Token).SuppressCancellationThrow().Forget();
    }
}
