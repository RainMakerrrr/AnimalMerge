using System.Collections.Generic;
using System.Diagnostics;
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
    /// Performance tests for pathfinding calculations
    /// TC-9.1, TC-9.2
    /// Note: Performance thresholds may vary by hardware
    /// </summary>
    [TestFixture]
    public class PerformanceTests
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
        /// TC-9.1: Performance - Internal calculations should be fast
        /// Tests performance through public API (GetPossibleAttackPositions)
        /// Note: GetAnchorPointsForCell is private, so we test the full algorithm
        /// </summary>
        [Test]
        [Category("Performance")]
        public void Performance_RepeatedCalculations_CompletesQuickly()
        {
            // Arrange - simple scenario for repeated calculations
            var currentNode = _gridManager.GetCell(3, 3);
            var targetCell = _gridManager.GetCell(5, 5);

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(5, 5));

            // Warmup - JIT compilation
            for (int i = 0; i < 10; i++)
            {
                _calculator.GetPossibleAttackPositions(currentNode, target, UnitSize.Small, Direction.North);
            }

            // Act - measure performance of repeated calculations
            var stopwatch = Stopwatch.StartNew();
            const int iterations = 100;

            for (int i = 0; i < iterations; i++)
            {
                _calculator.GetPossibleAttackPositions(currentNode, target, UnitSize.Small, Direction.North);
            }

            stopwatch.Stop();

            // Assert
            var elapsedMs = stopwatch.ElapsedMilliseconds;
            var avgMs = elapsedMs / (double)iterations;
            UnityEngine.Debug.Log($"Repeated calculations: {iterations} iterations took {elapsedMs}ms ({avgMs:F3}ms per call)");

            elapsedMs.Should().BeLessThan(200, // Relaxed threshold for Unity Editor with mocks
                $"{iterations} iterations should complete quickly (actual: {elapsedMs}ms)");
        }

        /// <summary>
        /// TC-9.2: Performance - GetPossibleAttackPositions полный алгоритм
        /// Ожидается: < 5ms для одного вызова
        /// </summary>
        [Test]
        [Category("Performance")]
        public void Performance_GetPossibleAttackPositions_CompletesInReasonableTime()
        {
            // Arrange - elephant attacking elephant (complex case with 2×2 units)
            var attackerPos = new Vector2Int(3, 0);
            var targetPos = new Vector2Int(3, 8);

            var currentNode = _gridManager.GetCell(attackerPos);

            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(3, 8),
                _gridManager.GetCell(4, 8),
                _gridManager.GetCell(3, 9),
                _gridManager.GetCell(4, 9)
            };

            GridTestHelper.SetupOccupiedCells(_gridManager,
                new Vector2Int(3, 8), new Vector2Int(4, 8),
                new Vector2Int(3, 9), new Vector2Int(4, 9));

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Warmup
            for (int i = 0; i < 5; i++)
            {
                _calculator.GetPossibleAttackPositions(currentNode, target, UnitSize.Large, Direction.North);
            }

            // Act - measure single call
            var stopwatch = Stopwatch.StartNew();
            var result = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large,
                Direction.North);
            stopwatch.Stop();

            // Assert
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"GetPossibleAttackPositions: completed in {elapsedMs:F3}ms, found {result.Length} positions");

            result.Should().NotBeEmpty("should find valid positions");

            elapsedMs.Should().BeLessThan(10, // Relaxed threshold for Unity Editor with mocks
                $"single call should complete quickly (actual: {elapsedMs:F3}ms)");
        }

        /// <summary>
        /// Performance test: Multiple consecutive calls should maintain performance
        /// </summary>
        [Test]
        [Category("Performance")]
        public void Performance_MultipleConsecutiveCalls_MaintainPerformance()
        {
            // Arrange - setup scenario
            var currentNode = _gridManager.GetCell(2, 2);
            var targetCell = _gridManager.GetCell(5, 5);

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(5, 5));

            // Act - measure 100 consecutive calls
            var stopwatch = Stopwatch.StartNew();
            const int calls = 100;

            for (int i = 0; i < calls; i++)
            {
                var positions = _calculator.GetPossibleAttackPositions(
                    currentNode,
                    target,
                    UnitSize.Small,
                    Direction.North);

                positions.Should().NotBeEmpty("should find positions on each call");
            }

            stopwatch.Stop();

            // Assert
            var totalMs = stopwatch.ElapsedMilliseconds;
            var avgMs = totalMs / (double)calls;

            UnityEngine.Debug.Log($"100 consecutive calls: total {totalMs}ms, average {avgMs:F3}ms per call");

            avgMs.Should().BeLessThan(1.0, // Average should be under 1ms per call
                $"average time per call should be fast (actual: {avgMs:F3}ms)");
        }

        /// <summary>
        /// Performance test: Complex scenario with obstacles
        /// </summary>
        [Test]
        [Category("Performance")]
        public void Performance_ComplexScenarioWithObstacles_AcceptablePerformance()
        {
            // Arrange - grid with 50% obstacles
            var attackerPos = new Vector2Int(1, 1);
            var targetPos = new Vector2Int(8, 8);

            var currentNode = _gridManager.GetCell(attackerPos);

            // Create 2×2 target
            var targetCells = new List<IGridCell>
            {
                _gridManager.GetCell(8, 8),
                _gridManager.GetCell(9, 8),
                _gridManager.GetCell(8, 9),
                _gridManager.GetCell(9, 9)
            };

            var blockedCells = new List<Vector2Int>();
            var random = new System.Random(123); // Fixed seed

            // Add 50% obstacles
            for (int x = 0; x < 10; x++)
            {
                for (int y = 0; y < 15; y++)
                {
                    if ((x == attackerPos.x && y == attackerPos.y) ||
                        (x >= 8 && x <= 9 && y >= 8 && y <= 9))
                        continue;

                    if (random.NextDouble() < 0.5)
                    {
                        blockedCells.Add(new Vector2Int(x, y));
                    }
                }
            }

            GridTestHelper.SetupOccupiedCells(_gridManager, blockedCells.ToArray());

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(targetCells);

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            // Act - measure with obstacles
            var stopwatch = Stopwatch.StartNew();

            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Large, // 2×2 unit
                Direction.North);

            stopwatch.Stop();

            // Assert
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"Complex scenario (50% obstacles): {elapsedMs:F3}ms, found {positions.Length} positions");

            // With many obstacles, fewer positions expected but should still complete quickly
            elapsedMs.Should().BeLessThan(20,
                $"should complete even with obstacles (actual: {elapsedMs:F3}ms)");
        }

        /// <summary>
        /// Performance baseline: Empty grid (best case)
        /// </summary>
        [Test]
        [Category("Performance")]
        public void Performance_EmptyGrid_BestCasePerformance()
        {
            // Arrange - minimal obstacles, best case scenario
            var currentNode = _gridManager.GetCell(0, 0);
            var targetCell = _gridManager.GetCell(5, 5);

            var targetTransformable = Substitute.For<ITransformable>();
            targetTransformable.GetOccupiedCells().Returns(new List<IGridCell> { targetCell });

            var target = Substitute.For<ITarget>();
            target.Transformable.Returns(targetTransformable);

            GridTestHelper.SetupOccupiedCells(_gridManager, new Vector2Int(5, 5));

            // Act - best case measurement
            var stopwatch = Stopwatch.StartNew();

            var positions = _calculator.GetPossibleAttackPositions(
                currentNode,
                target,
                UnitSize.Small,
                Direction.North);

            stopwatch.Stop();

            // Assert
            var elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
            UnityEngine.Debug.Log($"Empty grid (best case): {elapsedMs:F3}ms, found {positions.Length} positions");

            positions.Should().NotBeEmpty("empty grid should find many positions");

            elapsedMs.Should().BeLessThan(5,
                $"best case should be very fast (actual: {elapsedMs:F3}ms)");
        }
    }
}
