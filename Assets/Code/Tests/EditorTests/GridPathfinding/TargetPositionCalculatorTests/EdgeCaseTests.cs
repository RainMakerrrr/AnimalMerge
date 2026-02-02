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
    /// Edge case tests for TargetPositionCalculator
    /// TC-8.1, TC-8.2, TC-8.3, TC-8.4
    /// </summary>
    [TestFixture]
    public class EdgeCaseTests
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
        /// TC-8.1: Цель в углу сетки - некоторые позиции атаки за границами
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_TargetInCorner_FiltersOutOfBounds()
        {
            // Arrange - target at corner (0, 0)
            var targetCell = _gridManager.GetCell(0, 0);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Attacker at (3, 3)
            var currentNode = _gridManager.GetCell(3, 3);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1 cat
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find some valid positions even at corner");

            // Attack zone would include out-of-bounds cells like (-1,-1), (-1,0), (0,-1)
            // But after filtering, only valid positions remain
            foreach (var pos in positions)
            {
                pos.x.Should().BeGreaterThanOrEqualTo(0, "no negative X coordinates");
                pos.y.Should().BeGreaterThanOrEqualTo(0, "no negative Y coordinates");
                pos.x.Should().BeLessThan(8, "within grid width");
                pos.y.Should().BeLessThan(10, "within grid height");
            }

            // Expected valid positions near corner: (1,0), (0,1), (1,1)
            positions.Should().Contain(new Vector2Int(1, 0), "position right of corner");
            positions.Should().Contain(new Vector2Int(0, 1), "position above corner");
            positions.Should().Contain(new Vector2Int(1, 1), "position diagonal from corner");
        }

        /// <summary>
        /// TC-8.2: Юнит уже рядом с целью - текущая позиция уже является позицией атаки
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_UnitAlreadyAdjacentToTarget_IncludesCurrentPosition()
        {
            // Arrange - attacker already adjacent to target
            var attackerPos = new Vector2Int(3, 5);
            var targetPos = new Vector2Int(3, 6);

            var currentNode = _gridManager.GetCell(attackerPos);
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
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find attack positions");

            // Current position (3, 5) should be included as it's adjacent to target at (3, 6)
            positions.Should().Contain(attackerPos,
                "current position should be valid attack position when already adjacent");

            // Verify IsCloseToTarget would return true
            var isClose = _calculator.IsCloseToTarget(
                currentNode,
                new List<IGridCell>(),
                target);

            isClose.Should().BeTrue("attacker should already be close to target");
        }

        /// <summary>
        /// TC-8.3: Пустая сетка (только 2 юнита) - максимально простой случай
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_EmptyGrid_FindsManyPositions()
        {
            // Arrange - elephant vs elephant with empty grid
            var attackerPos = new Vector2Int(0, 0);
            var targetPos = new Vector2Int(4, 4);

            var currentNode = _gridManager.GetCell(attackerPos);

            // Target 2×2 at (4, 4)
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(4, 4),
                _gridManager.GetCell(5, 4),
                _gridManager.GetCell(4, 5),
                _gridManager.GetCell(5, 5)
            };

            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(4, 4), new Vector2Int(5, 4),
                new Vector2Int(4, 5), new Vector2Int(5, 5));

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2 elephant
                Direction.North);

            // Assert - with empty grid, should find many positions
            positions.Should().NotBeEmpty("empty grid should allow many attack positions");
            positions.Length.Should().BeGreaterThan(5,
                "should find multiple valid positions around 2×2 target");

            // All positions should be valid and not overlap with target
            foreach (var pos in positions)
            {
                // Check that this position doesn't overlap with target cells (4,4), (5,4), (4,5), (5,5)
                var occupiedByUnit = new List<Vector2Int>
                {
                    pos,
                    pos + new Vector2Int(1, 0),
                    pos + new Vector2Int(0, 1),
                    pos + new Vector2Int(1, 1)
                };

                occupiedByUnit.Should().NotContain(new Vector2Int(4, 4), "shouldn't overlap target");
                occupiedByUnit.Should().NotContain(new Vector2Int(5, 4), "shouldn't overlap target");
                occupiedByUnit.Should().NotContain(new Vector2Int(4, 5), "shouldn't overlap target");
                occupiedByUnit.Should().NotContain(new Vector2Int(5, 5), "shouldn't overlap target");
            }
        }

        /// <summary>
        /// TC-8.4: Сетка заполнена почти полностью - стресс-тест
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_HighlyOccupiedGrid_FindsFewOrNoPositions()
        {
            // Arrange - grid 80% filled with obstacles
            var attackerPos = new Vector2Int(0, 0);
            var targetPos = new Vector2Int(7, 9);

            var currentNode = _gridManager.GetCell(attackerPos);
            var targetCell = _gridManager.GetCell(targetPos);

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Fill grid with obstacles (80% occupancy)
            var blockedCells = new List<Vector2Int>();
            var random = new System.Random(42); // Fixed seed for reproducibility

            for (int x = 0; x < 8; x++)
            {
                for (int y = 0; y < 10; y++)
                {
                    // Skip attacker and target positions
                    if ((x == 0 && y == 0) || (x == 7 && y == 9))
                        continue;

                    // 80% chance to be blocked
                    if (random.NextDouble() < 0.8)
                    {
                        blockedCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            GridTestHelper.SetupOccupiedCells(_gridManager, blockedCells.ToArray());

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Medium, // 1×2 cheetah
                Direction.North);

            // Assert - with 80% occupancy, expect very few or no positions
            // This is acceptable behavior - pathfinding will handle "no path" case
            if (positions.Length > 0)
            {
                positions.Length.Should().BeLessThan(5,
                    "should find very few positions with 80% grid occupancy");
            }

            // All found positions should be valid
            foreach (var pos in positions)
            {
                _gridManager.IsInBounds(pos).Should().BeTrue("position should be in bounds");
            }
        }

        /// <summary>
        /// Edge case: Target is same size as attacker at adjacent position
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_SameSizeUnitsAdjacent_FindsValidPositions()
        {
            // Arrange - two 2×2 elephants next to each other
            var attackerPos = new Vector2Int(2, 2);
            var targetPos = new Vector2Int(4, 2); // Adjacent horizontally

            var currentNode = _gridManager.GetCell(attackerPos);

            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(4, 2),
                _gridManager.GetCell(5, 2),
                _gridManager.GetCell(4, 3),
                _gridManager.GetCell(5, 3)
            };

            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(4, 2), new Vector2Int(5, 2),
                new Vector2Int(4, 3), new Vector2Int(5, 3));

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large,
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find positions around adjacent target");

            // Verify no positions would cause overlap with target
            foreach (var pos in positions)
            {
                var occupiedCells = new List<Vector2Int>
                {
                    pos,
                    pos + new Vector2Int(1, 0),
                    pos + new Vector2Int(0, 1),
                    pos + new Vector2Int(1, 1)
                };

                foreach (var targetPos2 in new[] { new Vector2Int(4, 2), new Vector2Int(5, 2),
                                                    new Vector2Int(4, 3), new Vector2Int(5, 3) })
                {
                    occupiedCells.Should().NotContain(targetPos2,
                        $"position {pos} should not overlap with target at {targetPos2}");
                }
            }
        }
    }
}
