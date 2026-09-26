using System.Threading;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Code.Animals.UI
{
    public class MergePopupView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI _text;

        [Header("Float")]
        [SerializeField] private float _floatHeight = 150f;
        [SerializeField, Min(0.01f)] private float _duration = 1.2f;
        [SerializeField] private Ease _floatEase = Ease.OutCubic;

        [Header("Scale")]
        [SerializeField, Min(0f)] private float _startScale = 0.4f;
        [SerializeField, Min(0f)] private float _targetScale = 1f;
        [SerializeField, Min(0f)] private float _scaleDuration = 0.35f;
        [SerializeField] private Ease _scaleEase = Ease.OutBack;
        [SerializeField, Min(0f)] private float _scaleOvershoot = 1.7f;

        [Header("Fade")]
        [SerializeField, Min(0f)] private float _fadeInDuration = 0.15f;
        [SerializeField] private Ease _fadeInEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _fadeOutDuration = 0.4f;
        [SerializeField] private Ease _fadeOutEase = Ease.InQuad;

        private RectTransform _rectTransform;
        private Sequence _sequence;

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnDestroy() => KillSequence();

        public async UniTask PlayAsync(string content, Color color, CancellationToken cancellationToken)
        {
            KillSequence();

            _text.text = content;
            _text.color = color;
            _text.alpha = 0f;
            _rectTransform.localScale = Vector3.one * _startScale;

            var fadeOutStart = Mathf.Max(0f, _duration - _fadeOutDuration);

            var sequence = DOTween.Sequence()
                .Insert(0f, _rectTransform
                    .DOAnchorPosY(_rectTransform.anchoredPosition.y + _floatHeight, _duration)
                    .SetEase(_floatEase))
                .Insert(0f, _rectTransform
                    .DOScale(_targetScale, _scaleDuration)
                    .SetEase(_scaleEase, _scaleOvershoot))
                .Insert(0f, _text
                    .DOFade(color.a, _fadeInDuration)
                    .SetEase(_fadeInEase))
                .Insert(fadeOutStart, _text
                    .DOFade(0f, _fadeOutDuration)
                    .SetEase(_fadeOutEase));

            var finished = new UniTaskCompletionSource();

            sequence.OnKill(() =>
            {
                if (_sequence == sequence)
                    _sequence = null;

                finished.TrySetResult();
            });
            sequence.OnComplete(() => Destroy(gameObject));

            _sequence = sequence;

            var canceled = await finished.Task
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (canceled && this != null)
            {
                KillSequence();
                Destroy(gameObject);
            }
        }

        private void KillSequence()
        {
            if (_sequence == null)
                return;

            var sequence = _sequence;
            _sequence = null;
            sequence.Kill();
        }
    }
}
