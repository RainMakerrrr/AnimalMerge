using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Code.Animals;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Pathfinding;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Code.Tests.PlayModeTests.GridPathfinding
{
    /// <summary>
    /// End-to-end Play Mode tests for pathfinding and movement system
    /// Tests the full integration in Unity runtime with async operations
    /// Uses mocks like Editor tests, but runs in Play Mode to verify runtime behavior
    /// </summary>
    [TestFixture]
    public class EndToEndMovementTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;
        private PathfindingService _pathfindingService;

        [SetUp]
        public void Setup()
        {
            // Grid will be created per-test with appropriate size
            // Each test creates its own grid based on scenario
        }

        /// <summary>
        /// End-to-end test: Elephant (2×2) finds path to attack position
        /// Tests full integration: GetPossibleAttackPositions → PathfindingService.FindPath
        /// </summary>
        [UnityTest]
        public IEnumerator ElephantFindsPathToAttackPosition()
        {
            // Arrange
            var scenario = PlayModeScenarioBuilder.ElephantVsElephant();

            // Create grid with scenario-specific size
            _gridManager = PlayModeTestHelper.CreateMockGrid(
                scenario.GridSize.x,
                scenario.GridSize.y);
            _calculator = new TargetPositionCalculator(_gridManager);
            _pathfindingService = new PathfindingService(_gridManager);

            var attackerPos = scenario.AttackingUnit.AnchorPosition;
            var targetCells = scenario.TargetUnit.OccupiedCells;

            // Setup target as occupied
            PlayModeTestHelper.SetupOccupiedCells(_gridManager, targetCells);

            var currentNode = _gridManager.GetCell(attackerPos);

            // Create mock target
            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            target.Transformable.Returns(targetTransformable);

            var targetCellsList = targetCells
                .Select(pos => _gridManager.GetCell(pos))
                .ToList();
            targetTransformable.GetOccupiedCells().Returns(targetCellsList);

            // Act - find attack positions
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Wait one frame to simulate Unity runtime processing
            yield return null;

            // Assert - verify positions
            positions.Should().NotBeEmpty("should find valid attack positions");

            // Expected position (3,6) should be first (highest priority)
            positions[0].Should().Be(new Vector2Int(3, 6),
                "position (3,6) has minimum deltaX and should be prioritized");

            // Regression check: position (3,7) should NOT be in results
            positions.Should().NotContain(new Vector2Int(3, 7),
                "CRITICAL: position (3,7) causes overlap and must be filtered");

            // Now test PathfindingService integration
            var pathResult = _pathfindingService.FindPath(
                attackerPos,
                positions[0], // Best attack position
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            yield return null;

            // Verify path was found
            pathResult.Success.Should().BeTrue("pathfinding should find route to attack position");
            pathResult.Path.Should().NotBeEmpty("path should contain waypoints");
            pathResult.Path[^1].Should().Be(positions[0], "path should lead to attack position");
        }

        /// <summary>
        /// End-to-end test: Cheetah (1×2) navigates around obstacles
        /// Tests pathfinding with blocked cells and verifies alternative routes
        /// </summary>
        [UnityTest]
        public IEnumerator CheetahNavigatesAroundObstacles()
        {
            // Arrange
            var scenario = PlayModeScenarioBuilder.CheetahVsSmallEnemy();

            // Create grid with scenario-specific size
            _gridManager = PlayModeTestHelper.CreateMockGrid(
                scenario.GridSize.x,
                scenario.GridSize.y);
            _calculator = new TargetPositionCalculator(_gridManager);
            _pathfindingService = new PathfindingService(_gridManager);

            var attackerPos = scenario.AttackingUnit.AnchorPosition;
            var targetPos = scenario.TargetUnit.AnchorPosition;

            // Create obstacle wall blocking direct path
            var obstacles = new List<Vector2Int>
            {
                new Vector2Int(2, 3),
                new Vector2Int(2, 4),
                new Vector2Int(2, 5)
            };
            PlayModeTestHelper.SetupOccupiedCells(_gridManager, obstacles.ToArray());

            // Setup target
            PlayModeTestHelper.SetupOccupiedCells(_gridManager, scenario.TargetUnit.OccupiedCells);

            var currentNode = _gridManager.GetCell(attackerPos);

            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            target.Transformable.Returns(targetTransformable);

            var targetCell = _gridManager.GetCell(targetPos);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            // Act - find attack positions
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            yield return null;

            // Assert - should find positions despite obstacle
            positions.Should().NotBeEmpty("should find positions even with obstacles");

            // None of the positions should be on blocked cells
            foreach (var pos in positions)
            {
                obstacles.Should().NotContain(pos,
                    $"attack position {pos} should not be on obstacle");
            }

            // Test pathfinding - should find alternative route around obstacle
            var pathResult = _pathfindingService.FindPath(
                attackerPos,
                positions[0],
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            yield return null;

            // Verify path avoids obstacles
            pathResult.Success.Should().BeTrue("should find path around obstacles");

            if (pathResult.Success)
            {
                foreach (var waypoint in pathResult.Path)
                {
                    obstacles.Should().NotContain(waypoint,
                        $"path waypoint {waypoint} should not go through obstacle");
                }
            }
        }

        /// <summary>
        /// End-to-end test: Verify IsCloseToTarget works correctly in runtime
        /// Tests proximity detection when unit is adjacent to target
        /// </summary>
        [UnityTest]
        public IEnumerator UnitIsCloseToTargetWhenAdjacent()
        {
            // Arrange - create grid for test
            _gridManager = PlayModeTestHelper.CreateMockGrid(10, 10);
            _calculator = new TargetPositionCalculator(_gridManager);

            // Unit at (5, 5), target at (5, 6) - adjacent vertically
            var unitPos = new Vector2Int(5, 5);
            var targetPos = new Vector2Int(5, 6);

            var currentNode = _gridManager.GetCell(unitPos);
            var targetCell = _gridManager.GetCell(targetPos);

            // Setup mock target
            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            target.Transformable.Returns(targetTransformable);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            // Mark target as occupied
            PlayModeTestHelper.SetupOccupiedCells(_gridManager, targetPos);

            yield return null;

            // Act - check if unit is close to target
            var isClose = _calculator.IsCloseToTarget(
                currentNode,
                new List<IGridCell>(), // No other occupied cells
                target);

            // Assert
            isClose.Should().BeTrue("unit at (5,5) should be close to target at (5,6)");

            // Test with non-adjacent position - should return false
            var farNode = _gridManager.GetCell(1, 1);
            var isCloseWhenFar = _calculator.IsCloseToTarget(
                farNode,
                new List<IGridCell>(),
                target);

            yield return null;

            isCloseWhenFar.Should().BeFalse("unit at (1,1) should NOT be close to target at (5,6)");
        }

        /// <summary>
        /// End-to-end test: Multiple units pathfinding simultaneously
        /// Tests that pathfinding calculations don't interfere with each other in runtime
        /// </summary>
        [UnityTest]
        public IEnumerator MultipleUnitsPathfindingSimultaneously()
        {
            // Arrange - create 2 different scenarios
            var scenario1 = PlayModeScenarioBuilder.ElephantVsElephant();
            var scenario2 = PlayModeScenarioBuilder.CheetahVsSmallEnemy();

            // Create grid managers with scenario-specific sizes
            _gridManager = PlayModeTestHelper.CreateMockGrid(
                scenario1.GridSize.x,
                scenario1.GridSize.y);
            _calculator = new TargetPositionCalculator(_gridManager);

            var gridManager2 = PlayModeTestHelper.CreateMockGrid(
                scenario2.GridSize.x,
                scenario2.GridSize.y);
            var calculator2 = new TargetPositionCalculator(gridManager2);

            // Setup occupancy for both scenarios
            PlayModeTestHelper.SetupOccupiedCells(_gridManager,
                scenario1.TargetUnit.OccupiedCells);
            PlayModeTestHelper.SetupOccupiedCells(gridManager2,
                scenario2.TargetUnit.OccupiedCells);

            yield return null;

            // Act - calculate positions for both units simultaneously
            var currentNode1 = _gridManager.GetCell(scenario1.AttackingUnit.AnchorPosition);
            var currentNode2 = gridManager2.GetCell(scenario2.AttackingUnit.AnchorPosition);

            var target1 = Substitute.For<ITarget>();
            var transformable1 = Substitute.For<ITransformable>();
            target1.Transformable.Returns(transformable1);

            // Prepare cells list OUTSIDE of Returns() - NSubstitute requirement
            var targetCells1 = scenario1.TargetUnit.OccupiedCells
                .Select(p => _gridManager.GetCell(p))
                .ToList();
            transformable1.GetOccupiedCells().Returns(targetCells1);

            var target2 = Substitute.For<ITarget>();
            var transformable2 = Substitute.For<ITransformable>();
            target2.Transformable.Returns(transformable2);

            // Prepare cells list OUTSIDE of Returns() - NSubstitute requirement
            var targetCells2 = scenario2.TargetUnit.OccupiedCells
                .Select(p => gridManager2.GetCell(p))
                .ToList();
            transformable2.GetOccupiedCells().Returns(targetCells2);

            var positions1 = _calculator.GetPossibleAttackPositions(
                currentNode1,
                target1,
                scenario1.AttackingUnit.Size,
                scenario1.AttackingUnit.Direction);

            var positions2 = calculator2.GetPossibleAttackPositions(
                currentNode2,
                target2,
                scenario2.AttackingUnit.Size,
                scenario2.AttackingUnit.Direction);

            yield return null;

            // Assert - both should find valid positions independently
            positions1.Should().NotBeEmpty("elephant should find attack positions");
            positions2.Should().NotBeEmpty("cheetah should find attack positions");

            // Positions should be different (different scenarios)
            positions1[0].Should().NotBe(positions2[0],
                "different units should have different attack positions");
        }

        /// <summary>
        /// Stress test: Repeated pathfinding calculations in runtime
        /// Verifies performance and memory stability
        /// </summary>
        [UnityTest]
        public IEnumerator RepeatedPathfindingMaintainsPerformance()
        {
            // Arrange - create grid for test
            _gridManager = PlayModeTestHelper.CreateMockGrid(10, 10);
            _calculator = new TargetPositionCalculator(_gridManager);

            var attackerPos = new Vector2Int(1, 1);
            var targetPos = new Vector2Int(8, 8);

            var currentNode = _gridManager.GetCell(attackerPos);
            var targetCell = _gridManager.GetCell(targetPos);

            var target = Substitute.For<ITarget>();
            var targetTransformable = Substitute.For<ITransformable>();
            target.Transformable.Returns(targetTransformable);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            PlayModeTestHelper.SetupOccupiedCells(_gridManager, targetPos);

            yield return null;

            // Act - perform 50 pathfinding calculations
            var successCount = 0;
            for (int i = 0; i < 50; i++)
            {
                var positions = _calculator.GetPossibleAttackPositions(
                    currentNode,
                    target,
                    UnitSize.Small,
                    Direction.North);

                if (positions.Length > 0)
                    successCount++;

                // Yield every 10 iterations to prevent blocking
                if (i % 10 == 0)
                    yield return null;
            }

            // Assert
            successCount.Should().Be(50,
                "all 50 pathfinding calculations should succeed");
        }
    }

    /// <summary>
    /// Test helper for creating mock grids in Play Mode tests
    /// Simplified version without Editor-only dependencies
    /// </summary>
    public static class PlayModeTestHelper
    {
        public static IGridManager CreateMockGrid(int width, int height)
        {
            var gridManager = Substitute.For<IGridManager>();
            var cellsCache = new Dictionary<Vector2Int, TestGridCell>();

            gridManager.Width.Returns(width);
            gridManager.Height.Returns(height);
            gridManager.CellSize.Returns(1f);

            gridManager.IsInBounds(Arg.Any<int>(), Arg.Any<int>())
                .Returns(call => {
                    var x = call.ArgAt<int>(0);
                    var y = call.ArgAt<int>(1);
                    return x >= 0 && x < width && y >= 0 && y < height;
                });

            gridManager.IsInBounds(Arg.Any<Vector2Int>())
                .Returns(call => {
                    var pos = call.ArgAt<Vector2Int>(0);
                    return pos.x >= 0 && pos.x < width && pos.y >= 0 && pos.y < height;
                });

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pos = new Vector2Int(x, y);
                    cellsCache[pos] = new TestGridCell(x, y, isWalkable: true);
                }
            }

            gridManager.GetCell(Arg.Any<int>(), Arg.Any<int>())
                .Returns(call => {
                    var x = call.ArgAt<int>(0);
                    var y = call.ArgAt<int>(1);
                    var pos = new Vector2Int(x, y);
                    return cellsCache.TryGetValue(pos, out var cell) ? cell : null;
                });

            gridManager.GetCell(Arg.Any<Vector2Int>())
                .Returns(call => {
                    var pos = call.ArgAt<Vector2Int>(0);
                    return cellsCache.TryGetValue(pos, out var cell) ? cell : null;
                });

            gridManager.CanPlaceUnit(
                Arg.Any<Vector2Int>(),
                Arg.Any<UnitSize>(),
                Arg.Any<Direction>(),
                Arg.Any<bool>()
            ).Returns(call => {
                var pos = call.ArgAt<Vector2Int>(0);
                var size = call.ArgAt<UnitSize>(1);
                var direction = call.ArgAt<Direction>(2);

                var cells = GetOccupiedPositions(pos, size, direction);
                foreach (var cellPos in cells)
                {
                    if (!gridManager.IsInBounds(cellPos))
                        return false;

                    var cell = gridManager.GetCell(cellPos) as TestGridCell;
                    if (cell == null || !cell.IsWalkable)
                        return false;
                }
                return true;
            });

            // Mock GetOccupiedCells - CRITICAL for TargetPositionCalculator
            gridManager.GetOccupiedCells(
                Arg.Any<Vector2Int>(),
                Arg.Any<UnitSize>(),
                Arg.Any<Direction>()
            ).Returns(call => {
                var pos = call.ArgAt<Vector2Int>(0);
                var size = call.ArgAt<UnitSize>(1);
                var direction = call.ArgAt<Direction>(2);

                var positions = GetOccupiedPositions(pos, size, direction);
                var cells = new List<IGridCell>();

                foreach (var cellPos in positions)
                {
                    if (gridManager.IsInBounds(cellPos))
                    {
                        var cell = gridManager.GetCell(cellPos);
                        if (cell != null)
                            cells.Add(cell);
                    }
                }

                return cells;
            });

            return gridManager;
        }

        public static void SetupOccupiedCells(IGridManager grid, params Vector2Int[] positions)
        {
            foreach (var pos in positions)
            {
                var cell = grid.GetCell(pos) as TestGridCell;
                if (cell != null)
                    cell.IsWalkable = false;
            }
        }

        private static List<Vector2Int> GetOccupiedPositions(Vector2Int anchor, UnitSize size, Direction direction)
        {
            var positions = new List<Vector2Int>();

            if (size.Equals(UnitSize.Small))
            {
                positions.Add(anchor);
            }
            else if (size.Equals(UnitSize.Medium))
            {
                if (direction == Direction.North || direction == Direction.South)
                {
                    positions.Add(anchor);
                    positions.Add(anchor + new Vector2Int(0, 1));
                }
                else
                {
                    positions.Add(anchor);
                    positions.Add(anchor + new Vector2Int(1, 0));
                }
            }
            else if (size.Equals(UnitSize.Large))
            {
                positions.Add(anchor);
                positions.Add(anchor + new Vector2Int(1, 0));
                positions.Add(anchor + new Vector2Int(0, 1));
                positions.Add(anchor + new Vector2Int(1, 1));
            }

            return positions;
        }

        public class TestGridCell : IGridCell
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Vector2Int GridPosition => new Vector2Int(X, Y);
            public bool IsWalkable { get; set; }
            public Vector3 WorldPosition { get; set; }

            public float GCost { get; set; }
            public float HCost { get; set; }
            public float FCost => GCost + HCost;

            private IGridCell _parent;
            public IGridCell Parent
            {
                get => _parent;
                set => _parent = value;
            }

            public TestGridCell(int x, int y, bool isWalkable = true)
            {
                X = x;
                Y = y;
                IsWalkable = isWalkable;
                WorldPosition = new Vector3(x, 0, y);
                GCost = float.MaxValue;
                HCost = 0;
            }

            public void Reset()
            {
                GCost = float.MaxValue;
                HCost = 0;
                Parent = null;
            }

            public override string ToString() => $"TestCell({X},{Y})";
            public override int GetHashCode() => (X * 397) ^ Y;
            public override bool Equals(object obj) =>
                obj is TestGridCell other && other.X == X && other.Y == Y;
        }
    }

    /// <summary>
    /// Scenario builder for Play Mode tests
    /// </summary>
    public static class PlayModeScenarioBuilder
    {
        public static TestScenario ElephantVsElephant()
        {
            return new TestScenario
            {
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(3, 0),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(3, 0), new Vector2Int(4, 0),
                        new Vector2Int(3, 1), new Vector2Int(4, 1)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(3, 8),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(3, 8), new Vector2Int(4, 8),
                        new Vector2Int(3, 9), new Vector2Int(4, 9)
                    }
                }
            };
        }

        public static TestScenario CheetahVsSmallEnemy()
        {
            return new TestScenario
            {
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(2, 1),
                    Size = UnitSize.Medium,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(2, 1), new Vector2Int(2, 2)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(2, 6),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(2, 6) }
                }
            };
        }

        public class TestScenario
        {
            public Vector2Int GridSize { get; set; }
            public UnitSetup AttackingUnit { get; set; }
            public UnitSetup TargetUnit { get; set; }
            public List<Vector2Int> BlockedCells { get; set; } = new List<Vector2Int>();
        }

        public class UnitSetup
        {
            public Vector2Int AnchorPosition { get; set; }
            public UnitSize Size { get; set; }
            public Direction Direction { get; set; }
            public Vector2Int[] OccupiedCells { get; set; }
        }
    }
}
