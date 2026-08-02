#if UNITY_EDITOR
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public readonly struct DerivedGeometry
    {
        private DerivedGeometry(
            Vector3 colliderCenter,
            float colliderRadius,
            Vector3 attackPointLocalPosition,
            float healthBarHeightOffset,
            Bounds sourceBounds)
        {
            ColliderCenter = colliderCenter;
            ColliderRadius = colliderRadius;
            AttackPointLocalPosition = attackPointLocalPosition;
            HealthBarHeightOffset = healthBarHeightOffset;
            SourceBounds = sourceBounds;
        }

        public Vector3 ColliderCenter { get; }
        public float ColliderRadius { get; }
        public Vector3 AttackPointLocalPosition { get; }
        public float HealthBarHeightOffset { get; }
        public Bounds SourceBounds { get; }

        public static DerivedGeometry Calculate(Bounds rootLocalBounds, UnitSize footprint, Vector3 rootScale)
        {
            var planarScale = Mathf.Max(0.0001f, Mathf.Max(Mathf.Abs(rootScale.x), Mathf.Abs(rootScale.z)));

            var footprintHalfDiagonalInCells =
                0.5f * Mathf.Sqrt(footprint.Width * footprint.Width + footprint.Height * footprint.Height);

            var footprintRadius = footprintHalfDiagonalInCells / planarScale;
            var meshHorizontalExtent = Mathf.Max(rootLocalBounds.extents.x, rootLocalBounds.extents.z);
            var radius = Mathf.Min(footprintRadius, meshHorizontalExtent);

            var center = new Vector3(0f, rootLocalBounds.center.y, 0f);
            var attackPoint = new Vector3(0f, rootLocalBounds.center.y, radius);
            var healthBarOffset = rootLocalBounds.max.y * Mathf.Abs(rootScale.y);

            return new DerivedGeometry(center, radius, attackPoint, healthBarOffset, rootLocalBounds);
        }

        public string Describe(string prefabName) =>
            $"{prefabName}: derived collider r={ColliderRadius:0.###} c={ColliderCenter}, " +
            $"attackPoint={AttackPointLocalPosition}, healthBar={HealthBarHeightOffset:0.###} " +
            $"(mesh bounds center={SourceBounds.center} size={SourceBounds.size})";
    }
}
#endif
