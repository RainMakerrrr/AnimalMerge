using Code.Pathfinding;
using UnityEngine;

namespace Code.GridPathfinding
{
    /// <summary>
    /// Extension methods to convert between old ObjectSizeType and new UnitSize
    /// </summary>
    public static class ObjectSizeTypeExtensions
    {
        public static UnitSize ToUnitSize(this ObjectSizeType objectSizeType)
        {
            switch (objectSizeType)
            {
                case ObjectSizeType.Small:
                    return UnitSize.Small;
                case ObjectSizeType.Medium:
                    return UnitSize.Medium;
                case ObjectSizeType.Big:
                    return UnitSize.Large;
                default:
                    return UnitSize.Small;
            }
        }

        public static ObjectSizeType ToObjectSizeType(this UnitSize unitSize)
        {
            if (unitSize == UnitSize.Small) return ObjectSizeType.Small;
            if (unitSize == UnitSize.Medium) return ObjectSizeType.Medium;
            if (unitSize == UnitSize.Large) return ObjectSizeType.Big;
            return ObjectSizeType.Small;
        }

        /// <summary>
        /// Converts Vector3 direction to Direction enum
        /// </summary>
        public static Direction ToDirection(this Vector3 direction)
        {
            // Normalize to ensure we're working with unit vectors
            direction = direction.normalized;

            // Check which axis has the largest component
            float absX = Mathf.Abs(direction.x);
            float absZ = Mathf.Abs(direction.z);

            if (absZ > absX)
            {
                // Primarily moving along Z axis
                return direction.z > 0 ? Direction.North : Direction.South;
            }
            else
            {
                // Primarily moving along X axis
                return direction.x > 0 ? Direction.East : Direction.West;
            }
        }

        /// <summary>
        /// Converts Direction enum to Vector3
        /// </summary>
        public static Vector3 ToVector3(this Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return Vector3.forward;
                case Direction.South:
                    return Vector3.back;
                case Direction.East:
                    return Vector3.right;
                case Direction.West:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// Gets the number of cells a unit occupies
        /// </summary>
        public static int GetCellCount(this UnitSize size)
        {
            return size.Width * size.Height;
        }

        /// <summary>
        /// Gets the number of cells a unit occupies (old API compatibility)
        /// </summary>
        public static int GetNodeCount(this ObjectSizeType sizeType)
        {
            return sizeType.ToUnitSize().GetCellCount();
        }
    }
}
