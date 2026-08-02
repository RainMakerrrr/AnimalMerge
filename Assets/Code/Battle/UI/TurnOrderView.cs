using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Zenject;

namespace Code.Battle.UI
{
    [RequireComponent(typeof(RectTransform))]
    [RequireComponent(typeof(CanvasGroup))]
    public class TurnOrderView : MonoBehaviour, ITurnOrderView
    {
        [SerializeField] private TurnOrderEntryView _entryPrefab;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private int _maxEntries = 6;
        [SerializeField] private float _entryHeight = 150f;
        [SerializeField] private float _entrySpacing = 8f;
        [SerializeField] private float _fadeDuration = 0.15f;
        [SerializeField] private int _canvasSiblingIndex;

        private readonly List<TurnOrderEntryView> _pool = new List<TurnOrderEntryView>();
        private readonly List<TurnOrderEntryView> _active = new List<TurnOrderEntryView>();
        private readonly List<TurnOrderEntryView> _next = new List<TurnOrderEntryView>();

        private RectTransform _rectTransform;
        private Canvas _canvas;
        private Tween _fadeTween;

        [Inject]
        private void Construct(Canvas canvas) => _canvas = canvas;

        private void Awake()
        {
            _rectTransform = (RectTransform)transform;

            if (_canvasGroup == null)
                _canvasGroup = GetComponent<CanvasGroup>();

            _canvasGroup.alpha = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.interactable = false;

            AttachToCanvas();
        }

        private void OnDestroy() => KillFadeTween();

        public void Show()
        {
            KillFadeTween();

            _fadeTween = _canvasGroup.DOFade(1f, _fadeDuration);
        }

        public void Hide()
        {
            KillFadeTween();

            _fadeTween = _canvasGroup.DOFade(0f, _fadeDuration);
        }

        public void Render(IReadOnlyList<TurnOrderEntryData> entries)
        {
            if (_entryPrefab == null)
            {
                Debug.LogError($"[{nameof(TurnOrderView)}] {nameof(_entryPrefab)} is not assigned - the column stays empty.", this);
                return;
            }

            var visibleCount = Mathf.Min(entries.Count, _maxEntries);

            _next.Clear();

            for (int i = 0; i < visibleCount; i++)
                _next.Add(TakeMatchingEntry(entries[i].Id));

            for (int i = 0; i < visibleCount; i++)
            {
                if (_next[i] == null)
                    _next[i] = TakeAvailableEntry();
            }

            RetireUnusedEntries();

            for (int i = 0; i < visibleCount; i++)
                _next[i].Bind(entries[i], SlotPosition(i), _active.Contains(_next[i]));

            _active.Clear();
            _active.AddRange(_next);

            ResizeColumn(visibleCount);
        }

        private TurnOrderEntryView TakeMatchingEntry(int id)
        {
            for (int i = 0; i < _active.Count; i++)
            {
                var entry = _active[i];

                if (entry == null || entry.Id != id)
                    continue;

                if (_next.Contains(entry))
                    continue;

                return entry;
            }

            return null;
        }

        private TurnOrderEntryView TakeAvailableEntry()
        {
            for (int i = 0; i < _pool.Count; i++)
            {
                if (_active.Contains(_pool[i]) || _next.Contains(_pool[i]))
                    continue;

                return _pool[i];
            }

            var created = Instantiate(_entryPrefab, _rectTransform);

            _pool.Add(created);

            return created;
        }

        private void RetireUnusedEntries()
        {
            for (int i = 0; i < _active.Count; i++)
            {
                var entry = _active[i];

                if (entry == null || _next.Contains(entry))
                    continue;

                entry.Retire();
            }
        }

        private Vector2 SlotPosition(int index) =>
            new Vector2(0f, -index * (_entryHeight + _entrySpacing));

        private void ResizeColumn(int visibleCount)
        {
            var height = visibleCount > 0
                ? visibleCount * _entryHeight + (visibleCount - 1) * _entrySpacing
                : 0f;

            _rectTransform.sizeDelta = new Vector2(_rectTransform.sizeDelta.x, height);
        }

        private void AttachToCanvas()
        {
            if (_canvas == null)
            {
                Debug.LogError($"[{nameof(TurnOrderView)}] Canvas is not injected - the column cannot be positioned.", this);
                return;
            }

            if (_rectTransform.parent != _canvas.transform)
                _rectTransform.SetParent(_canvas.transform, false);

            _rectTransform.SetSiblingIndex(_canvasSiblingIndex);
        }

        private void KillFadeTween()
        {
            if (_fadeTween == null)
                return;

            _fadeTween.Kill();
            _fadeTween = null;
        }
    }
}
