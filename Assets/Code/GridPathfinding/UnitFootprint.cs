using System.Collections.Generic;
using UnityEngine;

namespace Code.GridPathfinding
{
    public static class UnitFootprint
    {
        public static Vector2Int AdjustAnchor(
            Vector2Int anchor, UnitSize size, Direction direction, int gridWidth, int gridHeight)
        {
            int adjustedX = anchor.x;
            int adjustedY = anchor.y;

            Vector3 directionVector = DirectionToVector(direction);
            int xDirection = GetCellOffsetDirection(directionVector.x);
            int zDirection = GetCellOffsetDirection(directionVector.z);

            int xExtent = size.Width;
            int yExtent = size.Height;

            if (size.IsRectangular() && (direction == Direction.East || direction == Direction.West))
            {
                xExtent = size.Height;
                yExtent = size.Width;
            }

            if (xDirection > 0)
            {
                if (anchor.x + xExtent > gridWidth)
                    adjustedX = gridWidth - xExtent;
            }
            else if (anchor.x - (xExtent - 1) < 0)
            {
                adjustedX = xExtent - 1;
            }

            if (zDirection > 0)
            {
                if (anchor.y + yExtent > gridHeight)
                    adjustedY = gridHeight - yExtent;
            }
            else if (anchor.y - (yExtent - 1) < 0)
            {
                adjustedY = yExtent - 1;
            }

            return new Vector2Int(
                Mathf.Clamp(adjustedX, 0, gridWidth - 1),
                Mathf.Clamp(adjustedY, 0, gridHeight - 1));
        }

        public static List<Vector2Int> Cells(
            Vector2Int anchor, UnitSize size, Direction direction, int gridWidth, int gridHeight)
        {
            var positions = new List<Vector2Int>();
            Cells(anchor, size, direction, gridWidth, gridHeight, positions);

            return positions;
        }

        public static void Cells(
            Vector2Int anchor,
            UnitSize size,
            Direction direction,
            int gridWidth,
            int gridHeight,
            List<Vector2Int> positions)
        {
            positions.Clear();

            Vector2Int adjustedAnchor = AdjustAnchor(anchor, size, direction, gridWidth, gridHeight);

            if (size.IsRectangular())
            {
                if (direction == Direction.North || direction == Direction.South)
                {
                    int zDirection = direction == Direction.North ? 1 : -1;

                    for (var dy = 0; dy < size.Height; dy++)
                        positions.Add(new Vector2Int(adjustedAnchor.x, adjustedAnchor.y + dy * zDirection));
                }
                else
                {
                    int xDirection = direction == Direction.East ? 1 : -1;

                    for (var dx = 0; dx < size.Height; dx++)
                        positions.Add(new Vector2Int(adjustedAnchor.x + dx * xDirection, adjustedAnchor.y));
                }

                return;
            }

            Vector3 directionVector = DirectionToVector(direction);
            int squareXDirection = GetCellOffsetDirection(directionVector.x);
            int squareZDirection = GetCellOffsetDirection(directionVector.z);

            for (var dx = 0; dx < size.Width; dx++)
            {
                for (var dy = 0; dy < size.Height; dy++)
                {
                    positions.Add(new Vector2Int(
                        adjustedAnchor.x + dx * squareXDirection,
                        adjustedAnchor.y + dy * squareZDirection));
                }
            }
        }

        private static Vector3 DirectionToVector(Direction direction)
        {
            switch (direction)
            {
                case Direction.North:
                    return new Vector3(0, 0, 1);
                case Direction.South:
                    return new Vector3(0, 0, -1);
                case Direction.East:
                    return new Vector3(1, 0, 0);
                case Direction.West:
                    return new Vector3(-1, 0, 0);
                default:
                    return new Vector3(0, 0, 1);
            }
        }

        private static int GetCellOffsetDirection(float directionComponent)
        {
            return directionComponent < 0 ? -1 : 1;
        }
    }
}
