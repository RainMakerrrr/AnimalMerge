#if UNITY_EDITOR
using UnityEngine;

namespace Code.Editor.AnimalPrefabBuilder
{
    public static class ModelBoundsCalculator
    {
        public static bool TryCalculateRootLocalBounds(GameObject root, out Bounds bounds)
        {
            bounds = new Bounds();
            if (root == null) return false;

            var rootTransform = root.transform;
            var hasAny = false;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!(renderer is MeshRenderer) && !(renderer is SkinnedMeshRenderer)) continue;

                var worldBounds = renderer.bounds;
                var min = worldBounds.min;
                var max = worldBounds.max;

                for (var corner = 0; corner < 8; corner++)
                {
                    var point = new Vector3(
                        (corner & 1) == 0 ? min.x : max.x,
                        (corner & 2) == 0 ? min.y : max.y,
                        (corner & 4) == 0 ? min.z : max.z);

                    var local = rootTransform.InverseTransformPoint(point);

                    if (!hasAny)
                    {
                        bounds = new Bounds(local, Vector3.zero);
                        hasAny = true;
                        continue;
                    }

                    bounds.Encapsulate(local);
                }
            }

            return hasAny && bounds.size != Vector3.zero;
        }
    }
}
#endif
