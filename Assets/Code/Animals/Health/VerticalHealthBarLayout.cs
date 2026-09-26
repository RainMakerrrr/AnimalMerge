using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Health
{
    public class VerticalHealthBarLayout : HealthBarLayout
    {
        [SerializeField] private RectTransform _bar;
        [SerializeField, Min(1f)] private float _thickness = 24f;
        [SerializeField, Min(1f)] private float _length = 95f;
        [SerializeField, Min(1)] private int _segmentCount = 10;

        public override int Apply(Vector3 ownerScale, UnitSize footprint)
        {
            _bar.sizeDelta = CompensateOwnerScale(new Vector2(_thickness, _length), ownerScale);
            return _segmentCount;
        }
    }
}
