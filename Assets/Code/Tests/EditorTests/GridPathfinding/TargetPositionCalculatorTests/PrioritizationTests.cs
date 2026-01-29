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
    /// Tests for position prioritization logic
    /// TC-6.1, TC-6.2, TC-6.3
    /// </summary>
    [TestFixture]
    public class PrioritizationTests
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
        /// TC-6.1: Приоритет - минимальный deltaX
        /// Позиции с минимальным отклонением по X имеют приоритет
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_MinimumDeltaX()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Target at (4, 7) so we can test positions around it
            var targetCell = _gridManager.GetCell(4, 7);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1
                Direction.North);

            // Assert - positions should be sorted by priority
            positions.Should().NotBeEmpty("should find valid positions");

            // First positions should have minimal deltaX from currentPos (3, 0)
            // Expected order: positions with X=3 (deltaX=0) should come first
            var firstPosition = positions[0];
            var firstDeltaX = Mathf.Abs(firstPosition.x - currentPos.x);

            foreach (var pos in positions)
            {
                var deltaX = Mathf.Abs(pos.x - currentPos.x);
                deltaX.Should().BeGreaterThanOrEqualTo(firstDeltaX,
                    $"position {pos} has deltaX={deltaX}, but first position has deltaX={firstDeltaX}");
            }

            // Specifically, position (3, 6) should be highest priority (deltaX=0, Y=6)
            // or (3, 8) if both are valid
            var topPriorityPositions = positions.Where(p => p.x == 3).ToList();
            if (topPriorityPositions.Count > 0)
            {
                positions[0].x.Should().Be(3,
                    "first position should have X=3 (same as current X) for minimum deltaX");
            }
        }

        /// <summary>
        /// TC-6.2: Приоритет - вторичная сортировка по deltaY
        /// При равном deltaX предпочитается движение вперед (больше Y)
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_SecondaryDeltaY()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Target at (3, 7) - directly above
            var targetCell = _gridManager.GetCell(3, 7);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find valid positions");

            // Filter positions with same X as current (deltaX=0)
            var sameDeltaXPositions = positions.Where(p => p.x == currentPos.x).ToList();

            if (sameDeltaXPositions.Count > 1)
            {
                // Positions with same deltaX should be sorted by Y descending (prefer forward movement)
                for (int i = 0; i < sameDeltaXPositions.Count - 1; i++)
                {
                    sameDeltaXPositions[i].y.Should().BeGreaterThanOrEqualTo(sameDeltaXPositions[i + 1].y,
                        $"position {sameDeltaXPositions[i]} should come before {sameDeltaXPositions[i + 1]} (higher Y preferred)");
                }

                // Expected order: (3, 6) before (3, 8) is wrong - actually (3, 8) should come first
                // because we want to move FORWARD (higher Y)
                // Correction: positions (3, 8), (3, 6), (3, 5), (3, 4) - sorted by Y descending
                var expectedFirst = sameDeltaXPositions.OrderByDescending(p => p.y).First();
                sameDeltaXPositions[0].Should().Be(expectedFirst,
                    "highest Y position should be first when deltaX is equal");
            }
        }

        /// <summary>
        /// TC-6.3: Приоритет - комбинированная сортировка
        /// Проверка полной логики: первичная по deltaX, вторичная по deltaY descending
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_CombinedSorting()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 2);
            var currentNode = _gridManager.GetCell(currentPos);

            // Create a 2×2 target at (4, 7) to get many possible positions
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(4, 7),
                _gridManager.GetCell(5, 7),
                _gridManager.GetCell(4, 8),
                _gridManager.GetCell(5, 8)
            };

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small, // 1×1
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find multiple valid positions");

            // Verify sorting logic:
            // 1. Primary: deltaX ascending (prefer positions with X close to 3)
            // 2. Secondary: deltaY descending (prefer higher Y when deltaX is equal)

            for (int i = 0; i < positions.Length - 1; i++)
            {
                var current = positions[i];
                var next = positions[i + 1];

                var currentDeltaX = Mathf.Abs(current.x - currentPos.x);
                var nextDeltaX = Mathf.Abs(next.x - currentPos.x);

                if (currentDeltaX == nextDeltaX)
                {
                    // Same deltaX - check that Y is descending
                    current.y.Should().BeGreaterThanOrEqualTo(next.y,
                        $"positions {current} and {next} have same deltaX={currentDeltaX}, but Y is not sorted descending");
                }
                else
                {
                    // Different deltaX - current should have smaller or equal deltaX
                    currentDeltaX.Should().BeLessThanOrEqualTo(nextDeltaX,
                        $"position {current} (deltaX={currentDeltaX}) should come before {next} (deltaX={nextDeltaX})");
                }
            }

            // Specific checks based on TC-6.3 expectations:
            // Expected order examples:
            // - Positions with X=3 (deltaX=0) should be first
            // - Among X=3, higher Y values first
            // - Then positions with X=2 or X=4 (deltaX=1)
            // - Then positions with X=1 or X=5 (deltaX=2), etc.

            var firstDeltaX = Mathf.Abs(positions[0].x - currentPos.x);
            firstDeltaX.Should().Be(0,
                "first position should have deltaX=0 (X=3, same as current position)");
        }

        /// <summary>
        /// Edge case: All positions have same deltaX - verify Y sorting
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_AllSameDeltaX_SortedByYDescending()
        {
            // Arrange
            var currentPos = new Vector2Int(4, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Target directly above at (4, 8)
            var targetCell = _gridManager.GetCell(4, 8);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            var straightLinePositions = positions.Where(p => p.x == currentPos.x).ToList();

            if (straightLinePositions.Count > 1)
            {
                // All should be sorted by Y descending
                for (int i = 0; i < straightLinePositions.Count - 1; i++)
                {
                    straightLinePositions[i].y.Should().BeGreaterThan(straightLinePositions[i + 1].y,
                        "positions in straight line should be sorted by Y descending (prefer forward)");
                }
            }
        }
    }
}
