using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Tutorial.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class TutorialHandView : MonoBehaviour, ITutorialHandView
    {
        [SerializeField] private Image _handImage;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.4f, 0f);
        [SerializeField] private float _fadeDuration = 0.2f;
        [SerializeField] private float _pressDuration = 0.18f;
        [SerializeField] private float _pressScale = 0.82f;
        [SerializeField] private float _slideDuration = 0.9f;
        [SerializeField] private float _loopPause = 0.6f;

        private RectTransform _rectTransform;
        private RectTransform _canvasRectTransform;
        private Canvas _canvas;
        private Camera _camera;
        private Sequence _sequence;
        private CancellationTokenSource _loopCancellation;

        [Inject]
        private void Construct(Canvas canvas, Camera camera)
        {
            _canvas = canvas;
            _camera = camera;
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            if (_handImage == null)
                _handImage = GetComponentInChildren<Image>(true);

            if (_handImage != null)
                _handImage.raycastTarget = false;

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            AttachToCanvas();
            ResetToHiddenState();
        }

        private void OnDestroy() => Hide();

        public void Show(Transform source, Transform target)
        {
            if (source == null || target == null)
                return;

            Hide();

            _loopCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                this.GetCancellationTokenOnDestroy());

            PlayLoopAsync(source, target, _loopCancellation.Token).Forget();
        }

        public void Hide()
        {
            if (_loopCancellation != null)
            {
                _loopCancellation.Cancel();
                _loopCancellation.Dispose();
                _loopCancellation = null;
            }

            KillSequence();
            ResetToHiddenState();
        }

        private async UniTaskVoid PlayLoopAsync(Transform source, Transform target, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (source == null || target == null)
                    return;

                if (!TryGetAnchoredPosition(source, out var sourcePosition) ||
                    !TryGetAnchoredPosition(target, out var targetPosition))
                {
                    var skipCanceled = await UniTask
                        .Yield(PlayerLoopTiming.Update, cancellationToken)
                        .SuppressCancellationThrow();

                    if (skipCanceled)
                        return;

                    continue;
                }

                PrepareForSlide(sourcePosition);

                KillSequence();

                var slideSequence = BuildSlideSequence(targetPosition);
                _sequence = slideSequence;

                var slideCanceled = await UniTask
                    .WaitUntil(
                        () => !slideSequence.IsActive() || slideSequence.IsComplete(),
                        cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (slideCanceled)
                    return;

                var pauseCanceled = await UniTask
                    .Delay(TimeSpan.FromSeconds(_loopPause), cancellationToken: cancellationToken)
                    .SuppressCancellationThrow();

                if (pauseCanceled)
                    return;
            }
        }

        private void PrepareForSlide(Vector2 sourcePosition)
        {
            _rectTransform.anchoredPosition = sourcePosition;
            _rectTransform.localScale = Vector3.one;
            _canvasGroup.alpha = 0f;

            if (_handImage != null)
                _handImage.enabled = true;
        }

        private Sequence BuildSlideSequence(Vector2 targetPosition)
        {
            var sequence = DOTween.Sequence();

            sequence.Append(_canvasGroup.DOFade(1f, _fadeDuration));
            sequence.Append(_rectTransform.DOScale(_pressScale, _pressDuration).SetLoops(2, LoopType.Yoyo));
            sequence.Append(_rectTransform.DOAnchorPos(targetPosition, _slideDuration).SetEase(Ease.InOutSine));
            sequence.Append(_rectTransform.DOScale(_pressScale, _pressDuration).SetLoops(2, LoopType.Yoyo));
            sequence.Append(_canvasGroup.DOFade(0f, _fadeDuration));

            return sequence;
        }

        private bool TryGetAnchoredPosition(Transform worldTarget, out Vector2 anchoredPosition)
        {
            anchoredPosition = Vector2.zero;

            if (_camera == null || _canvasRectTransform == null)
                return false;

            var screenPosition = _camera.WorldToScreenPoint(worldTarget.position + _worldOffset);

            if (screenPosition.z < 0f)
                return false;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _canvasRectTransform,
                screenPosition,
                null,
                out anchoredPosition);
        }

        private void AttachToCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogError($"[{nameof(TutorialHandView)}] Canvas is not injected - the hand cannot be positioned.", this);
                return;
            }

            _canvasRectTransform = _canvas.transform as RectTransform;

            if (_rectTransform.parent != _canvas.transform)
                _rectTransform.SetParent(_canvas.transform, false);

            _rectTransform.SetAsLastSibling();
        }

        private void ResetToHiddenState()
        {
            if (_canvasGroup != null)
                _canvasGroup.alpha = 0f;

            if (_handImage != null)
                _handImage.enabled = false;
        }

        private void KillSequence()
        {
            if (_sequence == null)
                return;

            _sequence.Kill();
            _sequence = null;
        }
    }
}
