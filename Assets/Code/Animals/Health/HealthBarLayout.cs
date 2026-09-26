using Code.GridPathfinding;
using UnityEngine;

namespace Code.Animals.Health
{
    public abstract class HealthBarLayout : MonoBehaviour
    {
        public abstract int Apply(Vector3 ownerScale, UnitSize footprint);

        protected static Vector2 CompensateOwnerScale(Vector2 size, Vector3 ownerScale) =>
            new Vector2(size.x / ownerScale.x, size.y / ownerScale.y);
    }
}
