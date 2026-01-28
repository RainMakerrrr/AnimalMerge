using System.Collections.Generic;
using FluentAssertions;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers
{
    /// <summary>
    /// Custom FluentAssertions extensions for pathfinding tests
    /// </summary>
    public static class AssertionExtensions
    {
        /// <summary>
        /// Asserts that a Vector2Int is adjacent to the target position (including diagonals)
        /// Adjacent means Manhattan or Chebyshev distance <= 1
        /// </summary>
        public static void BeAdjacentTo(this Vector2Int subject, Vector2Int target, string because = "")
        {
            var dx = Mathf.Abs(subject.x - target.x);
            var dy = Mathf.Abs(subject.y - target.y);
            var isAdjacent = dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0);

            isAdjacent.Should().BeTrue(
                $"position {subject} should be adjacent to {target} {because}, but distance was ({dx},{dy})");
        }

        /// <summary>
        /// Asserts that a Vector2Int is within grid bounds
        /// </summary>
        public static void BeWithinBounds(this Vector2Int subject, int width, int height, string because = "")
        {
            var isWithinBounds = subject.x >= 0 && subject.x < width && subject.y >= 0 && subject.y < height;

            isWithinBounds.Should().BeTrue(
                $"position {subject} should be within bounds (0,0) to ({width - 1},{height - 1}) {because}");
        }

        /// <summary>
        /// Asserts that a collection of Vector2Int positions are all unique
        /// </summary>
        public static void ContainUniquePositions(this IEnumerable<Vector2Int> subject, string because = "")
        {
            var list = new List<Vector2Int>(subject);
            var uniqueSet = new HashSet<Vector2Int>(list);

            list.Count.Should().Be(uniqueSet.Count,
                $"all positions should be unique {because}, but found {list.Count - uniqueSet.Count} duplicates");
        }

        /// <summary>
        /// Asserts that a Vector2Int has a specific X coordinate
        /// </summary>
        public static void HaveX(this Vector2Int subject, int expectedX, string because = "")
        {
            subject.x.Should().Be(expectedX, $"X coordinate should be {expectedX} {because}");
        }

        /// <summary>
        /// Asserts that a Vector2Int has a specific Y coordinate
        /// </summary>
        public static void HaveY(this Vector2Int subject, int expectedY, string because = "")
        {
            subject.y.Should().Be(expectedY, $"Y coordinate should be {expectedY} {because}");
        }

        /// <summary>
        /// Asserts that a collection contains all expected positions
        /// </summary>
        public static void ContainAllPositions(this IEnumerable<Vector2Int> subject, params Vector2Int[] expected)
        {
            var subjectList = new List<Vector2Int>(subject);

            foreach (var expectedPos in expected)
            {
                subjectList.Should().Contain(expectedPos,
                    $"collection should contain position {expectedPos}");
            }
        }

        /// <summary>
        /// Asserts that a position is at Manhattan distance from target
        /// </summary>
        public static void BeAtManhattanDistance(this Vector2Int subject, Vector2Int target, int distance, string because = "")
        {
            var actualDistance = Mathf.Abs(subject.x - target.x) + Mathf.Abs(subject.y - target.y);

            actualDistance.Should().Be(distance,
                $"Manhattan distance from {subject} to {target} should be {distance} {because}");
        }

        /// <summary>
        /// Asserts that a position is at Chebyshev distance from target
        /// </summary>
        public static void BeAtChebyshevDistance(this Vector2Int subject, Vector2Int target, int distance, string because = "")
        {
            var actualDistance = Mathf.Max(Mathf.Abs(subject.x - target.x), Mathf.Abs(subject.y - target.y));

            actualDistance.Should().Be(distance,
                $"Chebyshev distance from {subject} to {target} should be {distance} {because}");
        }

        /// <summary>
        /// Asserts that positions are sorted by priority (deltaX ascending, then deltaY descending)
        /// </summary>
        public static void BeSortedByPriority(this IEnumerable<Vector2Int> subject, Vector2Int referencePoint, string because = "")
        {
            var list = new List<Vector2Int>(subject);

            for (int i = 0; i < list.Count - 1; i++)
            {
                var current = list[i];
                var next = list[i + 1];

                var currentDeltaX = Mathf.Abs(current.x - referencePoint.x);
                var nextDeltaX = Mathf.Abs(next.x - referencePoint.x);

                var currentDeltaY = current.y - referencePoint.y;
                var nextDeltaY = next.y - referencePoint.y;

                // Check if sorted by deltaX ascending
                if (currentDeltaX > nextDeltaX)
                {
                    throw new FluentAssertions.Execution.AssertionFailedException(
                        $"Positions should be sorted by deltaX ascending {because}, " +
                        $"but {current} (deltaX={currentDeltaX}) comes before {next} (deltaX={nextDeltaX})");
                }

                // If deltaX is equal, check if sorted by deltaY descending (prefer higher Y)
                if (currentDeltaX == nextDeltaX && currentDeltaY < nextDeltaY)
                {
                    throw new FluentAssertions.Execution.AssertionFailedException(
                        $"Positions with equal deltaX should be sorted by deltaY descending {because}, " +
                        $"but {current} (deltaY={currentDeltaY}) comes before {next} (deltaY={nextDeltaY})");
                }
            }
        }
    }
}
