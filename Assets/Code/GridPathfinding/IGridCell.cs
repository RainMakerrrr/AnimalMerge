using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Interface for grid cells used in pathfinding
    /// Allows for testable implementations without MonoBehaviour dependencies
    /// </summary>
    public interface IGridCell
    {
        void SetColor(Color color);
        void UpdateVisual();
        /// <summary>
        /// X coordinate in grid
        /// </summary>
        int X { get; }

        /// <summary>
        /// Y coordinate in grid
        /// </summary>
        int Y { get; }

        /// <summary>
        /// Grid position as Vector2Int
        /// </summary>
        Vector2Int GridPosition { get; }

        /// <summary>
        /// Whether this cell is walkable
        /// </summary>
        bool IsWalkable { get; set; }

        /// <summary>
        /// World position (center of the cell)
        /// </summary>
        Vector3 WorldPosition { get; }

        /// <summary>
        /// A* cost from start node
        /// </summary>
        float GCost { get; set; }

        /// <summary>
        /// A* heuristic cost to target
        /// </summary>
        float HCost { get; set; }

        /// <summary>
        /// A* total cost (GCost + HCost)
        /// </summary>
        float FCost { get; }

        /// <summary>
        /// Parent cell in pathfinding
        /// </summary>
        IGridCell Parent { get; set; }

        /// <summary>
        /// Resets A* pathfinding data for reuse
        /// </summary>
        void Reset();
    }
}
