using System.Collections.Generic;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Interface for grid management operations
    /// </summary>
    public interface IGridManager
    {
        int Width { get; }
        int Height { get; }
        float CellSize { get; }

        /// <summary>
        /// Gets the cell at the specified grid position
        /// </summary>
        IGridCell GetCell(int x, int y);

        /// <summary>
        /// Gets the cell at the specified grid position (Vector2Int overload)
        /// </summary>
        IGridCell GetCell(Vector2Int position);

        /// <summary>
        /// Checks if the grid position is within bounds
        /// </summary>
        bool IsInBounds(int x, int y);

        /// <summary>
        /// Checks if the grid position is within bounds (Vector2Int overload)
        /// </summary>
        bool IsInBounds(Vector2Int position);

        /// <summary>
        /// Converts world position to grid coordinates
        /// </summary>
        Vector2Int WorldToGrid(Vector3 worldPosition);

        /// <summary>
        /// Converts grid coordinates to world position
        /// </summary>
        Vector3 GridToWorld(int x, int y);

        /// <summary>
        /// Converts grid coordinates to world position (Vector2Int overload)
        /// </summary>
        Vector3 GridToWorld(Vector2Int gridPosition);

        /// <summary>
        /// Converts grid coordinates to world position at cell center
        /// </summary>
        Vector3 GridToWorldCenter(int x, int y);

        /// <summary>
        /// Converts grid coordinates to world position at cell center (Vector2Int overload)
        /// </summary>
        Vector3 GridToWorldCenter(Vector2Int gridPosition);

        /// <summary>
        /// Gets the world position for a unit of given size and direction at grid position.
        /// Returns the center position of the unit, accounting for multi-cell units.
        /// </summary>
        Vector3 GetUnitWorldPosition(Vector2Int gridPosition, UnitSize size, Direction direction);

        /// <summary>
        /// Checks if a unit of given size and direction can be placed at the position
        /// </summary>
        bool CanPlaceUnit(Vector2Int position, UnitSize size, Direction direction, bool ignoreOccupied = false);

        /// <summary>
        /// Gets all cells that a unit would occupy at the given position
        /// </summary>
        List<IGridCell> GetOccupiedCells(Vector2Int position, UnitSize size, Direction direction);

        /// <summary>
        /// Gets the neighbor cells (additional cells excluding the base position cell)
        /// that a unit occupies based on its size and direction.
        /// This matches the old PathNode.GetNeighbours behavior.
        /// </summary>
        List<IGridCell> GetNeighborCells(Vector2Int position, UnitSize size, Direction direction);

        /// <summary>
        /// Marks cells as occupied by a unit
        /// </summary>
        void SetOccupied(Vector2Int position, UnitSize size, Direction direction, object unit);

        /// <summary>
        /// Clears occupancy from cells
        /// </summary>
        void ClearOccupied(Vector2Int position, UnitSize size, Direction direction);

        /// <summary>
        /// Sets the walkability of a cell
        /// </summary>
        void SetWalkable(int x, int y, bool walkable);
    }
}
