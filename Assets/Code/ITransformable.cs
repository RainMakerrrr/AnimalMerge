using System.Threading.Tasks;
using Code.GridPathfinding;
using Code.Pathfinding;
using UnityEngine;

namespace Code
{
    public interface ITransformable
    {
        Vector3 Position { get; }
        Vector2Int IntPosition { get; }
        UnitSize UnitSize { get; }
        int SizeEffect { get; }
        GridCell CurrentPathNode { get; }

        Task Shift();
    }
}