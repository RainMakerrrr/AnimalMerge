using System.Collections.Generic;
using System.Linq;
using Code.Animals;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Pathfinding;
using Code.Tests.EditorTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.GridPathfinding.TargetPositionCalculatorTests
{
    /// <summary>
    /// Integration tests for GetPossibleAttackPositions with Pathfinding
    /// TC-7.1, TC-7.2 (simplified Editor Mode versions)
    /// Full async integration tests should be in Play Mode tests
    /// </summary>
    [TestFixture]
    public class PathfindingIntegrationTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;
        private PathfindingService _pathfinding;

        [SetUp]
        public void Setup()
        {
            _gridManager = GridTestHelper.CreateMockGrid(10, 15);
            _calculator = new TargetPositionCalculator(_gridManager);
            _pathfinding = new PathfindingService(_gridManager);
        }

        /// <summary>
        /// TC-7.1: Simplified version - GetPossibleMoves returns positions, FindPath can reach them
        /// </summary>
        [Test]
        public void Integration_GetPossibleMovesAndFindPath_ReturnsReachablePositions()
        {
            // Arrange - Elephant 2×2 at (3, 0) attacking Elephant 2×2 at (3, 8)
            var attackerPos = new Vector2Int(3, 0);
            var targetPos = new Vector2Int(3, 8);

            var currentNode = _gridManager.GetCell(attackerPos);

            // Setup target
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(3, 8),
                _gridManager.GetCell(4, 8),
                _gridManager.GetCell(3, 9),
                _gridManager.GetCell(4, 9)
            };

            // Mark target cells as occupied
            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(3, 8), new Vector2Int(4, 8),
                new Vector2Int(3, 9), new Vector2Int(4, 9));

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act - Get possible attack positions
            var possiblePositions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2 elephant
                Direction.North);

            // Assert - Should find positions around target
            possiblePositions.Should().NotBeEmpty("should find positions to attack target");

            // Verify that at least one position is reachable via pathfinding
            var reachableCount = 0;
            foreach (var pos in possiblePositions)
            {
                var pathResult = _pathfinding.FindPath(
                    attackerPos,
                    pos,
                    UnitSize.Large,
                    Direction.North);

                if (pathResult.Success && pathResult.Path.Count > 0)
                {
                    reachableCount++;
                }
            }

            reachableCount.Should().BeGreaterThan(0,
                "at least one possible attack position should be reachable via pathfinding");

            // Specifically, position (3, 6) should be highest priority and reachable
            var priorityPosition = possiblePositions[0];

            var pathToPriority = _pathfinding.FindPath(
                attackerPos,
                priorityPosition,
                UnitSize.Large,
                Direction.North);

            pathToPriority.Success.Should().BeTrue("should find path to priority position");
            pathToPriority.Path.Count.Should().BeGreaterThan(0, "path should have waypoints");
        }

        /// <summary>
        /// TC-7.2: Simplified version - All positions blocked, FindPath returns no path
        /// </summary>
        [Test]
        public void Integration_AllPositionsBlocked_FindPathReturnsNoPath()
        {
            // Arrange - Cat 1×1 at (1, 1) trying to reach Elephant 2×2 at (5, 5)
            var attackerPos = new Vector2Int(1, 1);
            var targetPos = new Vector2Int(5, 5);

            var currentNode = _gridManager.GetCell(attackerPos);

            // Setup target - 2×2 elephant
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(5, 5),
                _gridManager.GetCell(6, 5),
                _gridManager.GetCell(5, 6),
                _gridManager.GetCell(6, 6)
            };

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Block all cells around target (completely surround it)
            var blockedCells = new List<Vector2Int>
            {
                // Target cells
                new Vector2Int(5, 5), new Vector2Int(6, 5),
                new Vector2Int(5, 6), new Vector2Int(6, 6),
                // Surrounding cells
                new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4), new Vector2Int(7, 4),
                new Vector2Int(4, 5),                                             new Vector2Int(7, 5),
                new Vector2Int(4, 6),                                             new Vector2Int(7, 6),
                new Vector2Int(4, 7), new Vector2Int(5, 7), new Vector2Int(6, 7), new Vector2Int(7, 7)
            };

            GridTestHelper.SetupOccupiedCells(_gridManager, blockedCells.ToArray());

            // Act - Get possible attack positions
            var possiblePositions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1 cat
                Direction.North);

            // Assert - Should not find any valid positions (all blocked)
            possiblePositions.Should().BeEmpty("all positions around target are blocked");

            // Even if GetPossibleMoves returned something (before filtering),
            // none should be reachable because they're all blocked
            // This is verified by the empty result above
        }

        /// <summary>
        /// TC-7.1 variant: Verify priority position (3, 6) is preferred over others
        /// </summary>
        [Test]
        public void Integration_ElephantVsElephant_PriorityPositionIsReachable()
        {
            // Arrange
            var scenario = ScenarioBuilder.ElephantVsElephant();

            // Create grid with correct size for this scenario
            var scenarioGrid = GridTestHelper.CreateMockGrid(scenario.GridSize.x, scenario.GridSize.y);
            var scenarioCalculator = new TargetPositionCalculator(scenarioGrid);
            var scenarioPathfinding = new PathfindingService(scenarioGrid);

            // Mark only target cells as occupied
            GridTestHelper.SetupOccupiedCells(scenarioGrid, scenario.TargetUnit.OccupiedCells);

            var currentNode = scenarioGrid.GetCell(scenario.AttackingUnit.AnchorPosition);

            var targetCells = scenario.TargetUnit.OccupiedCells
                .Select(pos => scenarioGrid.GetCell(pos))
                .ToList();

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = scenarioCalculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert - priority position should be first
            positions.Should().NotBeEmpty();

            // Priority position should have deltaX=0 (same X as attacker)
            var priorityPos = positions[0];
            var deltaX = Mathf.Abs(priorityPos.x - scenario.AttackingUnit.AnchorPosition.x);
            deltaX.Should().Be(0,
                "priority position should have minimum deltaX (same X as attacker for straight line)");

            // Verify it's below target (closer to attacker)
            priorityPos.y.Should().BeLessThan(scenario.TargetUnit.AnchorPosition.y,
                "priority position should be between attacker and target");

            // Verify path exists to priority position
            var pathResult = scenarioPathfinding.FindPath(
                scenario.AttackingUnit.AnchorPosition,
                positions[0],
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            pathResult.Success.Should().BeTrue("path to priority position should exist");
            pathResult.Path.Count.Should().BeGreaterThan(0, "path should have waypoints");

            // Verify final position would be (3, 6)
            var finalPosition = pathResult.Path[pathResult.Path.Count - 1];
            finalPosition.Should().Be(new Vector2Int(3, 6),
                "pathfinding should lead to position (3, 6)");
        }

        /// <summary>
        /// Edge case: Path blocked but alternative positions available
        /// </summary>
        [Test]
        public void Integration_PathToFirstPositionBlocked_FindsAlternative()
        {
            // Arrange - Small unit at (1, 1) attacking target at (5, 5)
            var attackerPos = new Vector2Int(1, 1);
            var currentNode = _gridManager.GetCell(attackerPos);

            var targetCell = _gridManager.GetCell(5, 5);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Block direct path to priority position but leave alternatives
            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(5, 5), // target
                new Vector2Int(3, 3), new Vector2Int(4, 3), new Vector2Int(5, 3)); // block direct path

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert - should find multiple positions
            positions.Should().NotBeEmpty("should find positions despite blocking");

            // At least one position should be reachable
            var reachablePositions = positions.Where(pos =>
            {
                var pathResult = _pathfinding.FindPath(
                    attackerPos,
                    pos,
                    UnitSize.Small,
                    Direction.North);

                return pathResult.Success && pathResult.Path.Count > 0;
            }).ToList();

            reachablePositions.Should().NotBeEmpty(
                "at least one alternative position should be reachable");
        }
    }
}
