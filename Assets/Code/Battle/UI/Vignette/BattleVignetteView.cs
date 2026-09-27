using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Code.Battle.UI.Vignette
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class BattleVignetteView : MonoBehaviour, IBattleVignetteView
    {
        private RectTransform _rectTransform;
        private CanvasGroup _canvasGroup;
        private Canvas _canvas;
        private Tween _fadeTween;

        [Inject]
        private void Construct(Canvas canvas)
        {
            _canvas = canvas;
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            AttachToCanvas();
            HideImmediate();
        }

        private void OnDestroy() => KillFade();

        public void FadeIn(float delay, float duration, Ease ease, float alpha)
        {
            KillFade();

            _fadeTween = _canvasGroup
                .DOFade(alpha, duration)
                .SetDelay(delay)
                .SetEase(ease);
        }

        public void FadeOut(float duration, Ease ease)
        {
            KillFade();

            _fadeTween = _canvasGroup
                .DOFade(0f, duration)
                .SetEase(ease);
        }

        public void HideImmediate()
        {
            KillFade();
            _canvasGroup.alpha = 0f;
        }

        private void KillFade()
        {
            _fadeTween?.Kill();
            _fadeTween = null;
        }

        private void AttachToCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogError($"[{nameof(BattleVignetteView)}] Canvas is not injected - the vignette cannot be shown.", this);
                return;
            }

            if (_rectTransform.parent != _canvas.transform)
                _rectTransform.SetParent(_canvas.transform, false);

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;
            _rectTransform.SetAsFirstSibling();
        }
    }
}
