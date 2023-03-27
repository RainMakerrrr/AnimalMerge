using System.Collections.Generic;
using UnityEngine;

namespace Code.Pathfinding
{
    public interface IPathfinder
    {
        List<PathNode> FindPath(int startX, int startY, int endX, int endY, ObjectSizeType objectSizeType,
            Vector3 direction);
    }
}