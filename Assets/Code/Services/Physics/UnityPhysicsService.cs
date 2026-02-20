using UnityEngine;

namespace Code.Services.Physics
{
    /// <summary>
    /// Production implementation of IPhysicsService that wraps Unity's Physics API.
    /// This is the default implementation used in runtime.
    /// </summary>
    public class UnityPhysicsService : IPhysicsService
    {
        /// <inheritdoc />
        public int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, LayerMask layerMask)
        {
            return UnityEngine.Physics.OverlapCapsuleNonAlloc(point0, point1, radius, results, layerMask);
        }

        /// <inheritdoc />
        public int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, LayerMask layerMask)
        {
            return UnityEngine.Physics.OverlapSphereNonAlloc(position, radius, results, layerMask);
        }
    }
}
