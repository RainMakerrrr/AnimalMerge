using System;
using Code.Battle.CameraControl;
using Code.Battle.Config;
using Code.Battle.Signals;
using Zenject;

namespace Code.Battle.Vfx
{
    public class UnitAttackShakePresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly ICameraShakeService _cameraShakeService;
        private readonly UnitBattleFeedbackConfig _config;

        public UnitAttackShakePresenter(
            SignalBus signalBus,
            ICameraShakeService cameraShakeService,
            UnitBattleFeedbackConfig config)
        {
            _signalBus = signalBus;
            _cameraShakeService = cameraShakeService;
            _config = config;
        }

        public void Initialize() =>
            _signalBus.Subscribe<UnitAttackLandedSignal>(OnUnitAttackLanded);

        public void Dispose() =>
            _signalBus.Unsubscribe<UnitAttackLandedSignal>(OnUnitAttackLanded);

        private void OnUnitAttackLanded(UnitAttackLandedSignal signal)
        {
            if (signal.Attacker == null)
                return;

            if (!_config.TryGetFeedback(signal.Attacker.AnimalType, out var entry) || !entry.ShakeCameraOnAttack)
                return;

            _cameraShakeService.Shake(entry.AttackShake);
        }
    }
}
