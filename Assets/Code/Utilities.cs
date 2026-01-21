using System;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public static class Utilities
    {
        public static Vector3 GetMovementOffset(ObjectSizeType sizeType, Vector3 direction)
        {
            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    // 1x1 unit: no offset needed, WorldPosition is already at cell center
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    // 1x2 unit: offset depends on orientation
                    // North/South (vertical): offset in Z only
                    // East/West (horizontal): offset in X only
                    if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
                    {
                        return new Vector3(0f, 0f, 0.5f);
                    }
                    else
                    {
                        return new Vector3(0.5f, 0f, 0f);
                    }
                case ObjectSizeType.Big:
                    // 2x2 unit: offset to center over 4 cells, adjusted for direction
                    float xOffset = Mathf.Approximately(direction.x, 0) ? 0.5f : (direction.x > 0 ? 0.5f : -0.5f);
                    float zOffset = Mathf.Approximately(direction.z, 0) ? 0.5f : (direction.z > 0 ? 0.5f : -0.5f);
                    return new Vector3(xOffset, 0f, zOffset);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Vector3 GetMovementOffset(GridCell cell, ObjectSizeType sizeType, Vector3 direction)
        {
            // Use the same logic as the non-cell version
            // The cell parameter was previously used for boundary handling,
            // but GridManager now handles boundary adjustment internally
            return GetMovementOffset(sizeType, direction);
        }

        [System.Obsolete("Use GetMovementOffset(GridCell, ObjectSizeType, Vector3) instead")]
        public static Vector3 GetMovementOffset(PathNode node, ObjectSizeType sizeType, Vector3 direction)
        {
            // Delegate to the main implementation for consistency
            return GetMovementOffset(sizeType, direction);
        }

        public static int GetNodeCount(ObjectSizeType sizeType)
        {
            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    return 1;
                case ObjectSizeType.Medium:
                    return 2;
                case ObjectSizeType.Big:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(sizeType), sizeType, null);
            }
        }

        public static Vector3 GetDirection(Vector3 first, Vector3 second)
        {
            Vector3 direction;

            return Vector3.zero;

            // if (second.z > first.z)
            // {
            //     direction = (second - first).normalized;
            // }
            // else
            // {
            //     direction = (first - second).normalized;
            // }
            //
            // return direction;
        }
    }
}