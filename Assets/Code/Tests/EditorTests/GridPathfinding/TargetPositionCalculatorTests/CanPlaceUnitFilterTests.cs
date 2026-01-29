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
    /// Tests for CanPlaceUnit filtering logic
    /// TC-5.1, TC-5.2, TC-5.3
    /// </summary>
    [TestFixture]
    public class CanPlaceUnitFilterTests
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
        /// TC-5.1: Фильтрация - позиция за границами сетки
        /// Anchor points за границами сетки должны быть отфильтрованы
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_OutOfBounds_FiltersInvalidPositions()
        {
            // Arrange - target near grid boundary
            var targetTransformable = Substitute.For<ITransformable>();
            var targetCell = _gridManager.GetCell(7, 5);
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            var currentNode = _gridManager.GetCell(0, 0);

            // Act - for 2×2 unit, positions near boundary
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2 unit
                Direction.North);

            // Assert
            positions.Should().NotContain(new Vector2Int(7, 5),
                "position (7,5) would place unit at (7,5), (8,5) - out of bounds");
            positions.Should().NotContain(new Vector2Int(8, 5),
                "position (8,5) is completely out of bounds");
            positions.Should().NotContain(new Vector2Int(-1, 5),
                "negative positions should be filtered");

            // All returned positions should be valid
            foreach (var pos in positions)
            {
                pos.x.Should().BeGreaterThanOrEqualTo(0, "x must be >= 0");
                pos.y.Should().BeGreaterThanOrEqualTo(0, "y must be >= 0");
                pos.x.Should().BeLessThan(8, "x must be < gridWidth");

                // For 2×2 unit, also check that unit won't go out of bounds
                (pos.x + 1).Should().BeLessThan(8, "unit width must fit in grid");
                (pos.y + 1).Should().BeLessThan(10, "unit height must fit in grid");
            }
        }

        /// <summary>
        /// TC-5.2: Фильтрация - позиция занята другим юнитом
        /// Позиции, где юнит будет занимать уже занятые клетки, должны быть отфильтрованы
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_OccupiedByOtherUnit_FiltersPosition()
        {
            // Arrange
            var targetCell = _gridManager.GetCell(5, 5);

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            var currentNode = _gridManager.GetCell(0, 0);

            // Mark cell (4, 4) as occupied by another unit
            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(4, 4));

            // Act - 2×2 unit trying to move to (3, 3) would occupy (3,3), (4,3), (3,4), (4,4)
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert
            positions.Should().NotContain(new Vector2Int(3, 3),
                "position (3,3) would place unit on occupied cell (4,4)");

            // All returned positions should not overlap with (4,4)
            foreach (var pos in positions)
            {
                var wouldOccupyCells = new List<Vector2Int>
                {
                    pos,
                    pos + new Vector2Int(1, 0),
                    pos + new Vector2Int(0, 1),
                    pos + new Vector2Int(1, 1)
                };

                wouldOccupyCells.Should().NotContain(new Vector2Int(4, 4),
                    $"position {pos} would place unit on occupied cell (4,4)");
            }
        }

        /// <summary>
        /// TC-5.3: Фильтрация - ignoreOccupied для самого юнита
        /// Текущая позиция атакующего юнита должна игнорироваться при проверке CanPlaceUnit
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_CurrentUnitPosition_IgnoredInChecks()
        {
            // Arrange
            var attackingUnitAnchor = new Vector2Int(3, 2);
            var currentNode = _gridManager.GetCell(attackingUnitAnchor);

            // Mark attacking unit's occupied cells (3,2), (4,2), (3,3), (4,3)
            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(3, 2), new Vector2Int(4, 2),
                new Vector2Int(3, 3), new Vector2Int(4, 3));

            // Target at (3, 6)
            var targetCell = _gridManager.GetCell(3, 6);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert - should find valid positions despite current unit occupying cells
            positions.Should().NotBeEmpty("should find positions even though unit is currently on grid");

            // Position (3,4) should be valid - it's next to target
            positions.Should().Contain(new Vector2Int(3, 4),
                "position (3,4) should be valid even though unit currently occupies nearby cells");
        }

        /// <summary>
        /// Edge case: Target at corner of grid
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_TargetAtCorner_OnlyValidPositions()
        {
            // Arrange - target at corner (0, 0)
            var targetCell = _gridManager.GetCell(0, 0);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            var currentNode = _gridManager.GetCell(5, 5);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1
                Direction.North);

            // Assert - only positions within bounds
            positions.Should().NotBeEmpty("should find some valid positions");
            positions.Should().NotContain(new Vector2Int(-1, 0), "no negative positions");
            positions.Should().NotContain(new Vector2Int(0, -1), "no negative positions");

            // Should contain valid adjacent positions like (1, 0), (0, 1)
            positions.Should().Contain(new Vector2Int(1, 0), "right of corner should be valid");
            positions.Should().Contain(new Vector2Int(0, 1), "above corner should be valid");
        }
    }
}
