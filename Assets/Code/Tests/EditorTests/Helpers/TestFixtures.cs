using Code.GridPathfinding;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers
{
    /// <summary>
    /// Common test data, constants and fixtures for pathfinding tests
    /// </summary>
    public static class TestFixtures
    {
        // Default grid dimensions
        public const int DefaultGridWidth = 8;
        public const int DefaultGridHeight = 10;
        public const float DefaultCellSize = 1f;

        // Common test positions
        public static readonly Vector2Int OriginPosition = new Vector2Int(0, 0);
        public static readonly Vector2Int CenterPosition = new Vector2Int(4, 5);
        public static readonly Vector2Int TopRightCorner = new Vector2Int(7, 9);
        public static readonly Vector2Int BottomLeftCorner = new Vector2Int(0, 0);

        // Unit sizes
        public static readonly UnitSize SmallUnit = UnitSize.Small;       // 1×1
        public static readonly UnitSize MediumUnit = UnitSize.Medium;     // 1×2
        public static readonly UnitSize LargeUnit = UnitSize.Large;       // 2×2

        // Directions
        public static readonly Direction DirectionNorth = Direction.North;
        public static readonly Direction DirectionEast = Direction.East;
        public static readonly Direction DirectionSouth = Direction.South;
        public static readonly Direction DirectionWest = Direction.West;

        // Test scenarios for elephant vs elephant (TC-3.1)
        public static class ElephantVsElephant
        {
            public static readonly Vector2Int AttackerAnchor = new Vector2Int(3, 0);
            public static readonly Vector2Int[] AttackerCells = new[]
            {
                new Vector2Int(3, 0), new Vector2Int(4, 0),
                new Vector2Int(3, 1), new Vector2Int(4, 1)
            };

            public static readonly Vector2Int TargetAnchor = new Vector2Int(3, 8);
            public static readonly Vector2Int[] TargetCells = new[]
            {
                new Vector2Int(3, 8), new Vector2Int(4, 8),
                new Vector2Int(3, 9), new Vector2Int(4, 9)
            };

            public static readonly Vector2Int ExpectedFirstPosition = new Vector2Int(3, 6);
        }

        // Test scenarios for cheetah vs small enemy (TC-3.2)
        public static class CheetahVsSmallEnemy
        {
            public static readonly Vector2Int AttackerAnchor = new Vector2Int(2, 1);
            public static readonly Vector2Int[] AttackerCells = new[]
            {
                new Vector2Int(2, 1), new Vector2Int(2, 2)
            };

            public static readonly Vector2Int TargetAnchor = new Vector2Int(2, 6);
            public static readonly Vector2Int[] TargetCells = new[]
            {
                new Vector2Int(2, 6)
            };

            public static readonly Vector2Int ExpectedFirstPosition = new Vector2Int(2, 5);
        }

        // Performance thresholds
        public const int PerformanceIterations1000 = 1000;
        public const int PerformanceThresholdMs10 = 10;
        public const int PerformanceThresholdMs5 = 5;
        public const int PerformanceThresholdMs100 = 100;

        // Edge case positions
        public static readonly Vector2Int NegativePosition = new Vector2Int(-1, -1);
        public static readonly Vector2Int OutOfBoundsPositionX = new Vector2Int(100, 5);
        public static readonly Vector2Int OutOfBoundsPositionY = new Vector2Int(5, 100);

        /// <summary>
        /// Creates a standard test grid with default dimensions
        /// </summary>
        public static IGridManager CreateStandardGrid()
        {
            return GridTestHelper.CreateMockGrid(DefaultGridWidth, DefaultGridHeight);
        }
    }
}
