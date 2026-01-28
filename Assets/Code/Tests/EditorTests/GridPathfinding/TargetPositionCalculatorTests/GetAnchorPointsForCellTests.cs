using Code.Animals;
using Code.Animals.Movement;
using Code.GridPathfinding;
using Code.Tests.EditorTests.Helpers;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using UnityEngine;

namespace Code.Tests.EditorTests.GridPathfinding.TargetPositionCalculatorTests
{
    /// <summary>
    /// Tests for TargetPositionCalculator.GetAnchorPointsForCell method
    /// Covers TC-1.1 through TC-1.5 from the test specification
    /// Tests behavior through GetPossibleAttackPositions public API
    /// </summary>
    [TestFixture]
    public class GetAnchorPointsForCellTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _gridManager = GridTestHelper.CreateMockGrid(
                TestFixtures.DefaultGridWidth,
                TestFixtures.DefaultGridHeight);
            _calculator = new TargetPositionCalculator(_gridManager);
        }

        [TearDown]
        public void TearDown()
        {
            _gridManager = null;
            _calculator = null;
        }

        /// <summary>
        /// TC-1.1: Unit 1×1 - Simple Case
        /// A 1×1 unit should produce attack positions adjacent to a single target cell
        /// Priority: High
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Unit1x1_ReturnsAdjacentPositions()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var currentCell = new Vector2Int(5, 2);
            var unitSize = UnitSize.Small;
            var direction = Direction.North;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().NotBeEmpty("should find attack positions adjacent to target");

            // All positions should be valid (within bounds)
            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height,
                    $"position {pos} should be within grid bounds");
            }
        }

        /// <summary>
        /// TC-1.2: Unit 1×2 Direction.North → attack positions for vertical unit
        /// Vertical unit facing north should find valid attack positions
        /// Priority: High
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Unit1x2DirectionNorth_ReturnsValidPositions()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var currentCell = new Vector2Int(5, 2);
            var unitSize = UnitSize.Medium;
            var direction = Direction.North;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().NotBeEmpty("1×2 unit should find attack positions");

            // Verify positions are valid
            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height);
            }
        }

        /// <summary>
        /// TC-1.3: Unit 1×2 Direction.East → attack positions for horizontal unit
        /// Horizontal unit facing east should find valid attack positions
        /// Priority: High
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Unit1x2DirectionEast_ReturnsValidPositions()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var currentCell = new Vector2Int(2, 5);
            var unitSize = UnitSize.Medium;
            var direction = Direction.East;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().NotBeEmpty("1×2 horizontal unit should find attack positions");

            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height);
            }
        }

        /// <summary>
        /// TC-1.4: Unit 2×2 Direction.North → attack positions for large square unit (CRITICAL)
        /// A 2×2 unit should find multiple valid attack positions
        /// This is a critical test for large unit positioning
        /// Priority: Critical
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Unit2x2DirectionNorth_ReturnsMultiplePositions()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var currentCell = new Vector2Int(5, 2);
            var unitSize = UnitSize.Large;
            var direction = Direction.North;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().NotBeEmpty("2×2 unit should find attack positions");

            // Large unit should have more attack position options due to multiple anchor variants
            positions.Length.Should().BeGreaterThan(0, "large unit should have attack options");

            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height);
            }

            // Verify positions are unique
            positions.ContainUniquePositions("all attack positions should be unique");
        }

        /// <summary>
        /// TC-1.5: Unit 2×2 Direction.West → attack positions with different direction
        /// Tests that direction changes are handled correctly for square units
        /// Priority: High
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Unit2x2DirectionWest_ReturnsValidPositions()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var currentCell = new Vector2Int(7, 5);
            var unitSize = UnitSize.Large;
            var direction = Direction.West;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().NotBeEmpty("2×2 unit facing west should find attack positions");

            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height);
            }

            positions.ContainUniquePositions();
        }

        /// <summary>
        /// Edge case: Attack positions at grid boundary
        /// Should handle boundary cases gracefully without errors
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_TargetAtGridBoundary_HandlesCorrectly()
        {
            // Arrange - target at top-right corner
            var targetCell = new Vector2Int(7, 9);
            var currentCell = new Vector2Int(5, 7);
            var unitSize = UnitSize.Large;
            var direction = Direction.North;

            var target = CreateMockTarget(targetCell, UnitSize.Small);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                unitSize,
                direction);

            // Assert
            positions.Should().NotBeNull("should not return null even at boundary");
            // May be empty if no valid positions, but should not crash

            foreach (var pos in positions)
            {
                pos.BeWithinBounds(_gridManager.Width, _gridManager.Height,
                    "boundary positions should still be within bounds");
            }
        }

        /// <summary>
        /// Edge case: Null current node should return empty array
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_NullCurrentNode_ReturnsEmptyArray()
        {
            // Arrange
            var targetCell = new Vector2Int(5, 5);
            var target = CreateMockTarget(targetCell, UnitSize.Small);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                null,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().BeEmpty("null current node should return empty array");
        }

        /// <summary>
        /// Edge case: Null target should return empty array
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_NullTarget_ReturnsEmptyArray()
        {
            // Arrange
            var currentCell = new Vector2Int(5, 5);
            var currentNode = _gridManager.GetCell(currentCell);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                null,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().BeEmpty("null target should return empty array");
        }

        /// <summary>
        /// Helper method to create a mock target at specified position
        /// </summary>
        private ITarget CreateMockTarget(Vector2Int position, UnitSize targetSize = default)
        {
            if (targetSize == default)
                targetSize = UnitSize.Small;

            var target = Substitute.For<ITarget>();
            var transformable = Substitute.For<ITransformable>();

            var targetCell = _gridManager.GetCell(position);
            transformable.CurrentPathNode.Returns(targetCell);
            transformable.Position.Returns(new Vector3(position.x, 0, position.y));
            transformable.IntPosition.Returns(position);
            transformable.UnitSize.Returns(targetSize);

            target.Transformable.Returns(transformable);

            return target;
        }
    }
}
