using Code.Pathfinding;
using UnityEngine;
using Grid = Code.Pathfinding.Grid;

namespace Code.Infrastructure.Factories.Nodes
{
    public interface IPathNodeFactory
    {
        PathNode Create(int x, int y, Grid grid);
        PathNode Create(Vector3 position, Transform parent, int x, int y, Grid grid);
    }
}