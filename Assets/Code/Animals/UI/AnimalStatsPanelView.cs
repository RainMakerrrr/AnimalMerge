using System.Collections.Generic;
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
        [SerializeField] private Image _headIcon;
        [SerializeField] private TextMeshProUGUI _titleLabel;
        [SerializeField] private TextMeshProUGUI _attackLabel;
        [SerializeField] private TextMeshProUGUI _healthLabel;
        [SerializeField] private TextMeshProUGUI[] _abilityLabels;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private Vector3 _worldOffset = new Vector3(0f, 0.6f, 0f);
        [SerializeField] private Vector2 _screenOffset = new Vector2(-220f, 60f);
        [SerializeField] private float _edgePadding = 12f;
        [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private float _appearScale = 0.85f;
        [SerializeField] private float _abilityLineHeight;

        private RectTransform _rectTransform;
        private RectTransform _canvasRectTransform;
        private Canvas _canvas;
        private Camera _camera;
        private Transform _anchor;
        private Sequence _sequence;
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

        private void OnDestroy() => KillSequence();

        public void Show(Transform anchor, Sprite icon, int attack, int health, string title,
            IReadOnlyList<string> abilityLines)
        {
            if (anchor == null)
                return;

            _anchor = anchor;

            ApplyIcon(icon);
            ApplyTitle(title);
            ApplyAbilityLines(abilityLines);
            UpdateStats(attack, health);
            UpdatePosition();

            KillSequence();

            _rectTransform.localScale = Vector3.one * _appearScale;

            _sequence = DOTween.Sequence();
            _sequence.Join(_canvasGroup.DOFade(1f, _fadeDuration));
            _sequence.Join(_rectTransform.DOScale(1f, _fadeDuration).SetEase(Ease.OutBack));
        }

        public void UpdateAbilityLines(IReadOnlyList<string> abilityLines) => ApplyAbilityLines(abilityLines);

        public void UpdateStats(int attack, int health)
        {
            if (_attackLabel != null)
                _attackLabel.text = attack.ToString();

            if (_healthLabel != null)
                _healthLabel.text = health.ToString();
        }

        public void Hide()
        {
            _anchor = null;

            KillSequence();

            if (_canvasGroup == null)
                return;

            _sequence = DOTween.Sequence();
            _sequence.Join(_canvasGroup.DOFade(0f, _fadeDuration));
        }

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

            ApplyHeight(Mathf.Min(lineCount, _abilityLabels.Length));
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

        private void KillSequence()
        {
            if (_sequence == null)
                return;

            _sequence.Kill();
            _sequence = null;
        }
    }
}
