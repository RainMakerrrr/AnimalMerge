using System.Collections.Generic;
using Code.NewPathfinding;
using UnityEngine;

namespace Code.Pathfinding
{
    public interface IPathfinder
    {
        List<PathNode> FindPath(int startX, int startY, int endX, int endY, ObjectSizeType objectSizeType,
            Vector3 direction);

        List<GridNode> FindPath(Vector2Int startPos, Vector2Int targetPos, Vector2Int unitSize);
    }
}