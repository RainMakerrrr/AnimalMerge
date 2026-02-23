using System.Collections.Generic;
using UnityEngine;
using Zenject;

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
        private readonly List<IGridCell> _openList = new List<IGridCell>(256);
        private readonly HashSet<IGridCell> _closedSet = new HashSet<IGridCell>();
        private readonly List<IGridCell> _neighbors = new List<IGridCell>(4);

        public PathfindingService([Inject(Id = GridIdentifier.GameGrid)]IGridManager gridManager)
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

                    // Calculate movement cost: 1 for orthogonal, sqrt(2) for diagonal
                    int dx = Mathf.Abs(neighbor.X - currentCell.X);
                    int dy = Mathf.Abs(neighbor.Y - currentCell.Y);
                    bool isDiagonal = (dx == 1 && dy == 1);
                    float movementCost = isDiagonal ? 1.414f : 1f;

                    float tentativeGCost = currentCell.GCost + movementCost;

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
        private List<IGridCell> GetValidNeighbors(IGridCell cell, UnitSize unitSize, Direction direction, bool ignoreOccupied)
        {
            _neighbors.Clear();

            // 8-directional movement: up, down, left, right, and 4 diagonals
            Vector2Int[] directions = new Vector2Int[]
            {
                new Vector2Int(0, 1),   // Up
                new Vector2Int(0, -1),  // Down
                new Vector2Int(1, 0),   // Right
                new Vector2Int(-1, 0),  // Left
                new Vector2Int(1, 1),   // Up-Right
                new Vector2Int(1, -1),  // Down-Right
                new Vector2Int(-1, 1),  // Up-Left
                new Vector2Int(-1, -1)  // Down-Left
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
        /// Calculates Octile distance heuristic (for 8-directional movement)
        /// </summary>
        private float CalculateHeuristic(Vector2Int from, Vector2Int to)
        {
            int dx = Mathf.Abs(from.x - to.x);
            int dy = Mathf.Abs(from.y - to.y);

            // Octile distance: D * (dx + dy) + (D2 - 2 * D) * min(dx, dy)
            // where D = 1 (orthogonal cost), D2 = 1.414 (diagonal cost)
            float D = 1f;
            float D2 = 1.414f;
            return D * (dx + dy) + (D2 - 2 * D) * Mathf.Min(dx, dy);
        }

        /// <summary>
        /// Gets the cell with the lowest FCost from the open list
        /// </summary>
        private IGridCell GetLowestFCostCell()
        {
            var lowestCell = _openList[0];

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
        private List<Vector2Int> ReconstructPath(IGridCell endCell)
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
