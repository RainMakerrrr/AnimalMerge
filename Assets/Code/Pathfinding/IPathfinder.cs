using System.Collections.Generic;

namespace Code.Pathfinding
{
    public interface IPathfinder
    {
        List<PathNode> FindPath(int startX, int startY, int endX, int endY, ObjectSizeType objectSizeType);
    }
}