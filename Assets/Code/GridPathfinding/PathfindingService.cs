using System.Collections.Generic;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Core A* pathfinding implementation with direction-aware multi-tile support
    /// Uses object pooling for performance optimization
    /// </summary>
    public class PathfindingService : IPathfindingService
    {
        private readonly IGridManager _gridManager;

        // Object pooling to reduce GC allocations
        private readonly List<GridCell> _openList = new List<GridCell>(256);
        private readonly HashSet<GridCell> _closedSet = new HashSet<GridCell>();
        private readonly List<GridCell> _neighbors = new List<GridCell>(4);

        public PathfindingService(IGridManager gridManager)
        {
            _gridManager = gridManager;
        }

        public PathResult FindPath(PathRequest request)
        {
            return FindPath(
                request.StartPosition,
                request.TargetPosition,
                request.UnitSize,
                request.Direction,
                request.IgnoreOccupied,
                request.RequestingUnit);
        }

        public PathResult FindPath(
            Vector2Int startPosition,
            Vector2Int targetPosition,
            UnitSize unitSize,
            Direction direction,
            bool ignoreOccupied = false,
            object requestingUnit = null)
        {
            // Validate target position
            if (!_gridManager.CanPlaceUnit(targetPosition, unitSize, direction, ignoreOccupied))
            {
                return PathResult.CreateFailure($"Target position {targetPosition} cannot fit unit of size {unitSize}");
            }

            var startCell = _gridManager.GetCell(startPosition);
            var targetCell = _gridManager.GetCell(targetPosition);

            if (startCell == null)
            {
                return PathResult.CreateFailure($"Start position {startPosition} is out of bounds");
            }

            if (targetCell == null)
            {
                return PathResult.CreateFailure($"Target position {targetPosition} is out of bounds");
            }

            // Reset all cells for new pathfinding
            ResetGrid();

            // Initialize A* algorithm
            _openList.Clear();
            _closedSet.Clear();

            startCell.GCost = 0;
            startCell.HCost = CalculateHeuristic(startPosition, targetPosition);
            _openList.Add(startCell);

            while (_openList.Count > 0)
            {
                // Get cell with lowest FCost
                var currentCell = GetLowestFCostCell();

                if (currentCell.X == targetPosition.x && currentCell.Y == targetPosition.y)
                {
                    // Path found
                    return PathResult.CreateSuccess(ReconstructPath(currentCell));
                }

                _openList.Remove(currentCell);
                _closedSet.Add(currentCell);

                // Process neighbors
                var neighbors = GetValidNeighbors(currentCell, unitSize, direction, ignoreOccupied);

                foreach (var neighbor in neighbors)
                {
                    if (_closedSet.Contains(neighbor))
                        continue;

                    float tentativeGCost = currentCell.GCost + 1; // Grid movement cost is 1

                    if (tentativeGCost < neighbor.GCost)
                    {
                        // Found a better path to this neighbor
                        neighbor.Parent = currentCell;
                        neighbor.GCost = tentativeGCost;
                        neighbor.HCost = CalculateHeuristic(
                            new Vector2Int(neighbor.X, neighbor.Y),
                            targetPosition);

                        if (!_openList.Contains(neighbor))
                        {
                            _openList.Add(neighbor);
                        }
                    }
                }
            }

            // No path found
            return PathResult.CreateFailure($"No path found from {startPosition} to {targetPosition}");
        }

        /// <summary>
        /// Gets valid neighboring cells considering unit size and direction
        /// </summary>
        private List<GridCell> GetValidNeighbors(GridCell cell, UnitSize unitSize, Direction direction, bool ignoreOccupied)
        {
            _neighbors.Clear();

            // 4-directional movement: up, down, left, right
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1),  // Down
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0)   // Left
            };

            foreach (var dir in directions)
            {
                Vector2Int neighborPos = new Vector2Int(cell.X + dir.x, cell.Y + dir.y);

                // Check if unit can be placed at this position
                if (_gridManager.CanPlaceUnit(neighborPos, unitSize, direction, ignoreOccupied))
                {
                    var neighborCell = _gridManager.GetCell(neighborPos);
                    if (neighborCell != null)
                    {
                        _neighbors.Add(neighborCell);
                    }
                }
            }

            return _neighbors;
        }

        /// <summary>
        /// Calculates Manhattan distance heuristic
        /// </summary>
        private float CalculateHeuristic(Vector2Int from, Vector2Int to)
        {
            return Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);
        }

        /// <summary>
        /// Gets the cell with the lowest FCost from the open list
        /// </summary>
        private GridCell GetLowestFCostCell()
        {
            GridCell lowestCell = _openList[0];

            for (int i = 1; i < _openList.Count; i++)
            {
                if (_openList[i].FCost < lowestCell.FCost ||
                    (_openList[i].FCost == lowestCell.FCost && _openList[i].HCost < lowestCell.HCost))
                {
                    lowestCell = _openList[i];
                }
            }

            return lowestCell;
        }

        /// <summary>
        /// Reconstructs the path by following parent pointers
        /// </summary>
        private List<Vector2Int> ReconstructPath(GridCell endCell)
        {
            var path = new List<Vector2Int>();
            var currentCell = endCell;

            while (currentCell != null)
            {
                path.Add(new Vector2Int(currentCell.X, currentCell.Y));
                currentCell = currentCell.Parent;
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// Resets all grid cells for new pathfinding operation
        /// </summary>
        private void ResetGrid()
        {
            for (int x = 0; x < _gridManager.Width; x++)
            {
                for (int y = 0; y < _gridManager.Height; y++)
                {
                    var cell = _gridManager.GetCell(x, y);
                    cell?.Reset();
                }
            }
        }
    }
}
