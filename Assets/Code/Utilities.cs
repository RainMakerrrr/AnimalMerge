using System;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public static class Utilities
    {
        /// <summary>
        /// Calculates the offset for a single axis based on the direction component.
        /// The anchor point is always the "back-left" cell relative to the unit's facing direction.
        /// </summary>
        /// <param name="directionComponent">Direction component (x or z)</param>
        /// <returns>Offset value: 0.5 if moving forward/right, -0.5 if moving backward/left, 0.5 if stationary</returns>
        private static float GetAxisOffset(float directionComponent)
        {
            if (Mathf.Approximately(directionComponent, 0f))
                return 0.5f; // Default offset when not moving along this axis

            return directionComponent > 0 ? 0.5f : -0.5f;
        }

        /// <summary>
        /// Calculates the world position offset for a unit based on its size and movement direction.
        /// The anchor point (GridCell position) represents the "back-left" corner relative to the unit's facing direction.
        /// Examples for 2x2 unit:
        /// - Direction (0,0,1) forward: anchor at (3,0), occupies cells: (3,0), (3,1), (4,0), (4,1), offset = (0.5, 0, 0.5)
        /// - Direction (0,0,-1) backward: anchor at (3,9), occupies cells: (3,9), (3,8), (4,8), (4,9), offset = (0.5, 0, -0.5)
        /// </summary>
        public static Vector3 GetMovementOffset(ObjectSizeType sizeType, Vector3 direction)
        {
            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    // 1x1 unit: no offset needed, WorldPosition is already at cell center
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    // 1x2 unit: offset depends on orientation and direction
                    // Vertical orientation (moving along Z): offset in Z axis based on direction
                    // Horizontal orientation (moving along X): offset in X axis based on direction
                    if (Mathf.Abs(direction.z) > Mathf.Abs(direction.x))
                    {
                        // Moving vertically: 1x2 in Z direction
                        return new Vector3(0f, 0f, GetAxisOffset(direction.z));
                    }
                    else
                    {
                        // Moving horizontally: 2x1 in X direction
                        return new Vector3(GetAxisOffset(direction.x), 0f, 0f);
                    }
                case ObjectSizeType.Big:
                    Debug.Log($"[GetMovementOffset] Direction: {direction}, sizeType: {sizeType}]");
                    // 2x2 unit: offset to center over 4 cells, adjusted for movement direction
                    // Both X and Z offsets depend on their respective direction components
                    return new Vector3(GetAxisOffset(direction.x), 0f, GetAxisOffset(direction.z));
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

        /// <summary>
        /// Converts Direction enum to Vector3 direction
        /// </summary>
        private static Vector3 DirectionToVector3(GridPathfinding.Direction direction)
        {
            switch (direction)
            {
                case GridPathfinding.Direction.North:
                    return Vector3.forward;
                case GridPathfinding.Direction.South:
                    return Vector3.back;
                case GridPathfinding.Direction.East:
                    return Vector3.right;
                case GridPathfinding.Direction.West:
                    return Vector3.left;
                default:
                    return Vector3.forward;
            }
        }

        /// <summary>
        /// Overload for UnitSize and Direction - modern types without casting
        /// </summary>
        public static Vector3 GetMovementOffset(GridPathfinding.UnitSize unitSize, GridPathfinding.Direction direction)
        {
            var directionVector = DirectionToVector3(direction);

            // 1×1 unit
            if (unitSize.Width == 1 && unitSize.Height == 1)
            {
                return Vector3.zero;
            }

            // 1×2 unit
            if (unitSize.Width == 1 && unitSize.Height == 2)
            {
                return new Vector3(0f, 0f, GetAxisOffset(directionVector.z));
            }

            // 2×1 unit
            if (unitSize.Width == 2 && unitSize.Height == 1)
            {
                return new Vector3(GetAxisOffset(directionVector.x), 0f, 0f);
            }

            // 2×2 unit
            if (unitSize.Width == 2 && unitSize.Height == 2)
            {
                return new Vector3(GetAxisOffset(directionVector.x), 0f, GetAxisOffset(directionVector.z));
            }

            // Default fallback
            return Vector3.zero;
        }

        /// <summary>
        /// Overload for GridCell with UnitSize and Direction
        /// </summary>
        public static Vector3 GetMovementOffset(GridPathfinding.GridCell cell, GridPathfinding.UnitSize unitSize, GridPathfinding.Direction direction)
        {
            return GetMovementOffset(unitSize, direction);
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