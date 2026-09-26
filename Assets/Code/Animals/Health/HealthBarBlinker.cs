using DG.Tweening;
using UnityEngine;

namespace Code.Animals.Health
{
    [RequireComponent(typeof(CanvasGroup))]
    public class HealthBarBlinker : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _canvasGroup;

        private Tween _tween;

        public bool IsPlaying => _tween != null && _tween.IsActive();

        private void OnDisable() => Stop();

        private void OnDestroy() => Stop();

        public void Play(float halfPeriod, float minAlpha)
        {
            if (IsPlaying)
                return;

            _canvasGroup.alpha = 1f;
            _tween = _canvasGroup
                .DOFade(minAlpha, halfPeriod)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .SetLink(gameObject);
        }

        public void Stop()
        {
            if (_tween != null)
            {
                _tween.Kill();
                _tween = null;
            }

            if (_canvasGroup != null)
                _canvasGroup.alpha = 1f;
        }
    }
}
