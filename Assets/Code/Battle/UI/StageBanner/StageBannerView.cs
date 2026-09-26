using System.Threading;
using Code.Battle.Config;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using Zenject;

namespace Code.Battle.UI.StageBanner
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class StageBannerView : MonoBehaviour, IStageBannerView
    {
        [SerializeField] private GameObject _visuals;
        [SerializeField] private RectTransform _band;
        [SerializeField] private CanvasGroup _bandGroup;
        [SerializeField] private RectTransform _content;
        [SerializeField] private CanvasGroup _contentGroup;
        [SerializeField] private TextMeshProUGUI _title;
        [SerializeField] private CanvasGroup _titleGroup;
        [SerializeField] private TextMeshProUGUI _subtitle;
        [SerializeField] private CanvasGroup _subtitleGroup;

        private RectTransform _rectTransform;
        private CanvasGroup _rootGroup;
        private Canvas _canvas;
        private StageBannerConfig _config;
        private Sequence _sequence;
        private Vector2 _contentRestPosition;

        [Inject]
        private void Construct(Canvas canvas, StageBannerConfig config)
        {
            _canvas = canvas;
            _config = config;
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _rootGroup = GetComponent<CanvasGroup>();
            _rootGroup.blocksRaycasts = false;
            _rootGroup.interactable = false;

            AttachToCanvas();
            ResetToHidden();
        }

        private void OnDestroy() => KillSequence();

        public async UniTask PlayAsync(StageBannerStyle style, CancellationToken cancellationToken)
        {
            if (style == null)
                return;

            KillSequence();
            _rectTransform.SetAsLastSibling();

            ApplyStyle(style);
            PrepareInitialState(style);
            _visuals.SetActive(true);

            var sequence = BuildSequence(style.HasSubtitle);
            var finished = new UniTaskCompletionSource();

            sequence.OnKill(() =>
            {
                if (_sequence == sequence)
                    _sequence = null;

                finished.TrySetResult();
            });

            _sequence = sequence;

            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                       cancellationToken, this.GetCancellationTokenOnDestroy()))
            {
                var canceled = await finished.Task
                    .AttachExternalCancellation(linkedCts.Token)
                    .SuppressCancellationThrow();

                if (canceled && this != null && _sequence == sequence)
                {
                    KillSequence();
                    ResetToHidden();
                }
            }
        }

        private void ApplyStyle(StageBannerStyle style)
        {
            ApplyText(_title, style.Title, style.TitleMaterial, style.TitleGradient);

            _subtitle.gameObject.SetActive(style.HasSubtitle);

            if (!style.HasSubtitle)
                return;

            ApplyText(_subtitle, style.Subtitle, style.SubtitleMaterial, style.SubtitleGradient);
            _subtitle.rectTransform.anchoredPosition = style.SubtitleOffset;
            _subtitle.rectTransform.localEulerAngles = new Vector3(0f, 0f, style.SubtitleTiltZ);
        }

        private static void ApplyText(TMP_Text text, string value, Material material, TMP_ColorGradient gradient)
        {
            text.text = value;
            text.fontSharedMaterial = material != null
                ? material
                : text.font != null ? text.font.material : text.fontSharedMaterial;

            text.enableVertexGradient = gradient != null;
            text.colorGradientPreset = gradient;
        }

        private void PrepareInitialState(StageBannerStyle style)
        {
            _band.localScale = new Vector3(1f, 0f, 1f);
            _bandGroup.alpha = 1f;

            _contentRestPosition = style.ContentOffset;
            _content.anchoredPosition = _contentRestPosition;
            _contentGroup.alpha = 1f;

            _title.rectTransform.localScale = Vector3.one * _config.TitleStartScale;
            _titleGroup.alpha = 0f;

            _subtitle.rectTransform.localScale = Vector3.zero;
            _subtitleGroup.alpha = 0f;
        }

        private Sequence BuildSequence(bool hasSubtitle)
        {
            var sequence = DOTween.Sequence().SetLink(gameObject);

            InsertBandExpand(sequence);
            var titleEnd = InsertTitleSlam(sequence);
            var appearEnd = hasSubtitle ? Mathf.Max(titleEnd, InsertSubtitlePop(sequence)) : titleEnd;
            InsertExit(sequence, appearEnd + _config.HoldDuration);

            return sequence;
        }

        private void InsertBandExpand(Sequence sequence) =>
            sequence.Insert(0f, _band.DOScaleY(1f, _config.BandExpandDuration).SetEase(_config.BandExpandEase));

        private float InsertTitleSlam(Sequence sequence)
        {
            var slamStart = _config.TitleDelay;
            var reboundStart = slamStart + _config.SlamDuration;
            var settleStart = reboundStart + _config.ReboundDuration;

            sequence.Insert(slamStart, _titleGroup.DOFade(1f, _config.TitleFadeInDuration));
            sequence.Insert(slamStart, _title.rectTransform
                .DOScale(ToScale(_config.SquashScale), _config.SlamDuration)
                .SetEase(_config.SlamEase));
            sequence.Insert(reboundStart, _title.rectTransform
                .DOScale(ToScale(_config.StretchScale), _config.ReboundDuration)
                .SetEase(_config.ReboundEase));
            sequence.Insert(settleStart, _title.rectTransform
                .DOScale(Vector3.one, _config.SettleDuration)
                .SetEase(_config.SettleEase));

            return settleStart + _config.SettleDuration;
        }

        private float InsertSubtitlePop(Sequence sequence)
        {
            var popStart = _config.SubtitleDelay;
            var wobbleStart = popStart + _config.SubtitlePopDuration;

            sequence.Insert(popStart, _subtitleGroup.DOFade(1f, _config.SubtitlePopDuration));
            sequence.Insert(popStart, _subtitle.rectTransform
                .DOScale(Vector3.one, _config.SubtitlePopDuration)
                .SetEase(_config.SubtitlePopEase));
            sequence.Insert(wobbleStart, _subtitle.rectTransform.DOPunchRotation(
                Vector3.forward * _config.SubtitleWobbleAngle,
                _config.SubtitleWobbleDuration,
                _config.SubtitleWobbleVibrato,
                _config.SubtitleWobbleElasticity));

            return wobbleStart + _config.SubtitleWobbleDuration;
        }

        private void InsertExit(Sequence sequence, float exitStart)
        {
            var exitTargetX = _contentRestPosition.x + _config.ExitDirection * _config.ExitSlideDistance;

            sequence.Insert(exitStart, _content
                .DOAnchorPosX(exitTargetX, _config.ExitDuration)
                .SetEase(_config.ExitEase));
            sequence.Insert(exitStart, _contentGroup.DOFade(0f, _config.ExitDuration));
            sequence.Insert(exitStart, _bandGroup.DOFade(0f, _config.BandFadeOutDuration));
            sequence.AppendCallback(ResetToHidden);
        }

        private static Vector3 ToScale(Vector2 scale) => new Vector3(scale.x, scale.y, 1f);

        private void AttachToCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogError($"[{nameof(StageBannerView)}] Canvas is not injected - the banner cannot be shown.", this);
                return;
            }

            _rectTransform.anchorMin = Vector2.zero;
            _rectTransform.anchorMax = Vector2.one;
            _rectTransform.offsetMin = Vector2.zero;
            _rectTransform.offsetMax = Vector2.zero;

            if (_rectTransform.parent != _canvas.transform)
                _rectTransform.SetParent(_canvas.transform, false);
        }

        private void ResetToHidden()
        {
            if (_visuals != null)
                _visuals.SetActive(false);
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
