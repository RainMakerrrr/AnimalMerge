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
    /// Tests for attack zone calculation
    /// TC-4.1, TC-4.2
    /// Note: Attack zone is internal logic, we verify it through GetPossibleAttackPositions results
    /// </summary>
    [TestFixture]
    public class AttackZoneTests
    {
        private IGridManager _gridManager;
        private TargetPositionCalculator _calculator;

        [SetUp]
        public void Setup()
        {
            _gridManager = GridTestHelper.CreateMockGrid(10, 15);
            _calculator = new TargetPositionCalculator(_gridManager);
        }

        /// <summary>
        /// TC-4.1: Зона атаки для одной клетки (1×1 target)
        /// Зона атаки должна включать 8 соседних клеток вокруг цели
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_SingleCellTarget_CoversAllAdjacentCells()
        {
            // Arrange - target at (5, 5)
            var targetPos = new Vector2Int(5, 5);
            var currentNode = _gridManager.GetCell(0, 0); // Far from target

            var targetCell = _gridManager.GetCell(targetPos);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Mark target as occupied
            GridTestHelper.SetupOccupiedCells(_gridManager, targetPos);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1 attacker
                Direction.North);

            // Assert - attack zone around (5,5) includes 8 adjacent cells
            // Expected positions: (4,4), (5,4), (6,4), (4,5), (6,5), (4,6), (5,6), (6,6)
            positions.Should().NotBeEmpty("should find positions around single cell target");

            // Verify all 8 adjacent positions are in the result
            var expectedAdjacentPositions = new List<Vector2Int>
            {
                new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4), // below
                new Vector2Int(4, 5),                       new Vector2Int(6, 5), // sides
                new Vector2Int(4, 6), new Vector2Int(5, 6), new Vector2Int(6, 6)  // above
            };

            foreach (var expectedPos in expectedAdjacentPositions)
            {
                positions.Should().Contain(expectedPos,
                    $"attack zone should include adjacent position {expectedPos}");
            }

            // Verify target position itself is NOT in results (occupied by target)
            positions.Should().NotContain(targetPos,
                "target position should not be in attack positions");
        }

        /// <summary>
        /// TC-4.2: Зона атаки для юнита 2×2
        /// Зона атаки вокруг большого юнита содержит 12 уникальных клеток
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_2x2Target_CoversAllSurroundingCells()
        {
            // Arrange - 2×2 target at (3,8), (4,8), (3,9), (4,9)
            var targetPositions = new[]
            {
                new Vector2Int(3, 8), new Vector2Int(4, 8),
                new Vector2Int(3, 9), new Vector2Int(4, 9)
            };

            var currentNode = _gridManager.GetCell(0, 0); // Far from target

            var targetCells = targetPositions
                .Select(pos => _gridManager.GetCell(pos))
                .ToList();

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Mark target cells as occupied
            GridTestHelper.SetupOccupiedCells(_gridManager, targetPositions);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1 attacker
                Direction.North);

            // Assert - attack zone around 2×2 target includes 12 unique surrounding cells
            // Expected: (2,7), (3,7), (4,7), (5,7), (2,8), (5,8), (2,9), (5,9), (2,10), (3,10), (4,10), (5,10)
            positions.Should().NotBeEmpty("should find positions around 2×2 target");

            var expectedSurroundingCells = new List<Vector2Int>
            {
                // Bottom row (y=7)
                new Vector2Int(2, 7), new Vector2Int(3, 7), new Vector2Int(4, 7), new Vector2Int(5, 7),
                // Middle rows - sides only
                new Vector2Int(2, 8), new Vector2Int(5, 8),
                new Vector2Int(2, 9), new Vector2Int(5, 9),
                // Top row (y=10)
                new Vector2Int(2, 10), new Vector2Int(3, 10), new Vector2Int(4, 10), new Vector2Int(5, 10)
            };

            // Verify all 12 surrounding positions are in the result
            foreach (var expectedPos in expectedSurroundingCells)
            {
                positions.Should().Contain(expectedPos,
                    $"attack zone should include surrounding position {expectedPos}");
            }

            // Verify target positions themselves are NOT in results
            foreach (var targetPos in targetPositions)
            {
                positions.Should().NotContain(targetPos,
                    $"target position {targetPos} should not be in attack positions");
            }

            // Count unique positions
            positions.Distinct().Count().Should().BeGreaterThanOrEqualTo(12,
                "attack zone should include at least 12 unique surrounding cells");
        }

        /// <summary>
        /// Additional test: Attack zone for 1×2 target (rectangular)
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_1x2Target_CoversAllSurroundingCells()
        {
            // Arrange - 1×2 target at (5,5), (5,6)
            var targetPositions = new[]
            {
                new Vector2Int(5, 5),
                new Vector2Int(5, 6)
            };

            var currentNode = _gridManager.GetCell(0, 0);

            var targetCells = targetPositions
                .Select(pos => _gridManager.GetCell(pos))
                .ToList();

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, targetPositions);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert - should cover all cells around 1×2 target
            positions.Should().NotBeEmpty("should find positions around 1×2 target");

            // Expected surrounding cells:
            // (4,4), (5,4), (6,4) - below first cell
            // (4,5), (6,5) - sides of first cell
            // (4,6), (6,6) - sides of second cell
            // (4,7), (5,7), (6,7) - above second cell
            var expectedSurrounding = new List<Vector2Int>
            {
                new Vector2Int(4, 4), new Vector2Int(5, 4), new Vector2Int(6, 4),
                new Vector2Int(4, 5), new Vector2Int(6, 5),
                new Vector2Int(4, 6), new Vector2Int(6, 6),
                new Vector2Int(4, 7), new Vector2Int(5, 7), new Vector2Int(6, 7)
            };

            // Verify all surrounding positions
            foreach (var expectedPos in expectedSurrounding)
            {
                positions.Should().Contain(expectedPos,
                    $"attack zone should include position {expectedPos} around 1×2 target");
            }

            // Target cells should not be in results
            foreach (var targetPos in targetPositions)
            {
                positions.Should().NotContain(targetPos,
                    $"target position {targetPos} should not be in attack positions");
            }
        }

        /// <summary>
        /// Edge case: Attack zone at grid boundary
        /// Some cells in attack zone would be out of bounds
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_TargetNearBoundary_FiltersBoundaryCorrectly()
        {
            // Arrange - target at (0, 0) - corner
            var targetPos = new Vector2Int(0, 0);
            var currentNode = _gridManager.GetCell(5, 5);

            var targetCell = _gridManager.GetCell(targetPos);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, targetPos);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert - only valid positions within bounds
            positions.Should().NotBeEmpty("should find some valid positions near corner");

            // Attack zone would include (-1,-1), (-1,0), (0,-1) etc, but only valid positions remain
            var validAdjacentPositions = new List<Vector2Int>
            {
                new Vector2Int(1, 0), // right
                new Vector2Int(0, 1), // up
                new Vector2Int(1, 1)  // diagonal
            };

            foreach (var validPos in validAdjacentPositions)
            {
                positions.Should().Contain(validPos,
                    $"position {validPos} should be in results");
            }

            // Verify no out-of-bounds positions
            foreach (var pos in positions)
            {
                pos.x.Should().BeGreaterThanOrEqualTo(0, "x must be >= 0");
                pos.y.Should().BeGreaterThanOrEqualTo(0, "y must be >= 0");
                pos.x.Should().BeLessThan(10, "x must be < grid width");
                pos.y.Should().BeLessThan(15, "y must be < grid height");
            }
        }
    }
}
