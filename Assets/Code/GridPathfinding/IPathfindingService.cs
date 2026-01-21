using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Interface for pathfinding operations
    /// </summary>
    public interface IPathfindingService
    {
        /// <summary>
        /// Finds a path from start to target for a unit of given size and direction
        /// </summary>
        PathResult FindPath(PathRequest request);

        /// <summary>
        /// Finds a path with individual parameters (convenience method)
        /// </summary>
        PathResult FindPath(
            Vector2Int startPosition,
            Vector2Int targetPosition,
            UnitSize unitSize,
            Direction direction,
            bool ignoreOccupied = false,
            object requestingUnit = null);
    }
}
