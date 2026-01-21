using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Value object encapsulating all parameters needed for a pathfinding request
    /// </summary>
    public class PathRequest
    {
        public Vector2Int StartPosition { get; }
        public Vector2Int TargetPosition { get; }
        public UnitSize UnitSize { get; }
        public Direction Direction { get; }
        public bool IgnoreOccupied { get; }
        public object RequestingUnit { get; }  // For debugging/occupancy tracking

        public PathRequest(
            Vector2Int startPosition,
            Vector2Int targetPosition,
            UnitSize unitSize,
            Direction direction,
            bool ignoreOccupied = false,
            object requestingUnit = null)
        {
            StartPosition = startPosition;
            TargetPosition = targetPosition;
            UnitSize = unitSize;
            Direction = direction;
            IgnoreOccupied = ignoreOccupied;
            RequestingUnit = requestingUnit;
        }

        public override string ToString()
        {
            return $"PathRequest: {StartPosition} -> {TargetPosition}, Size: {UnitSize}, Dir: {Direction}";
        }
    }
}
