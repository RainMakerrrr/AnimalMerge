using UnityEngine;

namespace Code.Services.Physics
{
    /// <summary>
    /// Service interface for Unity physics operations.
    /// Provides testable abstraction over Unity's Physics API.
    /// </summary>
    public interface IPhysicsService
    {
        /// <summary>
        /// Check a capsule against the physics world and return all overlapping colliders.
        /// </summary>
        /// <param name="point0">The center of the sphere at the start of the capsule.</param>
        /// <param name="point1">The center of the sphere at the end of the capsule.</param>
        /// <param name="radius">The radius of the capsule.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="layerMask">A Layer mask that is used to selectively ignore colliders.</param>
        /// <returns>The number of colliders stored in the results buffer.</returns>
        int OverlapCapsuleNonAlloc(Vector3 point0, Vector3 point1, float radius, Collider[] results, LayerMask layerMask);

        /// <summary>
        /// Computes and stores colliders touching or inside the sphere.
        /// </summary>
        /// <param name="position">Center of the sphere.</param>
        /// <param name="radius">Radius of the sphere.</param>
        /// <param name="results">The buffer to store the results in.</param>
        /// <param name="layerMask">A Layer mask that is used to selectively ignore colliders.</param>
        /// <returns>The number of colliders stored in the results buffer.</returns>
        int OverlapSphereNonAlloc(Vector3 position, float radius, Collider[] results, LayerMask layerMask);
    }
}
