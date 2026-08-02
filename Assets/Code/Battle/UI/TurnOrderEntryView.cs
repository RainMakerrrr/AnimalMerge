using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Code.Battle.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class TurnOrderEntryView : MonoBehaviour
    {
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Image _grayscalePortrait;
        [SerializeField] private Image _colorPortrait;
        [SerializeField] private Image _frame;
        [SerializeField] private Color _allyAccent = new Color(0.961f, 0.651f, 0.137f, 1f);
        [SerializeField] private Color _enemyAccent = new Color(0.898f, 0.235f, 0.208f, 1f);
        [SerializeField] private float _activeScale = 1.15f;
        [SerializeField] private float _moveDuration = 0.28f;
        [SerializeField] private Ease _moveEase = Ease.OutCubic;
        [SerializeField] private float _colorFadeDuration = 0.25f;
        [SerializeField] private float _activateDuration = 0.25f;
        [SerializeField] private Ease _activateEase = Ease.OutBack;
        [SerializeField] private float _deactivateDuration = 0.2f;
        [SerializeField] private Ease _deactivateEase = Ease.OutQuad;
        [SerializeField] private float _retireDuration = 0.15f;
        [SerializeField] private float _appearDuration = 0.15f;

        private RectTransform _rectTransform;
        private Tween _moveTween;
        private Tween _scaleTween;
        private Tween _portraitTween;
        private Tween _frameTween;
        private Tween _retireTween;
        private Tween _appearTween;

        public int Id { get; private set; }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();
        }

        private void OnDisable() => KillTweens();

        private void OnDestroy() => KillTweens();

        public void Bind(TurnOrderEntryData data, Vector2 slotPosition, bool animated)
        {
            if (animated == false)
                Restore();

            Id = data.Id;

            ApplyPortrait(data, animated);
            ApplyFrame(data, animated);
            ApplyScale(data.IsActive, animated);
            ApplyPosition(slotPosition, animated);
        }

        public void Retire()
        {
            KillMoveTween();
            KillScaleTween();
            KillPortraitTween();
            KillFrameTween();
            KillAppearTween();
            KillRetireTween();

            if (gameObject.activeSelf == false)
                return;

            _retireTween = _canvasGroup.DOFade(0f, _retireDuration).OnComplete(CompleteRetire);
        }

        private void Restore()
        {
            var startAlpha = _retireTween != null ? _canvasGroup.alpha : 0f;

            KillRetireTween();
            KillAppearTween();

            _canvasGroup.alpha = startAlpha;

            gameObject.SetActive(true);

            _appearTween = _canvasGroup.DOFade(1f, _appearDuration).OnComplete(CompleteAppear);
        }

        private void CompleteAppear() => _appearTween = null;

        private void CompleteRetire()
        {
            _retireTween = null;

            gameObject.SetActive(false);
        }

        private void ApplyPortrait(TurnOrderEntryData data, bool animated)
        {
            var hasPortrait = data.Portrait != null;

            _grayscalePortrait.sprite = data.Portrait;
            _grayscalePortrait.enabled = hasPortrait;

            _colorPortrait.sprite = data.Portrait;
            _colorPortrait.enabled = hasPortrait;

            KillPortraitTween();

            var targetAlpha = data.IsActive ? 1f : 0f;

            if (animated == false)
            {
                _colorPortrait.color = WithAlpha(_colorPortrait.color, targetAlpha);
                return;
            }

            _portraitTween = _colorPortrait.DOFade(targetAlpha, _colorFadeDuration);
        }

        private void ApplyFrame(TurnOrderEntryData data, bool animated)
        {
            var accent = data.IsEnemy ? _enemyAccent : _allyAccent;
            var targetAlpha = data.IsActive ? accent.a : 0f;

            KillFrameTween();

            if (animated == false)
            {
                _frame.color = WithAlpha(accent, targetAlpha);
                return;
            }

            _frame.color = WithAlpha(accent, _frame.color.a);
            _frameTween = _frame.DOFade(targetAlpha, _colorFadeDuration);
        }

        private void ApplyScale(bool isActive, bool animated)
        {
            KillScaleTween();

            var targetScale = isActive ? _activeScale : 1f;

            if (animated == false)
            {
                _content.localScale = Vector3.one * targetScale;
                return;
            }

            var duration = isActive ? _activateDuration : _deactivateDuration;
            var ease = isActive ? _activateEase : _deactivateEase;

            _scaleTween = _content.DOScale(targetScale, duration).SetEase(ease);
        }

        private void ApplyPosition(Vector2 slotPosition, bool animated)
        {
            KillMoveTween();

            if (animated == false)
            {
                _rectTransform.anchoredPosition = slotPosition;
                return;
            }

            if (_rectTransform.anchoredPosition == slotPosition)
                return;

            _moveTween = _rectTransform.DOAnchorPos(slotPosition, _moveDuration).SetEase(_moveEase);
        }

        private void KillTweens()
        {
            KillMoveTween();
            KillScaleTween();
            KillPortraitTween();
            KillFrameTween();
            KillAppearTween();
            KillRetireTween();
        }

        private void KillMoveTween()
        {
            if (_moveTween == null)
                return;

            _moveTween.Kill();
            _moveTween = null;
        }

        private void KillScaleTween()
        {
            if (_scaleTween == null)
                return;

            _scaleTween.Kill();
            _scaleTween = null;
        }

        private void KillPortraitTween()
        {
            if (_portraitTween == null)
                return;

            _portraitTween.Kill();
            _portraitTween = null;
        }

        private void KillFrameTween()
        {
            if (_frameTween == null)
                return;

            _frameTween.Kill();
            _frameTween = null;
        }

        private void KillRetireTween()
        {
            if (_retireTween == null)
                return;

            _retireTween.Kill();
            _retireTween = null;
        }

        private void KillAppearTween()
        {
            if (_appearTween == null)
                return;

            _appearTween.Kill();
            _appearTween = null;
        }

        private static Color WithAlpha(Color color, float alpha) =>
            new Color(color.r, color.g, color.b, alpha);
    }
}
