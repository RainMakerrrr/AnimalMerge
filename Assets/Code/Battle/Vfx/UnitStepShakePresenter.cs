using System;
using Code.Animals.Movement;
using Code.Battle.CameraControl;
using Code.Battle.Config;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.Vfx
{
    public class UnitStepShakePresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly ICameraShakeService _cameraShakeService;
        private readonly UnitBattleFeedbackConfig _config;

        private AnimalMovement _trackedMovement;
        private CameraShakeSettings _trackedShake;

        public UnitStepShakePresenter(
            SignalBus signalBus,
            ICameraShakeService cameraShakeService,
            UnitBattleFeedbackConfig config)
        {
            _signalBus = signalBus;
            _cameraShakeService = cameraShakeService;
            _config = config;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Subscribe<UnitTurnCompletedSignal>(OnUnitTurnCompleted);
            _signalBus.Subscribe<BattleEndedSignal>(StopTracking);
            _signalBus.Subscribe<PreBattlePhaseStartedSignal>(StopTracking);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<UnitTurnStartedSignal>(OnUnitTurnStarted);
            _signalBus.Unsubscribe<UnitTurnCompletedSignal>(OnUnitTurnCompleted);
            _signalBus.Unsubscribe<BattleEndedSignal>(StopTracking);
            _signalBus.Unsubscribe<PreBattlePhaseStartedSignal>(StopTracking);

            StopTracking();
        }

        private void OnUnitTurnStarted(UnitTurnStartedSignal signal)
        {
            StopTracking();

            var unit = signal.Unit;
            if (unit == null || unit.Movement == null)
                return;

            if (!_config.TryGetFeedback(unit.Type, out var entry) || !entry.ShakeCameraOnStep)
                return;

            _trackedMovement = unit.Movement;
            _trackedShake = entry.StepShake;
            _trackedMovement.StepTaken += OnStepTaken;
        }

        private void OnUnitTurnCompleted(UnitTurnCompletedSignal signal) => StopTracking();

        private void OnStepTaken() => _cameraShakeService.Shake(_trackedShake);

        private void StopTracking()
        {
            if (_trackedMovement != null)
                _trackedMovement.StepTaken -= OnStepTaken;

            _trackedMovement = null;
            _trackedShake = null;
        }
    }
}
