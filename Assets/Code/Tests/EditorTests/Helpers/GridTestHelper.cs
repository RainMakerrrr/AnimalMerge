using System.Collections.Generic;
using Code.GridPathfinding;
using NSubstitute;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers
{
    /// <summary>
    /// Helper utilities for creating mock grid and test cells
    /// </summary>
    public static class GridTestHelper
    {
        /// <summary>
        /// Creates a mock IGridManager with configurable dimensions
        /// </summary>
        public static IGridManager CreateMockGrid(int width = 8, int height = 10)
        {
            var gridManager = Substitute.For<IGridManager>();

            // Setup properties
            gridManager.Width.Returns(width);
            gridManager.Height.Returns(height);
            gridManager.CellSize.Returns(1f);

            // Setup IsInBounds
            gridManager.IsInBounds(Arg.Any<int>(), Arg.Any<int>())
                .Returns(call =>
                {
                    var x = call.ArgAt<int>(0);
                    var y = call.ArgAt<int>(1);
                    return x >= 0 && x < width && y >= 0 && y < height;
                });

            gridManager.IsInBounds(Arg.Any<Vector2Int>())
                .Returns(call =>
                {
                    var pos = call.ArgAt<Vector2Int>(0);
                    return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
                });

            // Setup GetCell - create test cells for all positions
            var cellsCache = new Dictionary<Vector2Int, TestGridCell>();

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    var cell = new TestGridCell(x, y, isWalkable: true);
                    cellsCache[pos] = cell;
                }
            }

            gridManager.GetCell(Arg.Any<int>(), Arg.Any<int>())
                .Returns(call =>
                {
                    var x = call.ArgAt<int>(0);
                    var y = call.ArgAt<int>(1);
                    var pos = new Vector2Int(x, y);
                    return cellsCache.ContainsKey(pos) ? cellsCache[pos] : null;
                });

            gridManager.GetCell(Arg.Any<Vector2Int>())
                .Returns(call =>
                {
                    var pos = call.ArgAt<Vector2Int>(0);
                    return cellsCache.ContainsKey(pos) ? cellsCache[pos] : null;
                });

            // Setup CanPlaceUnit with default behavior
            gridManager.CanPlaceUnit(
                Arg.Any<Vector2Int>(),
                Arg.Any<UnitSize>(),
                Arg.Any<Direction>(),
                Arg.Any<bool>()
            ).Returns(call =>
            {
                var anchor = call.ArgAt<Vector2Int>(0);
                var size = call.ArgAt<UnitSize>(1);
                var direction = call.ArgAt<Direction>(2);
                var ignoreOccupied = call.ArgAt<bool>(3);

                // Get all cells this unit would occupy
                var occupiedPositions = GetOccupiedPositions(anchor, size, direction);

                // Check each cell
                foreach (var cellPos in occupiedPositions)
                {
                    // Must be in bounds
                    if (!gridManager.IsInBounds(cellPos))
                        return false;

                    // Must be walkable (unless ignoreOccupied is true, we still check walkability for obstacles)
                    var cell = gridManager.GetCell(cellPos) as TestGridCell;
                    if (cell == null || !cell.IsWalkable)
                        return false;
                }

                return true;
            });

            // Setup GetOccupiedCells
            gridManager.GetOccupiedCells(
                Arg.Any<Vector2Int>(),
                Arg.Any<UnitSize>(),
                Arg.Any<Direction>()
            ).Returns(call =>
            {
                var anchor = call.ArgAt<Vector2Int>(0);
                var size = call.ArgAt<UnitSize>(1);
                var direction = call.ArgAt<Direction>(2);

                var occupiedPositions = GetOccupiedPositions(anchor, size, direction);
                var result = new List<IGridCell>();

                foreach (var pos in occupiedPositions)
                {
                    if (gridManager.IsInBounds(pos))
                    {
                        var cell = gridManager.GetCell(pos);
                        if (cell != null)
                        {
                            result.Add(cell);
                        }
                    }
                }

                return result;
            });

            return gridManager;
        }

        /// <summary>
        /// Marks specified cells as occupied (non-walkable)
        /// </summary>
        public static void SetupOccupiedCells(IGridManager grid, params Vector2Int[] positions)
        {
            foreach (var pos in positions)
            {
                var cell = grid.GetCell(pos) as TestGridCell;
                if (cell != null)
                {
                    cell.IsWalkable = false;
                }
            }
        }

        /// <summary>
        /// Marks specified cells as walkable
        /// </summary>
        public static void SetupWalkableCells(IGridManager grid, params Vector2Int[] positions)
        {
            foreach (var pos in positions)
            {
                var cell = grid.GetCell(pos) as TestGridCell;
                if (cell != null)
                {
                    cell.IsWalkable = true;
                }
            }
        }

        /// <summary>
        /// Verifies that a cell exists at the specified position
        /// </summary>
        public static bool VerifyCellExists(IGridManager grid, Vector2Int position)
        {
            var cell = grid.GetCell(position);
            return cell != null && cell.GridPosition == position;
        }

        /// <summary>
        /// Gets all positions occupied by a unit of given size and direction at anchor position
        /// </summary>
        private static List<Vector2Int> GetOccupiedPositions(Vector2Int anchor, UnitSize size, Direction direction)
        {
            var positions = new List<Vector2Int>();

            if (size.Equals(UnitSize.Small)) // 1×1
            {
                positions.Add(anchor);
            }
            else if (size.Equals(UnitSize.Medium)) // 1×2
            {
                if (direction == Direction.North || direction == Direction.South)
                {
                    positions.Add(anchor);
                    positions.Add(anchor + new Vector2Int(0, 1));
                }
                else // East or West
                {
                    positions.Add(anchor);
                    positions.Add(anchor + new Vector2Int(1, 0));
                }
            }
            else if (size.Equals(UnitSize.Large)) // 2×2
            {
                positions.Add(anchor);
                positions.Add(anchor + new Vector2Int(1, 0));
                positions.Add(anchor + new Vector2Int(0, 1));
                positions.Add(anchor + new Vector2Int(1, 1));
            }

            return positions;
        }
    }
}
