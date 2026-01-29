using System.Collections.Generic;
using Code.GridPathfinding;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers
{
    /// <summary>
    /// Builder pattern for creating complex test scenarios
    /// </summary>
    public static class ScenarioBuilder
    {
        /// <summary>
        /// TC-3.1: Elephant vs Elephant (2×2 vs 2×2) - CRITICAL test case
        /// </summary>
        public static TestScenario ElephantVsElephant()
        {
            return new TestScenario
            {
                Name = "Elephant vs Elephant",
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(3, 0),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(3, 0), new Vector2Int(4, 0),
                        new Vector2Int(3, 1), new Vector2Int(4, 1)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(3, 8),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(3, 8), new Vector2Int(4, 8),
                        new Vector2Int(3, 9), new Vector2Int(4, 9)
                    }
                }
            };
        }

        /// <summary>
        /// TC-3.2: Cheetah vs Small Enemy (1×2 vs 1×1)
        /// </summary>
        public static TestScenario CheetahVsSmallEnemy()
        {
            return new TestScenario
            {
                Name = "Cheetah vs Small Enemy",
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(2, 1),
                    Size = UnitSize.Medium,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(2, 1), new Vector2Int(2, 2)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(2, 6),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(2, 6) }
                }
            };
        }

        /// <summary>
        /// TC-3.3: Multiple Valid Positions scenario
        /// Small unit attacking another small unit with clear path
        /// </summary>
        public static TestScenario MultipleValidPositions()
        {
            return new TestScenario
            {
                Name = "Multiple Valid Positions",
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(4, 2),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(4, 2) }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(4, 7),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(4, 7) }
                },
                BlockedCells = new List<Vector2Int>() // No obstacles
            };
        }

        /// <summary>
        /// TC-3.4: No Valid Positions scenario
        /// Target is completely surrounded by obstacles
        /// </summary>
        public static TestScenario NoValidPositions()
        {
            return new TestScenario
            {
                Name = "No Valid Positions (Surrounded Target)",
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(1, 1),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(1, 1) }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(4, 5),
                    Size = UnitSize.Small,
                    Direction = Direction.North,
                    OccupiedCells = new[] { new Vector2Int(4, 5) }
                },
                BlockedCells = new List<Vector2Int>
                {
                    // Surround target completely
                    new Vector2Int(3, 4), new Vector2Int(4, 4), new Vector2Int(5, 4),
                    new Vector2Int(3, 5), /* target at 4,5 */ new Vector2Int(5, 5),
                    new Vector2Int(3, 6), new Vector2Int(4, 6), new Vector2Int(5, 6)
                }
            };
        }

        /// <summary>
        /// Large unit with obstacles on sides
        /// </summary>
        public static TestScenario LargeUnitWithObstacles()
        {
            return new TestScenario
            {
                Name = "Large Unit with Side Obstacles",
                GridSize = new Vector2Int(10, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(4, 1),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(4, 1), new Vector2Int(5, 1),
                        new Vector2Int(4, 2), new Vector2Int(5, 2)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(4, 7),
                    Size = UnitSize.Large,
                    Direction = Direction.North,
                    OccupiedCells = new[]
                    {
                        new Vector2Int(4, 7), new Vector2Int(5, 7),
                        new Vector2Int(4, 8), new Vector2Int(5, 8)
                    }
                },
                BlockedCells = new List<Vector2Int>
                {
                    // Obstacles on left side
                    new Vector2Int(2, 5), new Vector2Int(2, 6),
                    // Obstacles on right side
                    new Vector2Int(7, 5), new Vector2Int(7, 6)
                }
            };
        }

        /// <summary>
        /// Medium unit (1×2) vertical attacking horizontal
        /// </summary>
        public static TestScenario VerticalVsHorizontalMedium()
        {
            return new TestScenario
            {
                Name = "Vertical vs Horizontal Medium",
                GridSize = new Vector2Int(8, 10),
                AttackingUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(2, 2),
                    Size = UnitSize.Medium,
                    Direction = Direction.North, // Vertical
                    OccupiedCells = new[]
                    {
                        new Vector2Int(2, 2), new Vector2Int(2, 3)
                    }
                },
                TargetUnit = new UnitSetup
                {
                    AnchorPosition = new Vector2Int(3, 6),
                    Size = UnitSize.Medium,
                    Direction = Direction.East, // Horizontal
                    OccupiedCells = new[]
                    {
                        new Vector2Int(3, 6), new Vector2Int(4, 6)
                    }
                }
            };
        }
    }

    /// <summary>
    /// Represents a complete test scenario with two units
    /// </summary>
    public class TestScenario
    {
        public string Name { get; set; }
        public Vector2Int GridSize { get; set; }
        public UnitSetup AttackingUnit { get; set; }
        public UnitSetup TargetUnit { get; set; }
        public List<Vector2Int> BlockedCells { get; set; } = new List<Vector2Int>();

        public override string ToString()
        {
            return $"Scenario: {Name} " +
                   $"(Attacker: {AttackingUnit.Size} at {AttackingUnit.AnchorPosition}, " +
                   $"Target: {TargetUnit.Size} at {TargetUnit.AnchorPosition})";
        }
    }

    /// <summary>
    /// Setup data for a single unit in a scenario
    /// </summary>
    public class UnitSetup
    {
        public Vector2Int AnchorPosition { get; set; }
        public UnitSize Size { get; set; }
        public Direction Direction { get; set; }
        public Vector2Int[] OccupiedCells { get; set; }

        public override string ToString()
        {
            return $"{Size} unit at {AnchorPosition} facing {Direction}";
        }
    }
}
