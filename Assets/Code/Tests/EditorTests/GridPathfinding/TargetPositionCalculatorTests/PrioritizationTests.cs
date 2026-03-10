using System;
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
    /// TC-6.1-NEW, TC-6.2-NEW, TC-6.3-NEW, TC-6.4-NEW, TC-6.5-NEW
    /// Bug fix 2026-03-10: Updated sorting logic to use occupied cells distance
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
        /// TC-6.1-NEW: Priority - Minimum Distance to Enemy
        /// Positions are sorted by minimum distance from occupied cells to enemy
        /// Bug fix 2026-03-10: Changed from anchor deltaX to occupied cells distance
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_MinimumDistanceToEnemy()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Enemy 2×2 at (3, 8)-(4, 9) - Large unit
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(3, 8),
                _gridManager.GetCell(4, 8),
                _gridManager.GetCell(3, 9),
                _gridManager.GetCell(4, 9)
            };

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert - positions should be sorted by minimum distance to enemy
            positions.Should().NotBeEmpty("should find valid positions");

            // Calculate distance to enemy for each position using occupied cells
            var positionsWithDistance = positions.Select(pos =>
            {
                var occupiedCells = _gridManager.GetOccupiedCells(pos, UnitSize.Large, Direction.North);
                var minDist = occupiedCells.Min(oc =>
                    targetCells.Min(tc => Mathf.Abs(oc.X - tc.X) + Mathf.Abs(oc.Y - tc.Y)));
                return new { Position = pos, Distance = minDist };
            }).ToList();

            // Verify positions are sorted by distance ascending (closer to enemy = higher priority)
            for (int i = 0; i < positionsWithDistance.Count - 1; i++)
            {
                positionsWithDistance[i].Distance.Should().BeLessThanOrEqualTo(positionsWithDistance[i + 1].Distance,
                    $"position {positionsWithDistance[i].Position} (dist={positionsWithDistance[i].Distance}) should come before or equal to " +
                    $"{positionsWithDistance[i + 1].Position} (dist={positionsWithDistance[i + 1].Distance})");
            }

            // Key verification: Position (3, 6) should be prioritized over (5, 8) if both exist
            // (3, 6) occupies (3,6), (4,6), (3,7), (4,7) → min distance 1 from (3,7) to (3,8)
            // (5, 8) occupies (5,8), (6,8), (5,9), (6,9) → min distance 1 from (5,8) to (4,8)
            // Both distance 1, but (3,6) is closer to current (3,0): distance 6 vs 10

            var pos36Index = Array.IndexOf(positions, new Vector2Int(3, 6));
            var pos58Index = Array.IndexOf(positions, new Vector2Int(5, 8));

            if (pos36Index >= 0 && pos58Index >= 0)
            {
                pos36Index.Should().BeLessThan(pos58Index,
                    "bug fix: (3, 6) should come before (5, 8) - both adjacent but (3,6) closer to current");
            }
        }

        /// <summary>
        /// TC-6.2-NEW: Priority - Secondary Sort by Anchor Distance to Current
        /// When multiple positions have same distance to enemy, prefer closer to current position
        /// Bug fix 2026-03-10: Changed from Y descending to anchor distance to current
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_SecondaryAnchorDistanceToCurrent()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Enemy 2×2 at (4, 6)-(5, 7)
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(4, 6),
                _gridManager.GetCell(5, 6),
                _gridManager.GetCell(4, 7),
                _gridManager.GetCell(5, 7)
            };

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty("should find valid positions");

            // Calculate distance to enemy and distance to current for each position
            var positionsWithData = positions.Select(pos =>
            {
                var occupiedCells = _gridManager.GetOccupiedCells(pos, UnitSize.Large, Direction.North);
                var minDistToEnemy = occupiedCells.Min(oc =>
                    targetCells.Min(tc => Mathf.Abs(oc.X - tc.X) + Mathf.Abs(oc.Y - tc.Y)));
                var distToCurrent = Mathf.Abs(pos.x - currentPos.x) + Mathf.Abs(pos.y - currentPos.y);
                return new { Position = pos, DistToEnemy = minDistToEnemy, DistToCurrent = distToCurrent };
            }).ToList();

            // Among positions with same distance to enemy, verify sorted by distance to current
            var groupedByEnemyDist = positionsWithData.GroupBy(p => p.DistToEnemy).ToList();

            foreach (var group in groupedByEnemyDist.Where(g => g.Count() > 1))
            {
                var groupPositions = group.OrderBy(p => Array.IndexOf(positions, p.Position)).ToList();

                for (int i = 0; i < groupPositions.Count - 1; i++)
                {
                    groupPositions[i].DistToCurrent.Should().BeLessThanOrEqualTo(groupPositions[i + 1].DistToCurrent,
                        $"among positions at distance {group.Key} from enemy, " +
                        $"{groupPositions[i].Position} (distToCurrent={groupPositions[i].DistToCurrent}) should come before or equal to " +
                        $"{groupPositions[i + 1].Position} (distToCurrent={groupPositions[i + 1].DistToCurrent})");
                }
            }

            // Specific check: (3, 5) vs (2, 5) if both exist and have same enemy distance
            // (3, 5) → anchor distance to (3, 0) = 5
            // (2, 5) → anchor distance to (3, 0) = 6
            var pos35 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(3, 5));
            var pos25 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(2, 5));

            if (pos35 != null && pos25 != null && pos35.DistToEnemy == pos25.DistToEnemy)
            {
                var pos35Index = Array.IndexOf(positions, pos35.Position);
                var pos25Index = Array.IndexOf(positions, pos25.Position);

                pos35Index.Should().BeLessThan(pos25Index,
                    "(3, 5) should come before (2, 5) - both adjacent but (3, 5) closer to current");
            }
        }

        /// <summary>
        /// TC-6.3-NEW: Priority - Tertiary Sort Prefers Straight Path
        /// When positions have same enemy distance AND same anchor distance, prefer same X as current
        /// Bug fix 2026-03-10: Added tertiary sort for straight path preference (deltaX = 0)
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_Priority_TertiaryPrefersStraightPath()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Create a wide 3×1 target at Y=8 to get multiple positions at same distance
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(2, 8),
                _gridManager.GetCell(3, 8),
                _gridManager.GetCell(4, 8)
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

            // Calculate all three sorting criteria for each position
            var positionsWithData = positions.Select(pos =>
            {
                var occupiedCells = _gridManager.GetOccupiedCells(pos, UnitSize.Small, Direction.North);
                var minDistToEnemy = occupiedCells.Min(oc =>
                    targetCells.Min(tc => Mathf.Abs(oc.X - tc.X) + Mathf.Abs(oc.Y - tc.Y)));
                var distToCurrent = Mathf.Abs(pos.x - currentPos.x) + Mathf.Abs(pos.y - currentPos.y);
                var deltaX = Mathf.Abs(pos.x - currentPos.x);
                return new { Position = pos, DistToEnemy = minDistToEnemy, DistToCurrent = distToCurrent, DeltaX = deltaX };
            }).ToList();

            // Find positions with same enemy distance AND same current distance (tie situation)
            var tieGroups = positionsWithData
                .GroupBy(p => new { p.DistToEnemy, p.DistToCurrent })
                .Where(g => g.Count() > 1)
                .ToList();

            foreach (var group in tieGroups)
            {
                var groupPositions = group.OrderBy(p => Array.IndexOf(positions, p.Position)).ToList();

                // Among tied positions, verify straight path (deltaX=0) is preferred
                for (int i = 0; i < groupPositions.Count - 1; i++)
                {
                    groupPositions[i].DeltaX.Should().BeLessThanOrEqualTo(groupPositions[i + 1].DeltaX,
                        $"among tied positions (distToEnemy={group.Key.DistToEnemy}, distToCurrent={group.Key.DistToCurrent}), " +
                        $"{groupPositions[i].Position} (deltaX={groupPositions[i].DeltaX}) should come before or equal to " +
                        $"{groupPositions[i + 1].Position} (deltaX={groupPositions[i + 1].DeltaX})");
                }
            }

            // Specific check: At Y=7 (adjacent to enemy), position (3, 7) should come before (2, 7) or (4, 7)
            // All at distance 1 from enemy, same distance from current (7), but (3, 7) has deltaX=0
            var pos37 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(3, 7));
            var pos27 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(2, 7));
            var pos47 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(4, 7));

            if (pos37 != null && pos27 != null &&
                pos37.DistToEnemy == pos27.DistToEnemy &&
                pos37.DistToCurrent == pos27.DistToCurrent)
            {
                var pos37Index = Array.IndexOf(positions, pos37.Position);
                var pos27Index = Array.IndexOf(positions, pos27.Position);

                pos37Index.Should().BeLessThan(pos27Index,
                    "(3, 7) should come before (2, 7) - same enemy/current distance but (3, 7) is straight path");
            }

            if (pos37 != null && pos47 != null &&
                pos37.DistToEnemy == pos47.DistToEnemy &&
                pos37.DistToCurrent == pos47.DistToCurrent)
            {
                var pos37Index = Array.IndexOf(positions, pos37.Position);
                var pos47Index = Array.IndexOf(positions, pos47.Position);

                pos37Index.Should().BeLessThan(pos47Index,
                    "(3, 7) should come before (4, 7) - same enemy/current distance but (3, 7) is straight path");
            }
        }

        /// <summary>
        /// TC-6.4-NEW: Large Unit - Occupied Cells Distance Calculation
        /// Verifies that distance is calculated from closest occupied cell to enemy
        /// Bug fix 2026-03-10: Use occupied cells instead of just anchor point
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_LargeUnit_UsesOccupiedCellsDistance()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Enemy 1×1 at (4, 7)
            var targetCell = _gridManager.GetCell(4, 7);
            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(4, 7));

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty();

            // Verify calculation for anchor (3, 6):
            // Occupies: (3, 6), (4, 6), (3, 7), (4, 7)
            // Distance to enemy (4, 7): min(|3-4|+|6-7|, |4-4|+|6-7|, |3-4|+|7-7|, |4-4|+|7-7|)
            // = min(2, 1, 1, 0) = 0 → OVERLAPS!

            positions.Should().NotContain(new Vector2Int(3, 6),
                "position (3, 6) overlaps with enemy at (4, 7)");

            // Verify calculation for anchor (3, 5):
            // Occupies: (3, 5), (4, 5), (3, 6), (4, 6)
            // Distance to enemy (4, 7): min(|3-4|+|5-7|, |4-4|+|5-7|, |3-4|+|6-7|, |4-4|+|6-7|)
            // = min(3, 2, 2, 1) = 1 → VALID and ADJACENT

            positions.Should().Contain(new Vector2Int(3, 5),
                "position (3, 5) is valid and adjacent to enemy");

            // Additional check: (2, 5) should also be valid
            // Occupies: (2, 5), (3, 5), (2, 6), (3, 6)
            // Distance to enemy (4, 7): min(|2-4|+|5-7|, |3-4|+|5-7|, |2-4|+|6-7|, |3-4|+|6-7|)
            // = min(4, 3, 3, 2) = 2 → VALID but farther

            positions.Should().Contain(new Vector2Int(2, 5),
                "position (2, 5) is valid but farther from enemy");

            // Verify (3, 5) comes before (2, 5) due to closer distance to enemy
            var pos35Index = Array.IndexOf(positions, new Vector2Int(3, 5));
            var pos25Index = Array.IndexOf(positions, new Vector2Int(2, 5));

            if (pos35Index >= 0 && pos25Index >= 0)
            {
                pos35Index.Should().BeLessThan(pos25Index,
                    "(3, 5) closer to enemy than (2, 5) - should be prioritized");
            }
        }

        /// <summary>
        /// TC-6.5-NEW: Multiple Units Same Enemy Distance - Sorted by Anchor Distance
        /// Verifies secondary sorting when multiple positions have same distance to enemy
        /// Bug fix 2026-03-10: Added secondary sort by anchor distance to current
        /// </summary>
        [Test]
        public void GetPossibleAttackPositions_SameEnemyDistance_SortedByAnchorDistance()
        {
            // Arrange
            var currentPos = new Vector2Int(3, 0);
            var currentNode = _gridManager.GetCell(currentPos);

            // Enemy 2×2 at (4, 6)-(5, 7)
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(4, 6),
                _gridManager.GetCell(5, 6),
                _gridManager.GetCell(4, 7),
                _gridManager.GetCell(5, 7)
            };

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(4, 6), new Vector2Int(5, 6),
                new Vector2Int(4, 7), new Vector2Int(5, 7));

            // Act
            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2
                Direction.North);

            // Assert
            positions.Should().NotBeEmpty();

            // Calculate distance data for positions
            var positionsWithData = positions.Select(pos =>
            {
                var occupiedCells = _gridManager.GetOccupiedCells(pos, UnitSize.Large, Direction.North);
                var minDistToEnemy = occupiedCells.Min(oc =>
                    targetCells.Min(tc => Mathf.Abs(oc.X - tc.X) + Mathf.Abs(oc.Y - tc.Y)));
                var distToCurrent = Mathf.Abs(pos.x - currentPos.x) + Mathf.Abs(pos.y - currentPos.y);
                return new { Position = pos, DistToEnemy = minDistToEnemy, DistToCurrent = distToCurrent };
            }).ToList();

            // Find adjacent positions (distance 1 from enemy)
            var adjacentPositions = positionsWithData.Where(p => p.DistToEnemy == 1).ToList();

            if (adjacentPositions.Count > 1)
            {
                // Among adjacent positions, verify sorted by distance to current
                var sortedByCurrentDist = adjacentPositions.OrderBy(p => p.DistToCurrent).ToList();
                var actualOrder = adjacentPositions.OrderBy(p => Array.IndexOf(positions, p.Position)).ToList();

                for (int i = 0; i < actualOrder.Count - 1; i++)
                {
                    actualOrder[i].DistToCurrent.Should().BeLessThanOrEqualTo(actualOrder[i + 1].DistToCurrent,
                        $"among adjacent positions, {actualOrder[i].Position} (dist={actualOrder[i].DistToCurrent}) " +
                        $"should come before {actualOrder[i + 1].Position} (dist={actualOrder[i + 1].DistToCurrent})");
                }
            }

            // Specific check: Position (3, 5) vs (2, 5) if both adjacent
            // Position (3, 5) → anchor distance: |3-3| + |5-0| = 5
            // Position (2, 5) → anchor distance: |2-3| + |5-0| = 6
            // Expected: (3, 5) comes before (2, 5)

            var pos35 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(3, 5));
            var pos25 = positionsWithData.FirstOrDefault(p => p.Position == new Vector2Int(2, 5));

            if (pos35 != null && pos25 != null && pos35.DistToEnemy == 1 && pos25.DistToEnemy == 1)
            {
                var pos35Index = Array.IndexOf(positions, pos35.Position);
                var pos25Index = Array.IndexOf(positions, pos25.Position);

                if (pos35Index >= 0 && pos25Index >= 0)
                {
                    pos35Index.Should().BeLessThan(pos25Index,
                        "(3, 5) should come before (2, 5) - both adjacent but (3, 5) closer to current");
                }
            }
        }
    }
}
