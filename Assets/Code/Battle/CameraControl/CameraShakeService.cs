using System;
using Code.Battle.Config;
using DG.Tweening;
using UnityEngine;

namespace Code.Battle.CameraControl
{
    public class CameraShakeService : ICameraShakeService, IDisposable
    {
        private readonly Transform _cameraTransform;
        private readonly Quaternion _baseLocalRotation;

        private Tween _shakeTween;

        public CameraShakeService(Camera camera)
        {
            if (camera == null)
            {
                Debug.LogError($"[{nameof(CameraShakeService)}] Camera is missing - camera shake is disabled");
                return;
            }

            _cameraTransform = camera.transform;
            _baseLocalRotation = _cameraTransform.localRotation;
        }

        public void Shake(CameraShakeSettings settings)
        {
            if (_cameraTransform == null || settings == null)
                return;

            StopShake();

            _shakeTween = _cameraTransform
                .DOShakeRotation(settings.Duration, settings.Strength, settings.Vibrato, settings.Randomness, settings.FadeOut)
                .OnKill(() => _shakeTween = null);
        }

        public void Dispose() => StopShake();

        private void StopShake()
        {
            _shakeTween?.Kill();
            _shakeTween = null;

            if (_cameraTransform != null)
                _cameraTransform.localRotation = _baseLocalRotation;
        }
    }
}
