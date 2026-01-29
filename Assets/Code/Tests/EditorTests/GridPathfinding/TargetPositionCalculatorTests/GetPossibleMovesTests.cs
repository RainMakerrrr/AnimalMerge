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
    /// Integration tests for GetPossibleAttackPositions - main pathfinding logic
    /// TC-3.1, TC-3.2, TC-3.3, TC-3.4
    /// </summary>
    [TestFixture]
    public class GetPossibleMovesTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _gridManager = GridTestHelper.CreateMockGrid(8, 10);
            _calculator = new TargetPositionCalculator(_gridManager);
        }

        /// <summary>
        /// TC-3.1: Elephant 2×2 attacks Elephant 2×2 (CRITICAL test case)
        /// This is the main bug we're fixing - ensure (3,7) is NOT included
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_ElephantVsElephant_ReturnsValidPositionsWithoutConflicts()
        {
            // Arrange
            var scenario = ScenarioBuilder.ElephantVsElephant();

            // Setup target cells as occupied (blocked)
            GridTestHelper.SetupOccupiedCells(_gridManager, scenario.TargetUnit.OccupiedCells);

            var currentNode = _gridManager.GetCell(scenario.AttackingUnit.AnchorPosition);
            var target = CreateMockTarget(scenario.TargetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert
            positions.Should().NotBeEmpty("elephant should find valid attack positions");

            // Priority check: first position should be straight forward with minimum deltaX
            positions[0].Should().Be(new Vector2Int(3, 6),
                "position (3,6) has deltaX=0 and is directly forward - highest priority");

            // CRITICAL: Check that conflicting positions are NOT included
            positions.Should().NotContain(new Vector2Int(3, 7),
                "position (3,7) conflicts with enemy at (3,8) - 2×2 unit needs 2 cells forward");
            positions.Should().NotContain(new Vector2Int(4, 7),
                "position (4,7) conflicts with enemy at (4,8)");
            positions.Should().NotContain(new Vector2Int(2, 7),
                "position (2,7) conflicts with enemy at (3,8) when anchor is (2,7)");

            // Valid positions should be included
            positions.Should().Contain(new Vector2Int(3, 6),
                "straight forward position should be valid");
            positions.Should().Contain(new Vector2Int(2, 6),
                "position to the left should be valid");
            positions.Should().Contain(new Vector2Int(4, 6),
                "position to the right should be valid");

            // Verify positions are unique
            positions.ContainUniquePositions();
        }

        /// <summary>
        /// TC-3.2: Cheetah 1×2 attacks Small Enemy 1×1
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_CheetahVsSmallEnemy_ReturnsValidPositions()
        {
            // Arrange
            var scenario = ScenarioBuilder.CheetahVsSmallEnemy();

            GridTestHelper.SetupOccupiedCells(_gridManager, scenario.TargetUnit.OccupiedCells);

            var currentNode = _gridManager.GetCell(scenario.AttackingUnit.AnchorPosition);
            var target = CreateMockTarget(scenario.TargetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert
            positions.Should().NotBeEmpty("cheetah should find positions around small enemy");

            // Positions around (2,6) that are valid for 1×2 vertical unit
            positions.Should().Contain(new Vector2Int(2, 4),
                "position straight forward should be valid");

            // First position should have minimum deltaX
            var firstPos = positions[0];
            firstPos.x.Should().Be(2, "first position should have deltaX=0 (same X as current position)");

            positions.ContainUniquePositions();
        }

        /// <summary>
        /// TC-3.3: Small Unit 1×1 attacks Large Target 2×2
        /// Small unit should find many positions around large target
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_SmallUnitVsLargeTarget_FindsManyPositions()
        {
            // Arrange
            var currentNode = _gridManager.GetCell(0, 0);

            // Target: Large 2×2 unit at (3,5)
            var targetUnit = new UnitSetup
            {
                AnchorPosition = new Vector2Int(3, 5),
                Size = UnitSize.Large,
                Direction = Direction.North,
                OccupiedCells = new[]
                {
                    new Vector2Int(3, 5), new Vector2Int(4, 5),
                    new Vector2Int(3, 6), new Vector2Int(4, 6)
                }
            };

            GridTestHelper.SetupOccupiedCells(_gridManager, targetUnit.OccupiedCells);
            var target = CreateMockTarget(targetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("small unit should find positions around 2×2 target");

            // Should find positions around all 4 cells of the target (minus duplicates and occupied)
            // Expected positions include neighbors of (3,5), (4,5), (3,6), (4,6)
            positions.Should().Contain(new Vector2Int(2, 4), "left-bottom neighbor");
            positions.Should().Contain(new Vector2Int(3, 4), "center-bottom neighbor");
            positions.Should().Contain(new Vector2Int(4, 4), "center-right-bottom neighbor");
            positions.Should().Contain(new Vector2Int(5, 4), "right-bottom neighbor");

            positions.Should().Contain(new Vector2Int(2, 7), "left-top neighbor");
            positions.Should().Contain(new Vector2Int(3, 7), "center-top neighbor");
            positions.Should().Contain(new Vector2Int(4, 7), "center-right-top neighbor");
            positions.Should().Contain(new Vector2Int(5, 7), "right-top neighbor");

            // Should NOT contain occupied cells
            positions.Should().NotContain(new Vector2Int(3, 5));
            positions.Should().NotContain(new Vector2Int(4, 5));
            positions.Should().NotContain(new Vector2Int(3, 6));
            positions.Should().NotContain(new Vector2Int(4, 6));

            positions.ContainUniquePositions();
        }

        /// <summary>
        /// TC-3.4: No Valid Positions - target completely surrounded
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_TargetSurrounded_ReturnsEmptyArray()
        {
            // Arrange
            var scenario = ScenarioBuilder.NoValidPositions();

            // Mark target and all surrounding cells as occupied
            GridTestHelper.SetupOccupiedCells(_gridManager,
                scenario.TargetUnit.OccupiedCells.Concat(scenario.BlockedCells).ToArray());

            var currentNode = _gridManager.GetCell(scenario.AttackingUnit.AnchorPosition);
            var target = CreateMockTarget(scenario.TargetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert
            positions.Should().BeEmpty("no valid positions when target is completely surrounded");
        }

        /// <summary>
        /// Edge case: Target with obstacles on sides
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_ObstaclesOnSides_FiltersInvalidPositions()
        {
            // Arrange
            var scenario = ScenarioBuilder.LargeUnitWithObstacles();

            // Setup occupied cells
            GridTestHelper.SetupOccupiedCells(_gridManager,
                scenario.TargetUnit.OccupiedCells.Concat(scenario.BlockedCells).ToArray());

            var currentNode = _gridManager.GetCell(scenario.AttackingUnit.AnchorPosition);
            var target = CreateMockTarget(scenario.TargetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert
            positions.Should().NotBeEmpty("should find some valid positions despite obstacles");

            // Verify no positions overlap with blocked cells
            foreach (var blockedCell in scenario.BlockedCells)
            {
                positions.Should().NotContain(blockedCell,
                    $"should not include blocked position {blockedCell}");
            }

            positions.ContainUniquePositions();
        }

        /// <summary>
        /// Edge case: Null current node
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_NullCurrentNode_ReturnsEmptyArray()
        {
            // Arrange
            var target = CreateMockTarget(new UnitSetup
            {
                AnchorPosition = new Vector2Int(5, 5),
                Size = UnitSize.Small,
                Direction = Direction.North,
                OccupiedCells = new[] { new Vector2Int(5, 5) }
            });

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                null,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().BeEmpty("null current node should return empty array");
        }

        /// <summary>
        /// Edge case: Null target
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_NullTarget_ReturnsEmptyArray()
        {
            // Arrange
            var currentNode = _gridManager.GetCell(2, 2);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                null,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().BeEmpty("null target should return empty array");
        }

        /// <summary>
        /// Priority test: Positions sorted by deltaX ascending, then deltaY descending
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_ReturnsSortedByPriority()
        {
            // Arrange
            var currentNode = _gridManager.GetCell(3, 2);

            var targetUnit = new UnitSetup
            {
                AnchorPosition = new Vector2Int(3, 7),
                Size = UnitSize.Small,
                Direction = Direction.North,
                OccupiedCells = new[] { new Vector2Int(3, 7) }
            };

            GridTestHelper.SetupOccupiedCells(_gridManager, targetUnit.OccupiedCells);
            var target = CreateMockTarget(targetUnit);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty();

            // Verify sorting: deltaX ascending, then deltaY descending
            positions.BeSortedByPriority(new Vector2Int(3, 2),
                "positions should be sorted by deltaX ascending, then deltaY descending");
        }

        #region Helper Methods

        /// <summary>
        /// Creates a mock ITarget from UnitSetup
        /// </summary>
        private ITarget CreateMockTarget(UnitSetup unitSetup)
        {
            var target = Substitute.For<ITarget>();
            var transformable = Substitute.For<ITransformable>();

            var anchorCell = _gridManager.GetCell(unitSetup.AnchorPosition);
            transformable.CurrentPathNode.Returns(anchorCell);
            transformable.Position.Returns(anchorCell.WorldPosition);

            // Setup GetOccupiedCells to return all cells from UnitSetup
            var occupiedCells = new List<IGridCell>();
            foreach (var pos in unitSetup.OccupiedCells)
            {
                var cell = _gridManager.GetCell(pos);
                if (cell != null)
                {
                    occupiedCells.Add(cell);
                }
            }
            transformable.GetOccupiedCells().Returns(occupiedCells);

            target.Transformable.Returns(transformable);

            return target;
        }

        #endregion
    }
}
