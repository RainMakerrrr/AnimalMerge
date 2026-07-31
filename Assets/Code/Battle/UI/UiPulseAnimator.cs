using DG.Tweening;
using UnityEngine;

namespace Code.Battle.UI
{
    public class UiPulseAnimator : MonoBehaviour
    {
        [SerializeField] private float _scale = 1.08f;
        [SerializeField] private float _duration = 0.5f;

        private Tween _tween;
        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        public bool IsPlaying => _tween != null && _tween.IsActive() && _tween.IsPlaying();

        private void Awake() => CaptureBaseScale();

        private void OnDisable() => Stop();

        private void OnDestroy() => Stop();

        public void Play()
        {
            CaptureBaseScale();

            if (IsPlaying)
                return;

            Stop();

            _tween = transform
                .DOScale(_baseScale * _scale, _duration)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }

        public void Stop()
        {
            if (_tween != null)
            {
                _tween.Kill();
                _tween = null;
            }

            if (_baseScaleCaptured)
                transform.localScale = _baseScale;
        }

        private void CaptureBaseScale()
        {
            if (_baseScaleCaptured)
                return;

            _baseScale = transform.localScale;
            _baseScaleCaptured = true;
        }
    }
}
