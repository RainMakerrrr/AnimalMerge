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
                var pos = call.ArgAt<Vector2Int>(0);
                return gridManager.IsInBounds(pos);
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
    }
}
