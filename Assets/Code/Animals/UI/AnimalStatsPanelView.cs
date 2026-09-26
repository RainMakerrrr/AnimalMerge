using System.Collections.Generic;
using System.Threading;
using Code.Animals.UI.MergeStats;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Code.Animals.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class AnimalStatsPanelView : MonoBehaviour, IAnimalStatsPanelView
    {
        private const string MovesFormat = "x{0}";
        private const string PercentFormat = "{0:+0;-0}%";

        [SerializeField] private Image _headIcon;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _attackLabel;
        [SerializeField] private TextMeshProUGUI _healthLabel;
        [SerializeField] private TextMeshProUGUI _movesLabel;
        [SerializeField] private GameObject _abilitiesDivider;
        [SerializeField] private TextMeshProUGUI[] _abilityLabels;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.6f, 0f);
        [SerializeField] private Vector2 _screenOffset = new Vector2(-220f, 60f);
        [SerializeField] private float _edgePadding = 12f;
        [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private float _appearScale = 0.85f;
        [SerializeField] private float _abilityLineHeight;

        [Header("Stat Change Rows")]
        [SerializeField] private RectTransform _attackRow;
        [SerializeField] private RectTransform _healthRow;
        [SerializeField] private RectTransform _movesRow;
        [SerializeField] private TextMeshProUGUI _attackChangeLabel;
        [SerializeField] private TextMeshProUGUI _healthChangeLabel;
        [SerializeField] private TextMeshProUGUI _movesChangeLabel;
        [SerializeField] private Material _gainMaterial;
        [SerializeField] private Material _lossMaterial;
        [SerializeField] private float _changeLabelSpacing = 8f;

        [Header("Stat Change Timing")]
        [SerializeField, Min(0f)] private float _statChangeStartDelay = 0.2f;
        [SerializeField, Min(0f)] private float _percentAppearDuration = 0.12f;
        [SerializeField, Min(0f)] private float _percentHoldDuration = 0.35f;
        [SerializeField, Min(0f)] private float _percentFadeDuration = 0.15f;
        [SerializeField] private float _percentRiseDistance = 12f;
        [SerializeField, Min(0f)] private float _countDuration = 0.45f;
        [SerializeField] private Ease _countEase = Ease.OutQuad;
        [SerializeField, Min(0f)] private float _punchScale = 0.25f;
        [SerializeField, Min(0f)] private float _punchDuration = 0.25f;
        [SerializeField, Min(0)] private int _punchVibrato = 6;
        [SerializeField, Range(0f, 1f)] private float _punchElasticity = 0.5f;
        [SerializeField, Min(0f)] private float _statChangeGap = 0.1f;

        private RectTransform _rectTransform;
        private RectTransform _canvasRectTransform;
        private Canvas _canvas;
        private Camera _camera;
        private Transform _anchor;
        private Sequence _sequence;
        private Sequence _statSequence;
        private readonly Dictionary<MergeStatKind, StatRowWidgets> _statRows =
            new Dictionary<MergeStatKind, StatRowWidgets>();
        private float _baseHeight;
        private float _abilityLinesHeight;

        [Inject]
        private void Construct(Canvas canvas, Camera camera)
        {
            _canvas = canvas;
            _camera = camera;
        }

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;
            _baseHeight = _rectTransform.sizeDelta.y;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            CollectStatRows();
            StopStatChanges();
            AttachToCanvas();
        }

        private void LateUpdate()
        {
            if (ReferenceEquals(_anchor, null))
                return;

            if (_anchor == null)
            {
                Hide();
                return;
            }

            UpdatePosition();
        }

        private void OnDestroy()
        {
            KillSequence();
            KillStatSequence();
        }

        public void Show(Transform anchor, Sprite icon, AnimalCardStats stats, string title,
            IReadOnlyList<string> abilityLines)
        {
            if (anchor == null)
                return;

            _anchor = anchor;

            StopStatChanges();
            ApplyIcon(icon);
            ApplyTitle(title);
            ApplyAbilityLines(abilityLines);
            UpdateStats(stats);
            UpdatePosition();

            KillSequence();

            _rectTransform.localScale = Vector3.one * _appearScale;

            _sequence = DOTween.Sequence();
            _sequence.Join(_canvasGroup.DOFade(1f, _fadeDuration));
            _sequence.Join(_rectTransform.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack));
        }

        public void UpdateAbilityLines(IReadOnlyList<string> abilityLines) => ApplyAbilityLines(abilityLines);

        public void UpdateStats(AnimalCardStats stats)
        {
            if (_attackLabel != null)
                _attackLabel.text = FormatValue(MergeStatKind.Damage, stats.Attack);

            if (_healthLabel != null)
                _healthLabel.text = FormatValue(MergeStatKind.Health, stats.Health);

            if (_movesLabel != null)
                _movesLabel.text = FormatValue(MergeStatKind.Moves, stats.TilesPerMove);
        }

        public async UniTask PlayStatChangesAsync(IReadOnlyList<MergeStatChange> changes,
            CancellationToken cancellationToken)
        {
            StopStatChanges();

            if (changes == null || changes.Count == 0)
                return;

            var sequence = BuildStatChangeSequence(changes);
            var finished = new UniTaskCompletionSource();

            sequence.OnKill(() =>
            {
                if (_statSequence == sequence)
                    _statSequence = null;

                finished.TrySetResult();
            });

            _statSequence = sequence;

            var canceled = await finished.Task
                .AttachExternalCancellation(cancellationToken)
                .SuppressCancellationThrow();

            if (canceled && this != null && _statSequence == sequence)
                StopStatChanges();
        }

        public void StopStatChanges()
        {
            KillStatSequence();

            foreach (var row in _statRows.Values)
                ResetStatRow(row);
        }

        public void Hide()
        {
            _anchor = null;

            StopStatChanges();
            KillSequence();

            if (_canvasGroup == null)
                return;

            _sequence = DOTween.Sequence();
            _sequence.Join(_canvasGroup.DOFade(0f, _fadeDuration));
        }

        private void CollectStatRows()
        {
            _statRows.Clear();

            TryAddStatRow(MergeStatKind.Damage, _attackRow, _attackLabel, _attackChangeLabel);
            TryAddStatRow(MergeStatKind.Health, _healthRow, _healthLabel, _healthChangeLabel);
            TryAddStatRow(MergeStatKind.Moves, _movesRow, _movesLabel, _movesChangeLabel);
        }

        private void TryAddStatRow(MergeStatKind kind, RectTransform row, TextMeshProUGUI valueLabel,
            TextMeshProUGUI changeLabel)
        {
            if (row == null || valueLabel == null || changeLabel == null)
                return;

            _statRows[kind] = new StatRowWidgets(row, valueLabel, changeLabel,
                changeLabel.rectTransform.anchoredPosition);
        }

        private Sequence BuildStatChangeSequence(IReadOnlyList<MergeStatChange> changes)
        {
            var sequence = DOTween.Sequence().SetLink(gameObject);
            var time = _statChangeStartDelay;

            foreach (var change in changes)
            {
                if (_statRows.TryGetValue(change.Kind, out var row) == false)
                    continue;

                time = InsertStatChange(sequence, time, change, row) + _statChangeGap;
            }

            return sequence;
        }

        private float InsertStatChange(Sequence sequence, float start, MergeStatChange change, StatRowWidgets row)
        {
            var label = row.ChangeLabel;
            var restPosition = ResolveChangeLabelPosition(change, row);
            var fadeStart = start + _percentAppearDuration + _percentHoldDuration;
            var percentEnd = fadeStart + _percentFadeDuration;
            var countEnd = percentEnd + _countDuration;

            sequence.InsertCallback(start, () => PrepareChangeLabel(change, label, restPosition));
            sequence.Insert(start, DOTween.To(() => 0f, alpha => label.alpha = alpha, 1f, _percentAppearDuration));
            sequence.Insert(start, DOTween
                .To(() => 0f, offset => label.rectTransform.anchoredPosition = restPosition + Vector2.up * offset,
                    _percentRiseDistance, percentEnd - start)
                .SetEase(Ease.OutCubic));
            sequence.Insert(fadeStart, DOTween.To(() => 1f, alpha => label.alpha = alpha, 0f, _percentFadeDuration));
            sequence.InsertCallback(percentEnd, () => ResetChangeLabel(row));
            sequence.Insert(percentEnd, DOTween
                .To(() => (float)change.From,
                    value => row.ValueLabel.text = FormatValue(change.Kind, Mathf.RoundToInt(value)),
                    change.To, _countDuration)
                .SetEase(_countEase));
            sequence.Insert(countEnd, row.Row.DOPunchScale(
                Vector3.one * _punchScale, _punchDuration, _punchVibrato, _punchElasticity));

            return countEnd + _punchDuration;
        }

        private Vector2 ResolveChangeLabelPosition(MergeStatChange change, StatRowWidgets row)
        {
            var valueRect = row.ValueLabel.rectTransform;
            var widestValue = FormatValue(change.Kind, Mathf.Max(change.From, change.To));
            var valueWidth = row.ValueLabel.GetPreferredValues(widestValue).x;
            var valueLeftEdge = valueRect.anchoredPosition.x - valueRect.rect.width * valueRect.pivot.x;

            return new Vector2(valueLeftEdge + valueWidth + _changeLabelSpacing, row.ChangeLabelRestPosition.y);
        }

        private void PrepareChangeLabel(MergeStatChange change, TextMeshProUGUI label, Vector2 restPosition)
        {
            var material = change.IsGain ? _gainMaterial : _lossMaterial;

            if (material != null)
                label.fontSharedMaterial = material;

            label.text = string.Format(PercentFormat, change.Percent);
            label.alpha = 0f;
            label.rectTransform.anchoredPosition = restPosition;
            label.gameObject.SetActive(true);
        }

        private void ResetStatRow(StatRowWidgets row)
        {
            ResetChangeLabel(row);
            row.Row.localScale = Vector3.one;
        }

        private void ResetChangeLabel(StatRowWidgets row)
        {
            row.ChangeLabel.gameObject.SetActive(false);
            row.ChangeLabel.rectTransform.anchoredPosition = row.ChangeLabelRestPosition;
        }

        private string FormatValue(MergeStatKind kind, int value) =>
            kind == MergeStatKind.Moves ? string.Format(MovesFormat, value) : value.ToString();

        private void ApplyIcon(Sprite icon)
        {
            if (_headIcon == null)
                return;

            _headIcon.sprite = icon;
            _headIcon.gameObject.SetActive(icon != null);
        }

        private void ApplyTitle(string title)
        {
            if (_titleLabel == null)
                return;

            bool hasTitle = string.IsNullOrEmpty(title) == false;

            _titleLabel.text = hasTitle ? title : string.Empty;
            _titleLabel.gameObject.SetActive(hasTitle);
        }

        private void ApplyAbilityLines(IReadOnlyList<string> abilityLines)
        {
            if (_abilityLabels == null)
                return;

            int lineCount = abilityLines?.Count ?? 0;

            if (lineCount > _abilityLabels.Length)
                Debug.LogWarning($"[{nameof(AnimalStatsPanelView)}] {lineCount} ability lines do not fit into {_abilityLabels.Length} labels - the rest is not shown.", this);

            for (int i = 0; i < _abilityLabels.Length; i++)
            {
                var label = _abilityLabels[i];

                if (label == null)
                    continue;

                bool hasLine = i < lineCount;

                if (hasLine)
                    label.text = abilityLines[i];

                label.gameObject.SetActive(hasLine);
            }

            int visibleLineCount = Mathf.Min(lineCount, _abilityLabels.Length);

            if (_abilitiesDivider != null)
                _abilitiesDivider.SetActive(visibleLineCount > 0);

            ApplyHeight(visibleLineCount);
        }

        private void ApplyHeight(int visibleLineCount)
        {
            if (_abilityLineHeight <= 0f)
                return;

            _abilityLinesHeight = visibleLineCount * _abilityLineHeight;

            _rectTransform.sizeDelta = new Vector2(
                _rectTransform.sizeDelta.x,
                _baseHeight + _abilityLinesHeight);
        }

        private void UpdatePosition()
        {
            if (_camera == null || _canvasRectTransform == null)
                return;

            var screenPosition = _camera.WorldToScreenPoint(_anchor.position + _worldOffset);

            if (screenPosition.z < 0f)
                return;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _canvasRectTransform, screenPosition, null, out var localPoint) == false)
                return;

            _rectTransform.anchoredPosition = ResolvePanelPosition(localPoint);
        }

        protected virtual bool ShouldAvoidCoveringAnchor => true;

        private Vector2 ScreenOffsetKeepingTopEdge =>
            new Vector2(_screenOffset.x, _screenOffset.y - _abilityLinesHeight * 0.5f);

        private Vector2 ResolvePanelPosition(Vector2 anchorPoint)
        {
            var offset = ScreenOffsetKeepingTopEdge;
            var preferred = ClampInsideCanvas(anchorPoint + offset);

            if (ShouldAvoidCoveringAnchor == false)
                return preferred;

            if (CoversAnchor(preferred, anchorPoint) == false)
                return preferred;

            var mirrored = ClampInsideCanvas(anchorPoint + new Vector2(-offset.x, offset.y));

            return CoversAnchor(mirrored, anchorPoint) ? preferred : mirrored;
        }

        private bool CoversAnchor(Vector2 position, Vector2 anchorPoint) =>
            Mathf.Abs(position.x - anchorPoint.x) < _rectTransform.rect.width * 0.5f;

        private Vector2 ClampInsideCanvas(Vector2 position)
        {
            var canvasHalfWidth = _canvasRectTransform.rect.width * 0.5f;
            var canvasHalfHeight = _canvasRectTransform.rect.height * 0.5f;
            var panelHalfWidth = _rectTransform.rect.width * 0.5f;
            var panelHalfHeight = _rectTransform.rect.height * 0.5f;

            var minX = -canvasHalfWidth + panelHalfWidth + _edgePadding;
            var maxX = canvasHalfWidth - panelHalfWidth - _edgePadding;
            var minY = -canvasHalfHeight + panelHalfHeight + _edgePadding;
            var maxY = canvasHalfHeight - panelHalfHeight - _edgePadding;

            return new Vector2(
                Mathf.Clamp(position.x, minX, maxX),
                Mathf.Clamp(position.y, minY, maxY));
        }

        private void AttachToCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogError($"[{nameof(AnimalStatsPanelView)}] Canvas is not injected - the panel cannot be positioned.", this);
                return;
            }

            _canvasRectTransform = _canvas.transform as RectTransform;

            if (_rectTransform.parent != _canvas.transform)
                _rectTransform.SetParent(_canvas.transform, false);

            _rectTransform.SetAsLastSibling();
        }

        private void KillStatSequence()
        {
            if (_statSequence == null)
                return;

            var sequence = _statSequence;
            _statSequence = null;
            sequence.Kill();
        }

        private void KillSequence()
        {
            if (_sequence == null)
                return;

            _sequence.Kill();
            _sequence = null;
        }

        private readonly struct StatRowWidgets
        {
            public StatRowWidgets(RectTransform row, TextMeshProUGUI valueLabel, TextMeshProUGUI changeLabel,
                Vector2 changeLabelRestPosition)
            {
                Row = row;
                ValueLabel = valueLabel;
                ChangeLabel = changeLabel;
                ChangeLabelRestPosition = changeLabelRestPosition;
            }

            public RectTransform Row { get; }
            public TextMeshProUGUI ValueLabel { get; }
            public TextMeshProUGUI ChangeLabel { get; }
            public Vector2 ChangeLabelRestPosition { get; }
        }
    }
}
