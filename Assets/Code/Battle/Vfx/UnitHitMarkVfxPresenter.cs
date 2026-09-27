using System;
using Code.Animals.Facades;
using Code.Battle.Config;
using Code.Battle.Signals;
using UnityEngine;
using Zenject;

namespace Code.Battle.Vfx
{
    public class UnitHitMarkVfxPresenter : IInitializable, IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly IVfxSpawner _vfxSpawner;
        private readonly UnitBattleFeedbackConfig _config;
        private readonly Camera _camera;

        public UnitHitMarkVfxPresenter(
            SignalBus signalBus,
            IVfxSpawner vfxSpawner,
            UnitBattleFeedbackConfig config,
            Camera camera)
        {
            _signalBus = signalBus;
            _vfxSpawner = vfxSpawner;
            _config = config;
            _camera = camera;
        }

        public void Initialize()
        {
            _signalBus.Subscribe<UnitDamagedSignal>(OnUnitDamaged);
        }

        public void Dispose()
        {
            _signalBus.Unsubscribe<UnitDamagedSignal>(OnUnitDamaged);
        }

        private void OnUnitDamaged(UnitDamagedSignal signal)
        {
            if (signal.Attacker == null || signal.Target == null)
                return;

            if (!_config.TryGetFeedback(signal.Attacker.AnimalType, out var entry))
                return;

            var hitMark = entry.HitMark;
            if (hitMark == null || hitMark.Prefab == null)
                return;

            var target = signal.TargetUnit;
            var position = CalculatePosition(signal.Target.transform, target, hitMark);
            var scale = UnitVfxScale.Calculate(target, hitMark.ScalePerCell);

            _vfxSpawner.Spawn(hitMark.Prefab, position, Quaternion.identity, scale);
        }

        private Vector3 CalculatePosition(Transform fallbackTransform, AnimalFacade target, UnitHitMarkSettings hitMark)
        {
            var center = TryGetCollidersCenter(target, out var collidersCenter)
                ? collidersCenter
                : fallbackTransform.position + Vector3.up * hitMark.HeightOffset;

            if (_camera == null)
                return center;

            var towardCamera = (_camera.transform.position - center).normalized;
            return center + towardCamera * hitMark.TowardCameraOffset;
        }

        private static bool TryGetCollidersCenter(AnimalFacade target, out Vector3 center)
        {
            center = Vector3.zero;

            if (target == null || target.Colliders == null)
                return false;

            var hasBounds = false;
            var bounds = new Bounds();

            foreach (var collider in target.Colliders)
            {
                if (collider == null || !collider.enabled)
                    continue;

                if (hasBounds)
                {
                    bounds.Encapsulate(collider.bounds);
                    continue;
                }

                bounds = collider.bounds;
                hasBounds = true;
            }

            if (hasBounds)
                center = bounds.center;

            return hasBounds;
        }
    }
}
