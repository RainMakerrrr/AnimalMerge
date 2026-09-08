using System;
using System.Threading;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Code.Battle.CameraControl
{
    public class BattleCameraService : IBattleCameraService, IDisposable
    {
        private readonly Camera _camera;
        private readonly BattleCameraConfig _config;
        private readonly Vector3 _basePosition;
        private readonly Vector3 _zoomedInPosition;

        private CancellationTokenSource _cancellationTokenSource;

        public BattleCameraService(Camera camera, BattleCameraConfig config)
        {
            _camera = camera;
            _config = config;

            if (_camera == null || _config == null)
            {
                Debug.LogError("[BattleCameraService] Camera or config is missing - battle camera zoom is disabled");
                return;
            }

            _basePosition = _camera.transform.position;
            _zoomedInPosition = _basePosition + _camera.transform.forward * _config.DollyDistance;
        }

        public UniTask ZoomInAsync(CancellationToken cancellationToken)
        {
            if (!CanAnimate)
                return UniTask.CompletedTask;

            return MoveToAsync(
                _zoomedInPosition,
                _config.ZoomInDuration,
                _config.ZoomInCurve,
                _config.ZoomInDelay,
                cancellationToken);
        }

        public UniTask ZoomOutAsync(CancellationToken cancellationToken)
        {
            if (!CanAnimate)
                return UniTask.CompletedTask;

            return MoveToAsync(
                _basePosition,
                _config.ZoomOutDuration,
                _config.ZoomOutCurve,
                0f,
                cancellationToken);
        }

        public void Dispose()
        {
            CancelRunningMove();
        }

        private bool CanAnimate => _camera != null && _config != null;

        private async UniTask MoveToAsync(
            Vector3 to,
            float duration,
            AnimationCurve curve,
            float delay,
            CancellationToken cancellationToken)
        {
            var token = RenewToken(cancellationToken);

            if (delay > 0f)
                await UniTask.Delay(TimeSpan.FromSeconds(delay), cancellationToken: token);

            if (_camera == null)
                return;

            var from = _camera.transform.position;
            var elapsed = 0f;

            while (true)
            {
                var progress = duration > 0f ? Mathf.Clamp01(elapsed / duration) : 1f;

                _camera.transform.position = Vector3.LerpUnclamped(from, to, Evaluate(curve, progress));

                if (progress >= 1f)
                    return;

                await UniTask.Yield(PlayerLoopTiming.Update, token);

                if (_camera == null)
                    return;

                elapsed += Time.deltaTime;
            }
        }

        private CancellationToken RenewToken(CancellationToken externalToken)
        {
            CancelRunningMove();
            _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

            return _cancellationTokenSource.Token;
        }

        private void CancelRunningMove()
        {
            if (_cancellationTokenSource == null)
                return;

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
            _cancellationTokenSource = null;
        }

        private static float Evaluate(AnimationCurve curve, float progress)
        {
            if (curve == null || curve.length == 0)
                return progress;

            return curve.Evaluate(progress);
        }
    }
}
