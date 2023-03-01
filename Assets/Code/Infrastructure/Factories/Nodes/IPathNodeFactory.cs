using Code.Pathfinding;
using UnityEngine;

namespace Code.Infrastructure.Factories.Nodes
{
    public interface IPathNodeFactory
    {
        PathNode Create(int x, int y);
        PathNode Create(Vector3 position, Transform parent, int x, int y);
    }
}