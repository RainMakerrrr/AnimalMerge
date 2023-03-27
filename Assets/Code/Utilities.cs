using System;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public static class Utilities
    {
        public static Vector3 GetMovementOffset(ObjectSizeType sizeType, Vector3 direction)
        {
            float offset = direction == Vector3.forward ? 0.5f : -0.5f;
            
            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, offset);
                case ObjectSizeType.Big:
                    return new Vector3(offset, 0f, offset);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public static Vector3 GetMovementOffset(PathNode node, ObjectSizeType sizeType, Vector3 direction)
        {
            float offset = direction == Vector3.forward ? 0.5f : -0.5f;

            switch (sizeType)
            {
                case ObjectSizeType.Small:
                    return Vector3.zero;
                case ObjectSizeType.Medium:
                    return new Vector3(0f, 0f, offset);
                case ObjectSizeType.Big:
                    return node.x == 7 ? new Vector3(-0.5f, 0f, 0.5f) : new Vector3(offset, 0f, offset);
                default:
                    throw new ArgumentOutOfRangeException();
            }
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