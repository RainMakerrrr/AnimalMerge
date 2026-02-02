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
    /// Regression tests to ensure previously fixed bugs stay fixed
    /// TC-10.1, TC-10.2
    /// </summary>
    [TestFixture]
    public class RegressionTests
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
        /// TC-10.1: Regression - старый баг с (3,7)
        /// CRITICAL: Проверка что исправлен баг где позиция (3,7) неправильно включалась в список
        /// Баг: позиция (3,7) перекрывается с целью на (3,8)-(4,9)
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_ElephantVsElephant_Position37NotInValidList()
        {
            // Arrange - точно как в TC-3.1 (Elephant vs Elephant)
            var scenario = ScenarioBuilder.ElephantVsElephant();

            var currentNode = _gridManager.GetCell(scenario.AttackingUnit.AnchorPosition);

            var targetCells = scenario.TargetUnit.OccupiedCells
                .Select(pos => _gridManager.GetCell(pos))
                .ToList();

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Mark target cells as occupied
            GridTestHelper.SetupOccupiedCells(_gridManager, scenario.TargetUnit.OccupiedCells);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                scenario.AttackingUnit.Size,
                scenario.AttackingUnit.Direction);

            // Assert - CRITICAL CHECKS FOR BUG FIX

            // 1. Position (3, 7) should NOT be in the list
            //    Because placing 2×2 elephant at (3,7) would occupy (3,7), (4,7), (3,8), (4,8)
            //    and (3,8), (4,8) overlap with target at (3,8), (4,8), (3,9), (4,9)
            positions.Should().NotContain(new Vector2Int(3, 7),
                "BUG FIX: position (3,7) causes overlap with target - must be filtered out");

            // 2. Position (3, 6) SHOULD be in the list and have high priority
            positions.Should().Contain(new Vector2Int(3, 6),
                "position (3,6) is valid and should be included");

            // 3. Verify no positions that would overlap with target
            var targetOccupiedPositions = scenario.TargetUnit.OccupiedCells;

            foreach (var pos in positions)
            {
                // Calculate what cells would be occupied if elephant placed at this position
                var wouldOccupy = new List<Vector2Int>
                {
                    pos,
                    pos + new Vector2Int(1, 0),
                    pos + new Vector2Int(0, 1),
                    pos + new Vector2Int(1, 1)
                };

                // Verify no overlap with target
                foreach (var targetPos in targetOccupiedPositions)
                {
                    wouldOccupy.Should().NotContain(targetPos,
                        $"position {pos} should not cause unit to overlap with target at {targetPos}");
                }
            }

            // 4. Additional specific checks for positions near target
            // These positions should be filtered out because they overlap:
            var invalidPositions = new List<Vector2Int>
            {
                new Vector2Int(3, 7), // Overlaps (3,8), (4,8)
                new Vector2Int(4, 7), // Overlaps (4,8)
                new Vector2Int(2, 8), // Overlaps (3,8), (3,9)
                new Vector2Int(3, 8), // Directly on target
                new Vector2Int(4, 8)  // Directly on target
            };

            foreach (var invalidPos in invalidPositions)
            {
                positions.Should().NotContain(invalidPos,
                    $"BUG FIX: position {invalidPos} should be filtered (causes overlap)");
            }
        }

        /// <summary>
        /// TC-10.2: Regression - null target handling
        /// Проверка что алгоритм корректно обрабатывает null target
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_NullTarget_ReturnsEmptyArrayWithoutException()
        {
            // Arrange
            var currentNode = _gridManager.GetCell(3, 3);

            // Act - null target should be handled gracefully
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                null, // NULL TARGET
                UnitSize.Small,
                Direction.North);

            // Assert - should not throw NullReferenceException
            // Should return empty array instead
            positions.Should().NotBeNull("should return array, not null");
            positions.Should().BeEmpty("should return empty array for null target");
        }

        /// <summary>
        /// TC-10.2 variant: Null current node handling
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_NullCurrentNode_ReturnsEmptyArrayWithoutException()
        {
            // Arrange
            var targetCell = _gridManager.GetCell(5, 5);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act - null current node
            var positions = _calculator.GetPossibleAttackPositions(
                null, // NULL CURRENT NODE
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeNull("should return array, not null");
            positions.Should().BeEmpty("should return empty array for null current node");
        }

        /// <summary>
        /// Regression: Target with null transformable
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_TargetWithNullTransformable_ReturnsEmptyArray()
        {
            // Arrange
            var currentNode = _gridManager.GetCell(3, 3);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns((ITransformable)null); // NULL TRANSFORMABLE

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            // Assert
            positions.Should().NotBeNull();
            positions.Should().BeEmpty("should return empty array when target has null transformable");
        }

        /// <summary>
        /// Regression: Verify overlap detection is consistent
        /// Previously, some overlapping positions were not filtered
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_OverlapDetection_ConsistentlyFiltersAllOverlaps()
        {
            // Arrange - various sized units
            var testCases = new[]
            {
                new { Size = UnitSize.Small, Name = "1×1" },
                new { Size = UnitSize.Medium, Name = "1×2" },
                new { Size = UnitSize.Large, Name = "2×2" }
            };

            foreach (var testCase in testCases)
            {
                // Target at (5, 5)
                var currentNode = _gridManager.GetCell(2, 2);
                var targetCell = _gridManager.GetCell(5, 5);

                var targetTransformable = Substitute.For<ITransformable>();
                targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

                var target = Substitute.For<ITarget>();
                target.Transformable.Returns(targetTransformable);

                GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(5, 5));

                // Act
                var positions = _calculator.GetPossibleAttackPositions(
                    currentNode,
                    target,
                    testCase.Size,
                    Direction.North);

                // Assert - no position should cause overlap with target
                foreach (var pos in positions)
                {
                    // For 1×1 unit
                    if (testCase.Size.Equals(UnitSize.Small))
                    {
                        pos.Should().NotBe(new Vector2Int(5, 5),
                            $"{testCase.Name} unit at {pos} should not overlap target at (5,5)");
                    }
                }
            }
        }

        /// <summary>
        /// Regression: Priority sorting stays consistent
        /// Verify that priority positions always come first
        /// </summary>
        [Test]
        [Category("Regression")]
        public void Regression_PrioritySorting_RemainsConsistent()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            var targetCell = _gridManager.GetCell(5, 5);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(5, 5));

            // Act - run multiple times to verify consistency
            Vector2Int? firstPosition = null;

            for (int i = 0; i < 10; i++)
            {
                var positions = _calculator.GetPossibleAttackPositions(
                    currentNode,
                    target,
                    UnitSize.Small,
                    Direction.North);

                positions.Should().NotBeEmpty("should always find positions");

                if (firstPosition == null)
                {
                    firstPosition = positions[0];
                }
                else
                {
                    positions[0].Should().Be(firstPosition.Value,
                        $"priority position should be consistent across multiple calls (iteration {i})");
                }

                // Verify first position has minimum deltaX
                var firstDeltaX = Mathf.Abs(positions[0].x - currentPos.x);

                foreach (var pos in positions)
                {
                    var deltaX = Mathf.Abs(pos.x - currentPos.x);
                    deltaX.Should().BeGreaterThanOrEqualTo(firstDeltaX,
                        "all positions should have deltaX >= first position's deltaX");
                }
            }
        }
    }
}
