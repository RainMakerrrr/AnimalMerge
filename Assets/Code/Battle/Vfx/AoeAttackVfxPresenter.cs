using System;
using Code.Battle.Config;
using Code.Battle.Signals;
using UnityEngine;
using Zenject;

namespace Code.Battle.Vfx
{
    public class AoeAttackVfxPresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly IVfxSpawner _vfxSpawner;
        private readonly BattleVfxConfig _config;

        public AoeAttackVfxPresenter(SignalBus signalBus, IVfxSpawner vfxSpawner, BattleVfxConfig config)
        {
            _signalBus = signalBus;
            _vfxSpawner = vfxSpawner;
            _config = config;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<AoeAttackLandedSignal>(OnAoeAttackLanded);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<AoeAttackLandedSignal>(OnAoeAttackLanded);
        }

        private void OnAoeAttackLanded(AoeAttackLandedSignal signal)
        {
            var position = signal.Center + Vector3.up * _config.AoeWaveHeightOffset;
            var rotation = signal.Forward.sqrMagnitude > 0f ? Quaternion.LookRotation(signal.Forward) : Quaternion.identity;
            float scale = signal.Radius * _config.AoeWaveRadiusMultiplier / _config.AoeWaveAuthoredRadius;

            _vfxSpawner.Spawn(_config.AoeWavePrefab, position, rotation, scale);
        }
    }
}
