using Code.GridPathfinding;
using UnityEngine;

namespace Code.Tests.EditorTests.Helpers
{
    /// <summary>
    /// Simple POCO test double for GridCell (which is MonoBehaviour and can't be mocked)
    /// Used for unit testing pathfinding logic without Unity runtime dependencies
    /// </summary>
    public class TestGridCell : IGridCell
    {
        public int X { get; set; }
        public int Y { get; set; }
        public Vector2Int GridPosition => new Vector2Int(X, Y);
        public bool IsWalkable { get; set; }
        public Vector3 WorldPosition { get; set; }

        // A* properties
        public float GCost { get; set; }
        public float HCost { get; set; }
        public float FCost => GCost + HCost;

        private IGridCell _parent;
        public IGridCell Parent
        {
            get => _parent;
            set => _parent = value;
        }

        public TestGridCell(int x, int y, bool isWalkable = true)
        {
            X = x;
            Y = y;
            IsWalkable = isWalkable;
            WorldPosition = new Vector3(x, 0, y);
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }

        public void Reset()
        {
            GCost = float.MaxValue;
            HCost = 0;
            Parent = null;
        }

        public override string ToString() => $"TestCell({X},{Y}) Walkable:{IsWalkable}";

        public override int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public override bool Equals(object obj) =>
            obj is TestGridCell other && other.X == X && other.Y == Y;
    }
}
