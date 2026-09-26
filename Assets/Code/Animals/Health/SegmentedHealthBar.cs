using System.Collections.Generic;
using UnityEngine;

namespace Code.Animals.Health
{
    public class SegmentedHealthBar : MonoBehaviour
    {
        [SerializeField] private RectTransform _container;
        [SerializeField] private HealthBarSegment _segmentTemplate;
        [SerializeField] private HealthBarOrientation _orientation = HealthBarOrientation.Horizontal;
        [SerializeField, Range(0f, 0.9f)] private float _gap = 0.2f;
        [SerializeField] private Vector2 _startCrossSpan = new Vector2(0f, 1f);
        [SerializeField] private Vector2 _endCrossSpan = new Vector2(0f, 1f);

        [Header("Low Health")]
        [SerializeField] private Sprite _lowFillSprite;
        [SerializeField] private Color _lowFillColor = Color.white;
        [SerializeField] private GameObject _lowHealthHighlight;

        private readonly List<HealthBarSegment> _segments = new List<HealthBarSegment>();
        private Sprite _normalFillSprite;
        private Color _normalFillColor;

        public void Build(int count)
        {
            if (_segments.Count > 0)
                return;

            _normalFillSprite = _segmentTemplate.Fill.sprite;
            _normalFillColor = _segmentTemplate.Fill.color;
            _segmentTemplate.gameObject.SetActive(false);

            for (var i = 0; i < count; i++)
            {
                var segment = Instantiate(_segmentTemplate, _container);
                segment.name = $"Segment_{i}";
                segment.gameObject.SetActive(true);
                PlaceSegment(segment.Rect, i, count);
                _segments.Add(segment);
            }

            SetLowHealth(false);
        }

        public void SetFill(float normalizedValue)
        {
            var count = _segments.Count;

            for (var i = 0; i < count; i++)
                _segments[i].Fill.fillAmount = Mathf.Clamp01(normalizedValue * count - i);
        }

        public void SetLowHealth(bool isLowHealth)
        {
            var sprite = isLowHealth && _lowFillSprite != null ? _lowFillSprite : _normalFillSprite;
            var color = isLowHealth ? _lowFillColor : _normalFillColor;

            foreach (var segment in _segments)
            {
                segment.Fill.sprite = sprite;
                segment.Fill.color = color;
            }

            if (_lowHealthHighlight != null)
                _lowHealthHighlight.SetActive(isLowHealth);
        }

        private void PlaceSegment(RectTransform segment, int index, int count)
        {
            var slot = 1f / count;
            var halfGap = _gap * slot * 0.5f;
            var alongMin = index * slot + halfGap;
            var alongMax = (index + 1) * slot - halfGap;
            var cross = Vector2.Lerp(_startCrossSpan, _endCrossSpan, (index + 0.5f) * slot);

            if (_orientation == HealthBarOrientation.Vertical)
            {
                segment.anchorMin = new Vector2(cross.x, alongMin);
                segment.anchorMax = new Vector2(cross.y, alongMax);
            }
            else
            {
                segment.anchorMin = new Vector2(alongMin, cross.x);
                segment.anchorMax = new Vector2(alongMax, cross.y);
            }

            segment.offsetMin = Vector2.zero;
            segment.offsetMax = Vector2.zero;
        }
    }
}
