using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Health
{
    public class HorizontalHealthBarLayout : HealthBarLayout
    {
        [SerializeField] private RectTransform _bar;
        [SerializeField] private RectTransform _icon;
        [SerializeField, Min(1f)] private float _widthPerGridCell = 100f;
        [SerializeField, Min(1f)] private float _barHeight = 12f;
        [SerializeField] private Vector2 _iconSize = new Vector2(23f, 30f);
        [SerializeField, Min(1)] private int _segmentsPerCell = 10;

        public override int Apply(Vector3 ownerScale, UnitSize footprint)
        {
            var cells = Mathf.Max(1, footprint.Width);

            _bar.sizeDelta = CompensateOwnerScale(new Vector2(_widthPerGridCell * cells, _barHeight), ownerScale);

            if (_icon != null)
                _icon.sizeDelta = CompensateOwnerScale(_iconSize, ownerScale);

            return _segmentsPerCell * cells;
        }
    }
}
